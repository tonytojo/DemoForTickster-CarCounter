using System;
using System.ServiceProcess;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using WindowsServiceCarCounter.Services;
using System.Collections.Generic;
using WindowsServiceCarCounter.Data;
using WindowsServiceCarCounter.Db;
using System.Data.SqlClient;
using System.Data;
using System.Timers;
using System.Net.Mail;
using System.Net;

//do cmd for win adm
//Then cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
//InstallUtil.exe C:\Users\dbadmin\source\repos\WindowsServiceCarCounter\WindowsServiceCarCounter\bin\Debug\WindowsServiceCarCounter.exe
//InstallUtil.exe -u C:\Users\dbadmin\source\repos\WindowsServiceCarCounter\WindowsServiceCarCounter\bin\Debug\WindowsServiceCarCounter.exe
//Logfile C:\Users\dbadmin\source\repos\WindowsServiceCarCounter\WindowsServiceCarCounter\bin\Debug\Logs

namespace WindowsServiceCarCounter
{
    public partial class CarCounter : ServiceBase
    {
        private Timer timer;
        private const string FILENAME = "a6109523-a23b-47ed-9732-40ea4535c82f.xml";
        private string FullFilePath = @"C:\Users\IdreFjall\CarCounter\a6109523-a23b-47ed-9732-40ea4535c82f.xml";

        /// <summary>
        /// C-tor some initialization
        /// </summary>
        public CarCounter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is started when the windows service is given a start command by SCM
        /// We watch only for files named a6109523-a23b-47ed-9732-40ea4535c82f.xml in 
        /// this directory C:\Users\IdreFjall\CarCounter
        /// </summary>
        /// <param name="args">N/A</param>
        protected override void OnStart(string[] args)
        {
            DateTime now = DateTime.Now;
            DateTime scheduleTime;

            //Depending of the current time we set the initial scheduleTime. The file need to be checked at x:15 and x:45
            if (now.Minute <= 15)
            {
                scheduleTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 15, 0);
            }
            else if (now.Minute <= 45)
            {
                scheduleTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 45, 0);
            }
            else
            {
                scheduleTime = (new DateTime(now.Year, now.Month, now.Day, now.Hour, 15, 0)).AddHours(1);
            }

            // For the first time, we set the amount of seconds between schedule time and current time in second
            int diffInSec = (int)Math.Ceiling(scheduleTime.Subtract(now).TotalSeconds);

            // Create a Timer object
            timer = new Timer();

            // Set the interval to run the first time after diffInSec
            timer.Interval = diffInSec * 1000; //We multiply by 1000 to get millisec
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            WriteToFile("The WindowService service has been started " + DateTime.Now);
        }

        /// <summary>
        /// We process the file when the time X.15 or x.45 for every hour
        /// We then reschedule for the next run
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Get the current time
            var currentTime = DateTime.Now;

            //Check current time if we have Minute 15 or 45
            if (currentTime.Minute == 15 || currentTime.Minute == 45)
            {
                timer.Interval = 30 * 60 * 1000; //add 30 minutes to reschedule the next run

                //Check if file exist and it is not empty
                if (File.Exists(FullFilePath) && new FileInfo(FullFilePath).Length > 0)
                {
                    WriteToFile("Process the file because all condition is fulfiled " + DateTime.Now);
                    ProcessFile();
                }
                else if (!File.Exists(FullFilePath))
                {
                    WriteToFile("We have no file located at " + FullFilePath);
                }
                else
                {
                    WriteToFile("The file located at " + FullFilePath + " is empty. We remove it");
                    new JsonHandling().DeleteFile(FullFilePath);
                }
            }
            else
                WriteToFile("Error currentTime.Minute is not 15 or 45");
        }

        protected override void OnStop()
        {
            timer.Stop();
            timer.Dispose();

            WriteToFile("The WindowService CarCounter has been stopped at " + DateTime.Now);
        }

        private void SendEmail(string errorMessage)
        {
            string fromEmail = "qbim999@gmail.com";
            string toEmail = "support@qbim.se";
            string appPassword = "ubxmdorhstcbqkvn"; // This is a generated app password

            using (var client = new SmtpClient("smtp.gmail.com"))
            {
                client.Port = 587;
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(fromEmail, appPassword);

                var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = "From windows service CarCounter",
                    Body = errorMessage,
                    IsBodyHtml = true
                };

                try
                {
                    client.Send(message);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    WriteToFile("We get an Exception when sending email   " + ex.Message);
                    Console.WriteLine("Error sending email: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// We process the file because all conditions is met. We have a file and it is not empty
        /// </summary>
        private void ProcessFile()
        {
            JsonHandling jsonHandling = new JsonHandling();
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(FullFilePath);
                string json = jsonHandling.Serialize(xmlDoc);
                json = jsonHandling.Clean(json);
                List<Item> carCounter = jsonHandling.DeserializeObject(json);
                jsonHandling.ChangeDate(carCounter);
                jsonHandling.SendToDb(carCounter);
                jsonHandling.DeleteFile(FullFilePath);
            }
            catch (Exception ex)
            {
                //We send an email to support@qbim.se
                SendEmail("An exception has occurred in Window Service CarCounter with this error " +  ex.Message + "  " + DateTime.Now);
                WriteToFile("Exception occurred in method ProcessFile " + ex.Message);
            }
        }


        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                //Create a file to write to
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
