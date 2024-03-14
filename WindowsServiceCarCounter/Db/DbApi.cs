using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsServiceCarCounter.Data;


namespace WindowsServiceCarCounter.Db
{
        public static class DbApi
        {
            public static void InsertCarCounter(IEnumerable<Item> carCounters)
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["CarCounterConnection"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandText = "INSERT INTO CarCounter (id,direction,count,date) VALUES (@id,@direction,@count,@date);";
                        command.Parameters.Add(new SqlParameter("@id", SqlDbType.NChar));
                        command.Parameters.Add(new SqlParameter("@direction", SqlDbType.NChar));
                        command.Parameters.Add(new SqlParameter("@count", SqlDbType.NChar));
                        command.Parameters.Add(new SqlParameter("@date", SqlDbType.NChar));
                        try
                        {
                            foreach (var carCounter in carCounters)
                            {
                                command.Parameters[0].Value = carCounter.Id;
                                command.Parameters[1].Value = carCounter.Direction;
                                command.Parameters[2].Value = carCounter.Count;
                                command.Parameters[3].Value = carCounter.Date;
                                if (command.ExecuteNonQuery() != 1)
                                {
                                    throw new InvalidProgramException();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception to the Windows Event Log
                            using (EventLog eventLog = new EventLog("Application"))
                            {
                                eventLog.Source = "WinServCarCounter";
                                eventLog.WriteEntry($"An error occurred: {ex.Message}", EventLogEntryType.Error);
                            }
                        }
                    }
                }
            }
        }
    }
