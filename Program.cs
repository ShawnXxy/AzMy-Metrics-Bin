// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;
using System.Threading.Tasks;

using MySqlConnector;
using System.Timers;
using System.Threading;
using AzMyStatusBin;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMySQLMetricsCollector
{
    class Program
    {
        static MySqlConnection conn = null;
        static MySQLMetrics myMetrics = null;
        static LAWorkspace logAnalyticsWorkspace = null;
        static StatusLog statusLog = null;
        static void Main(string[] args)
        {
            // Reads ApplicationInsights.config file if present: https://docs.microsoft.com/en-us/azure/azure-monitor/app/console
            // Sample: https://github.com/microsoft/ApplicationInsights-dotnet/blob/develop/examples/ConsoleApp/Program.cs

            // Create the DI container.
            IServiceCollection services = new ServiceCollection();                   

            // Being a regular console app, there is no appsettings.json or configuration providers enabled by default.
            // Hence instrumentation key must be specified here.
            services.AddApplicationInsightsTelemetryWorkerService("aa7c610a-626a-469e-84b0-3d552d2a2c3c");

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Obtain logger instance from DI.
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Obtain TelemetryClient instance from DI, for additional manual tracking or to flush.
            var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();


            telemetryClient.TrackEvent("AzMyStatusBinEvent");

            
            try
            {
                // Input MySQL connections tring
                Console.WriteLine("Please input the the MySQL connection information in the following");
                Console.WriteLine("MySQL hostname (Full FQDN):");
                string myHost = Console.ReadLine()!;
                Console.WriteLine("MySQL username:");
                string myUser = Console.ReadLine()!; 
                Console.WriteLine("MySQL password:");
                string myPwd = Console.ReadLine()!;
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
                string strWorkspaceID = Console.ReadLine()!;
                Console.WriteLine("Primary Key:");
                string strPrimaryKey = Console.ReadLine()!;

                logAnalyticsWorkspace ??= new LAWorkspace
                {
                    CustomerId = strWorkspaceID,
                    SharedKey = strPrimaryKey
                };

                statusLog = new StatusLog(@"/var/lib/custom/azMy-metrics-collector", @"azMy_global_status.log");

                var aTimer = new System.Timers.Timer(30000);
                aTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, e) => OnTimedEvent(s, e, myHost,myUser,myPwd));
                aTimer.AutoReset = true;
                aTimer.Enabled = true;

                Console.WriteLine("\nPress the Enter key to exit the application...");
                Console.WriteLine("The application started at {0}", DateTime.Now.ToString());
                Console.ReadLine();
                Console.WriteLine("Terminating the application...");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // Explicitly call Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if application terminates, telemetry is sent to the back-end.
            telemetryClient.Flush();
            Task.Delay(500000).Wait();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e,string myHost,string myUser, string myPwd)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
            var builder = new MySqlConnectionStringBuilder
            {
                Server = myHost,
                UserID = myUser,
                Password = myPwd,
            };
        
            conn = new MySqlConnection(builder.ConnectionString);

            using (conn)
            {

                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = @"select m.metric_name,
                                        g.VARIABLE_VALUE - m.origin_metric_value AS metric_value, 
                                        v.Variable_value AS logical_server_name 
                                    from 
                                         (select VARIABLE_NAME,VARIABLE_VALUE from information_schema.global_status) AS g,
                                         (select metric_name,origin_metric_value from azmy_metrics_collector.azmy_global_status) AS m,
                                         (SELECT Variable_value FROM INFORMATION_SCHEMA.GLOBAL_VARIABLES WHERE VARIABLE_NAME = 'LOGICAL_SERVER_NAME') AS v
                                    WHERE m.metric_name = g.VARIABLE_NAME;";
                    command.ExecuteNonQuery();

                    using (var reader = command.ExecuteReader())
                    {
                        StreamWriter writer2 = new StreamWriter(@"/var/lib/custom/azMy-metrics-collector/azMy_global_status.log", true);
                        while (reader.Read())
                        {
                            DateTime dt = DateTime.Now;
                            writer2.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            writer2.WriteLine(string.Format(" {0} {1} {2}", reader.GetString(0), reader.GetDouble(1), reader.GetString(2)));//写入一行

                        }
                        writer2.Close();
                    }
                    command.CommandText = @"UPDATE azmy_metrics_collector.azmy_global_status m, information_schema.global_status g
                             SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Updated global status data change table again");

                    string myGlobalStatus = statusLog.GetJsonPayload();
                    if (myGlobalStatus != null)
                    {
                        logAnalyticsWorkspace.InjestLog(myGlobalStatus, "AzMyStatus");
                    }

                }


            }

        }
    }
}


