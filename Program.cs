// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;
using System.Threading.Tasks;

using MySqlConnector;
using System.Timers;
using System.Threading;
using AzMyStatusBin;

namespace AzureMySQLMetricsCollector
{
    class Program
    {
        static MySqlConnection conn = null;
        static MySQLMetrics myMetrics = null;
        static LAWorkspace logAnalyticsWorkspace = null;
        //static string[] strTables;
        static StatusLog statusLog = null;
        static void Main(string[] args)
        {
            try
            {
                // Input MySQL connections tring
                Console.WriteLine("Please input the the MySQL connection information in the following");
                Console.WriteLine("MySQL hostname (Full FQDN):");
                string myHost = Console.ReadLine();
                Console.WriteLine("MySQL username:");
                string myUser = Console.ReadLine(); 
                Console.WriteLine("MySQL password:");
                string myPwd = Console.ReadLine();
                // Console.WriteLine("SSL path if any:");
                // string mySsl = Console.ReadLine();

                var builder = new MySqlConnectionStringBuilder
                {
                    Server = myHost,
                    UserID = myUser,
                    Password = myPwd,
                    // SslMode = MySqlSslMode.Required,
                };
                conn ??= new MySqlConnection(builder.ConnectionString);
                conn.Open();
                Console.WriteLine($"MySQL version : {conn.ServerVersion}");
                Console.WriteLine("============");

                myMetrics ??= new MySQLMetrics(conn);

                //input the workspace information
                Console.WriteLine("Please go to Logistic-Analytics-Workspace advanced setting, to copy Workspace-ID and Primary-Key");
                Console.WriteLine("Workspace ID:");
                string strWorkspaceID = Console.ReadLine();
                Console.WriteLine("Primary Key:");
                string strPrimaryKey = Console.ReadLine();

                logAnalyticsWorkspace ??= new LAWorkspace
                {
                    CustomerId = strWorkspaceID,
                    SharedKey = strPrimaryKey
                };

                statusLog = new StatusLog(@"/var/lib/custom/azMy-metrics-collector", @"azMy_global_status.log");

                var aTimer = new System.Timers.Timer(60000); //1 Minute
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;

                Console.WriteLine("\nPress the Enter key to exit the application...");
                Console.WriteLine("The application started at {0}", DateTime.Now.ToString());
                Console.ReadLine();
                aTimer.Stop();
                aTimer.Dispose();
                conn?.Close();

                Console.WriteLine("Terminating the application...");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);

            using (conn)
            {

                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = @"select m.metric_name,g.VARIABLE_VALUE - m.origin_metric_value AS metric_value from 
                                 (select VARIABLE_NAME,VARIABLE_VALUE from performance_schema.global_status) AS g,
                                 (select metric_name,origin_metric_value from azmy_metrics_collector.azmy_global_status) AS m WHERE m.metric_name = g.VARIABLE_NAME;";
                    command.ExecuteNonQuery();

                    using (var reader = command.ExecuteReader())
                    {
                        StreamWriter writer2 = new StreamWriter(@"/var/lib/custom/azMy-metrics-collector/azMy_global_status.log", true);
                        while (reader.Read())
                        {
                            DateTime dt = DateTime.Now;
                            writer2.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            writer2.WriteLine(string.Format(" {0} {1}", reader.GetString(0), reader.GetDouble(1)));//写入一行

                        }
                        writer2.Close();
                    }
                    command.CommandText = @"UPDATE azmy_metrics_collector.azmy_global_status m, performance_schema.global_status g
                             SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                    command.ExecuteNonQuery();

                    Console.WriteLine("Updated global status data change table again");

                    string myGlobalStatus = statusLog.GetJsonPayload();
                    if (myGlobalStatus != null)
                    {
                        logAnalyticsWorkspace.InjestLog(myGlobalStatus, "StatusLog");
                    }

                }


            }

        }
    }
}


