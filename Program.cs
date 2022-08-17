// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;
using System.Threading.Tasks;

using MySqlConnector;
using System.Timers;

namespace AzureMySqlExample
{
    class Create
    {
        static async Task Main(string[] args)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "",
                Database = "",
                UserID = "",
                Password = "",
                SslMode = MySqlSslMode.Required,
            };

            using (var conn = new MySqlConnection(builder.ConnectionString))
            {
                Console.WriteLine("Opening connection");
                await conn.OpenAsync();  //establish a connection to MySQL

                using (var command = conn.CreateCommand())   // CreateCommand:sets the CommandText property
                {
                    
                //step 1: Create a table to restore the global status
                    
                    command.CommandText = "DROP TABLE IF EXISTS my_global_status;";
                    await command.ExecuteNonQueryAsync();  // run the database commands
                    Console.WriteLine("Finished dropping table (if existed)");

                    command.CommandText = "CREATE TABLE my_global_status (metric_name VARCHAR(64) NOT NULL UNIQUE, origin_metric_value VARCHAR(1024));";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("Finished creating table");
                    
                //step 2: Insert the variables selected from information_schema.global_status table to my_global_status.

                    command.CommandText = @"INSERT INTO my_global_status (metric_name, origin_metric_value) select * from performance_schema.global_status;";
                    int rowCount = await command.ExecuteNonQueryAsync();
                    Console.WriteLine(String.Format("Number of rows inserted={0}", rowCount));

                //step 3 : Get the changed values and output it to a file.

                    command.CommandText = @"select m.metric_name,g.VARIABLE_VALUE - m.origin_metric_value AS metric_value from 
                        (select VARIABLE_NAME,VARIABLE_VALUE from performance_schema.global_status) AS g,
                        (select metric_name,origin_metric_value from globalstatus.my_global_status) AS m WHERE m.metric_name = g.VARIABLE_NAME;";


                    // if second parameter is true：append

                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        StreamWriter writer = new StreamWriter(@"/home/azureuser/AzureMySqlExample/mysql_global_status.log",true);  
                        while (await reader.ReadAsync())
                        {

                            DateTime dt = DateTime.Now;
                            // Console.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            // Console.WriteLine(string.Format(" {0} {1}",reader.GetString(0),reader.GetDouble(1)));
                            writer.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            writer.WriteLine(string.Format(" {0} {1}",reader.GetString(0),reader.GetDouble(1)));

                        }
                        writer.Close(); 

                    }
                    
                //step 4: Flush history table to make it as the next baseline

                    command.CommandText = @"UPDATE globalstatus.my_global_status m, performance_schema.global_status g
                    SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine("Updated history table");
                }

            }

            //step 5: Use timer to control it, write to a file every 60 seconds.

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 60000; //  ms
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(test); 
            Console.ReadKey();
        }
            

        // Function
        static async void test(object? sender, EventArgs e)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "mysql-flex-1.mysql.database.azure.com",
                Database = "globalstatus",
                UserID = "myadmin",
                Password = "666666Xk",
                SslMode = MySqlSslMode.Required,
            };
            using (var conn = new MySqlConnection(builder.ConnectionString))
            {

                await conn.OpenAsync(); 
                using (var command = conn.CreateCommand()) 
                {
                    command.CommandText = @"select m.metric_name,g.VARIABLE_VALUE - m.origin_metric_value AS metric_value from 
                        (select VARIABLE_NAME,VARIABLE_VALUE from performance_schema.global_status) AS g,
                        (select metric_name,origin_metric_value from globalstatus.my_global_status) AS m WHERE m.metric_name = g.VARIABLE_NAME;";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        StreamWriter writer2 = new StreamWriter(@"/home/azureuser/AzureMySqlExample/mysql_global_status.log",true); 
                        while (await reader.ReadAsync())
                        {
                            DateTime dt = DateTime.Now;
                            writer2.Write(dt.GetDateTimeFormats('s')[0].ToString());
                            writer2.WriteLine(string.Format(" {0} {1}",reader.GetString(0),reader.GetDouble(1)));//写入一行

                        }
                        writer2.Close(); 
                    }
                    command.CommandText = @"UPDATE globalstatus.my_global_status m, performance_schema.global_status g
                    SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("Updated history table again");

                }

            } 

        }

    }

}


        







