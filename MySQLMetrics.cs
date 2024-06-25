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
                command.CommandText = "CREATE DATABASE if not exists azmy_metrics_collector";
                command.ExecuteNonQuery();
                command.CommandText = "CREATE TABLE IF NOT EXISTS azmy_metrics_collector.azmy_global_status (metric_name VARCHAR(64) NOT NULL UNIQUE, origin_metric_value VARCHAR(1024)) ENGINE = MEMORY;";
                command.ExecuteNonQuery();
                Console.WriteLine("Base table azmy_metrics_collector.azmy_global_status is created");

                //step 2: Insert the variables selected from performance_schema.global_status table to my_global_status. 
                //This will be used as a baseline. If table already exists, skip to next step
                command.CommandText = @"REPLACE INTO azmy_metrics_collector.azmy_global_status (metric_name, origin_metric_value) select * from performance_schema.global_status;";
                command.ExecuteNonQuery();
                Console.WriteLine("Copied metric value from performance_schema");

                // Step 3, To get the difference between current metric value and previous checked value
                // see OnTimedEvent() in Program.cs

                // //step 4: Flush history table to make it as the next baseline
                // see OnTimedEvent() in Program.cs
                
            }

        }

        
    }
}
