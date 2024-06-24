using MySqlConnector;

namespace AzMyStatusBin
{
    class MySQLMetrics
    {
        // Connection to MySQL
        private MySqlConnection _connection = null;

        public MySQLMetrics(MySqlConnection connection)
        {
            _connection = connection;

            using (var command = _connection.CreateCommand())   // CreateCommand:sets the CommandText property
            {

                //step 1: Create a table to store the global status
                /*
                    CREATE DATABASE globalstatus;
                    USE globalstatus;

                -- metric_name will be matched the VARIABLE_NAME in performance_schema.global_status
                -- origin_metric_value will be copied directly from performance_schema.global_status when calling

                    Create table my_global_status(
                        metric_name varchar(64) NOT NULL UNIQUE,
                        origin_metric_value varchar(1024)
                    );
                */
                //command.CommandText = "CREATE DATABASE if not exists azmy_metrics_collector";
                //command.ExecuteNonQuery();
                command.CommandText = "CREATE DATABASE IF NOT EXISTS azmy_metrics_collector; CREATE TABLE IF NOT EXISTS azmy_metrics_collector.azmy_global_status (metric_name VARCHAR(64) NOT NULL UNIQUE, origin_metric_value VARCHAR(1024)) ENGINE = MEMORY;";
                command.ExecuteNonQuery();
                Console.WriteLine("Base table azmy_metrics_collector.azmy_global_status is created");

                //step 2: Insert the variables selected from performance_schema.global_status table to my_global_status. 
                //This will be used as a baseline. If table already exists, skip to next step
                command.CommandText = @"REPLACE INTO azmy_metrics_collector.azmy_global_status (metric_name, origin_metric_value) select * from performance_schema.global_status;";
                command.ExecuteNonQuery();
                //int rowCount = command.ExecuteNonQueryAsync();
                Console.WriteLine("Copied metric value from performance_schema");
                //Console.WriteLine(String.Format("Number of rows inserted={0}", rowCount));

                // Step 3, To get the difference between current metric value and previous checked value
                /*
                    a, get the current value from performance_schema
                    b, get the current stored value in metric_value column
                    c, if metric_name matched VARIABLE_NAME from performance_schema, do the subtraction
                    d, insert the substracted value into the column of the table we created above [OPTIONAL]

                    -- c get the subtraction
                    SELECT m.metric_name, g.VARIABLE_VALUE - m.origin_metric_value AS metric_value, v.Variable_value AS logical_server_name FROM
                        -- a get the current value from performance_schema
                        (SELECT VARIABLE_NAME, VARIABLE_VALUE FROM performance_schema.global_status) AS g,
                        -- b get the current stored value in metric_value column
                        (SELECT metric_name, origin_metric_value FROM globalstatus.my_global_status) AS m,
                        (SELECT Variable_value FROM performance_schema.GLOBAL_VARIABLES WHERE VARIABLE_NAME = 'LOGICAL_SERVER_NAME') AS v
                    WHERE m.metric_name = g.VARIABLE_NAME;
                */
                command.CommandText = @"select m.metric_name,g.VARIABLE_VALUE - m.origin_metric_value AS metric_value, v.Variable_value AS logical_server_name from 
                        (select VARIABLE_NAME,VARIABLE_VALUE from performance_schema.global_status) AS g,
                        (select metric_name,origin_metric_value from azmy_metrics_collector.azmy_global_status) AS m, 
                        (SELECT Variable_value FROM performance_schema.GLOBAL_VARIABLES WHERE VARIABLE_NAME = 'LOGICAL_SERVER_NAME') AS v
                        WHERE m.metric_name = g.VARIABLE_NAME;";
                command.ExecuteNonQuery();
                Console.WriteLine("Get the changed values and output it to a file");

                using (var reader = command.ExecuteReader())
                {

                    // if second parameter is true：append
                    string directory_path = "/var/lib/custom/azMy-metrics-collector";
                    string file_name = "azMy_global_status.log";
                    if (!Directory.Exists(directory_path))
                    {
                        Directory.CreateDirectory(directory_path);
                    }
                    StreamWriter writer = new StreamWriter(Path.Combine(directory_path, file_name), true);

                    while (reader.Read())
                    {
                        DateTime dt = DateTime.Now;
                        
                        writer.Write(dt.GetDateTimeFormats('s')[0].ToString());
                        writer.WriteLine(string.Format(" {0} {1} {2}", reader.GetString(0), reader.GetDouble(1), reader.GetString(2)));

                    }
                    writer.Close();

                }

                //step 4: Flush history table to make it as the next baseline
                command.CommandText = @"UPDATE azmy_metrics_collector.azmy_global_status m, performance_schema.global_status g
                    SET m.origin_metric_value = g.VARIABLE_VALUE WHERE m.metric_name = g.VARIABLE_NAME;";
                command.ExecuteNonQuery();
                Console.WriteLine("Updated history table");
            }

        }

        
    }
}
