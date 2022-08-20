// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;
using System.Threading.Tasks;

using MySqlConnector;
using System.Timers;
using System.Threading;

namespace AzureMySqlExample
{
    class Create
    {
        static async Task Main(string[] args)
        {
            //input the MySQL connection string
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

            var conn = new MySqlConnection(builder.ConnectionString);
            using (conn)
            {
                Console.WriteLine("Opening connection");
                await conn.OpenAsync();  //establish a connection to MySQL
                Console.WriteLine($"MySQL version : {conn.ServerVersion}");
                Console.WriteLine("============================================================");

                using (var command = conn.CreateCommand())   // CreateCommand:sets the CommandText property
                {
                    
                    //step 1: Create a table to store the global status
                    /*
                        CREATE DATABASE globalstatus;
                        USE globalstatus;
                     
                    -- metric_name will be matched the VARIABLE_NAME in information_schema.global_status
                    -- origin_metric_value will be copied directly from information_schema.global_status when calling
                    
                        Create table my_global_status(
                            metric_name varchar(64) NOT NULL UNIQUE,
                            origin_metric_value varchar(1024)
                        );
                    */
                    command.CommandText = "CREATE DATABASE IF NOT EXISTS myMetricsCollector; Use myMetricsCollector; CREATE TABLE IF NOT EXISTS my_global_status (metric_name VARCHAR(64) NOT NULL UNIQUE, origin_metric_value VARCHAR(1024));";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("Base table my_global_status is created");
                    
                    //step 2: Insert the variables selected from information_schema.global_status table to my_global_status. 
                    //This will be used as a baseline. If table already exists, skip to next step
                    command.CommandText = @"INSERT INTO my_global_status (metric_name, origin_metric_value) select * from information_schema.global_status;";
                    int rowCount = await command.ExecuteNonQueryAsync();
                    Console.WriteLine("Copied metric value from information_schema");
                    Console.WriteLine(String.Format("Number of rows inserted={0}", rowCount));
                    
                    
                    // Step 3, To get the difference between current metric value and previous checked value
                    /*
                        a, get the current value from information_schema
                        b, get the current stored value in metric_value column
                        c, if metric_name matched VARIABLE_NAME from information_schema, do the subtraction
                        d, insert the substracted value into the column of the table we created above [OPTIONAL]
                        
                        -- c get the subtraction
                        SELECT m.metric_name, g.VARIABLE_VALUE - m.origin_metric_value AS metric_value FROM
                            -- a get the current value from information_schema
                            (SELECT VARIABLE_NAME, VARIABLE_VALUE FROM information_schema.global_status) AS g,
                            -- b get the current stored value in metric_value column
                            (SELECT metric_name, origin_metric_value FROM globalstatus.my_global_status) AS m
                        WHERE m.metric_name = g.VARIABLE_NAME;
                    */
                    command.CommandText = @"select m.metric_name,g.VARIABLE_VALUE - m.origin_metric_value AS metric_value from 
                        (select VARIABLE_NAME,VARIABLE_VALUE from information_schema.global_status) AS g,
                        (select metric_name,origin_metric_value from myMetricsCollector.my_global_status) AS m WHERE m.metric_name = g.VARIABLE_NAME;";
                    Console.WriteLine("Get the changed values and output it to a file");
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        // if second parameter is true：append
                        string directory_path = "/var/lib/custom/mysql-metrics-collector";
                        string file_name = "mysql_global_status.log";
                        if (!Directory.Exists(directory_path))
                        {
                            Directory.CreateDirectory(directory_path);
                        }
                        StreamWriter writer = new StreamWriter(Path.Combine(directory_path,file_name),true);  
                        while (await reader.ReadAsync())
                        {
                            DateTime dt = DateTime.Now;
                            
                            writer.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            writer.WriteLine(string.Format(" {0} {1}",reader.GetString(0),reader.GetDouble(1)));

                        }
                        writer.Close(); 

                    }
                    
                    //step 4: Flush history table to make it as the next baseline
                    command.CommandText = @"UPDATE myMetricsCollector.my_global_status m, information_schema.global_status g
                    SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("Updated history table");
                }

            }

            //step 5: Use timer to control it, write to a file every 60 seconds.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 30000; //  ms
            timer.Start();
            //timer.Elapsed += new System.Timers.ElapsedEventHandler(test); 
            Console.ReadKey();

            Console.WriteLine("\nPress the Enter key to exit the application...");
            Console.WriteLine("The application started at {0}", DateTime.Now.ToString());
            Console.ReadLine();
            timer.Stop();
            timer.Dispose();
            conn.Close();

            Console.WriteLine("Terminating the application...");
        }
            

    }

}


