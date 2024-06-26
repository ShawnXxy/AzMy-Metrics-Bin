﻿
using System.Text;

namespace AzMyStatusBin
{
    class StatusLog
    {
        private string _strLogFilePath;
        private string _strLogFileDir;
        private string _strLogFileName;
        private long _lastReadPos = 0;

        public StatusLog(string strLogFileDir, string strLogFileName)
        {
            _strLogFilePath = strLogFileDir + @"/" + strLogFileName;
            if (File.Exists(_strLogFilePath))
            {
                _strLogFileDir = strLogFileDir;
                _strLogFileName = strLogFileName;
            }
            else
            {
                Console.WriteLine("MySQL Global Status Log File: {0} does not exist!", _strLogFilePath);

                Console.WriteLine("Log File: {0} will be created!", _strLogFilePath);
                string directory_path = "/var/lib/custom/azMy-metrics-collector";
                string file_name = "azMy_global_status.log";
                if (!Directory.Exists(directory_path))
                {
                    Directory.CreateDirectory(directory_path);
                }
                StreamWriter writer = new StreamWriter(Path.Combine(directory_path, file_name), true);
                
                _strLogFilePath = null;
                _strLogFileDir = null;
                _strLogFileName = null;
            }
        }

        /*
         * TODO: It does not add support for the log rotation monitoring by FileSystemWatcher
         * it is simple version now to try pick up the error logs. If log rotated, the seek 
         * will fail, just handle it to reset the pos to zero.
         */

        public string GetJsonPayload()
        {
            if (_strLogFilePath == null)
                return null;

            try
            {
                using var fileStream = new FileStream(_strLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(_lastReadPos, SeekOrigin.Begin);
                Console.WriteLine("File Postion {0}", _lastReadPos);
                using StreamReader sr = new StreamReader(fileStream);
                if (sr.EndOfStream)
                {
                    return null; //the file stream is empty, so return NULL;
                }

                var jsonString = new StringBuilder();
                jsonString.Append("[");

                string s;
                while ((s = sr.ReadLine()!) != null)
                {
                    _lastReadPos = fileStream.Position;
                    if (s.Contains(" "))
                    {
                        string [] words = s.Split(' ');
                        
                        jsonString.Append("{");
                        jsonString.Append(string.Format("\"{0}\":\"{1}\"", "Time", words[0]));
                        jsonString.Append(",");
                        jsonString.Append(string.Format("\"{0}\":\"{1}\"", "MetricName", words[1]));
                        jsonString.Append(",");
                        jsonString.Append(string.Format("\"{0}\":\"{1}\"", "MetricValue", words[2]));
                        jsonString.Append(",");
                        jsonString.Append(string.Format("\"{0}\":\"{1}\"", "LogicalServerName", words[3]));
                        jsonString.Append("},");
                    }

                }
                jsonString.Remove(jsonString.Length - 1, 1);
                jsonString.Append("]");
                if (jsonString.ToString() == "]")
                {
                    return null;
                }
                else
                {
                    Console.WriteLine(jsonString.ToString());
                    return jsonString.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _lastReadPos = 0; //ToDo: It may not handle all corner case
                return null;
            }
        }
    }
}
