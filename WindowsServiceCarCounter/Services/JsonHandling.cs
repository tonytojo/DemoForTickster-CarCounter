using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WindowsServiceCarCounter.Data;
using WindowsServiceCarCounter.Db;


namespace WindowsServiceCarCounter.Services
{
    public class JsonHandling : IJsonHandling
    {
        /// <summary>
        /// Clean the json file. Make it just an array of json object
        /// </summary>
        /// <param name="json">The json string to clean</param>
        /// <returns>Return the cleaned json string</returns>
        public string Clean(string json)
        {
            string temp = json.Remove(0, json.IndexOf('['));
            string result = temp.TrimEnd('}');
            result = result.TrimEnd();
            return result.Remove(result.LastIndexOf('}'));
        }

        /// <summary>
        /// Delete the file
        /// </summary>
        /// <param name="filename">This contains the complete path inclusive the filename</param>
        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// Deserialise the json string array into List<Item>
        /// </summary>
        /// <param name="json">The string to deserialize</param>
        /// <returns>Return List<Item></returns>
        public List<Item> DeserializeObject(string json)
        {
            return JsonConvert.DeserializeObject<List<Item>>(json);
        }

        /// <summary>
        /// Call a helper method to insert the array of Item into the database
        /// </summary>
        /// <param name="carCounter"></param>
        public void SendToDb(IEnumerable<Item> carCounter)
        {
            DbApi.InsertCarCounter(carCounter);
        }

        /// <summary>
        /// Serialize an Xml document into a string
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public string Serialize(XmlDocument xmlDoc)
        {
            return JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// This method will change the minute part according to this
        /// If have minute 59 we change minute to 30 and
        /// If we have minute 29 we change it to 0
        /// Note we doesn't need to return anything becuse the changes we make here is effected also
        /// in the passed argument of thye caller
        /// </summary>
        /// <param name="carCounter">The object that need to be changed</param>
        public void ChangeDate(List<Item> carCounter)
        {
            string format = "yyyy-MM-ddTHH:mm:ss";

            //Loop through the collection
            for (int i = 0; i < carCounter.Count; i++)
            {
                // Parse the original string into a DateTime object
                DateTime originalDateTime = DateTime.ParseExact(carCounter[i].Date, format, null);

                // Check if the original minute is 59 or 29/30
                if (originalDateTime.Minute == 59 || originalDateTime.Minute == 29 || originalDateTime.Minute == 30)
                {
                    // Create a new DateTime object with updated values
                    DateTime updatedDateTime = originalDateTime.Minute == 59
                       ? new DateTime(originalDateTime.Year, originalDateTime.Month, originalDateTime.Day, originalDateTime.Hour, 30, 0)
                       : new DateTime(originalDateTime.Year, originalDateTime.Month, originalDateTime.Day, originalDateTime.Hour, 0, 0);

                    // Format the updated DateTime object back to a string
                    carCounter[i].Date = updatedDateTime.ToString(format);
                }
            }
        }
    }
}
