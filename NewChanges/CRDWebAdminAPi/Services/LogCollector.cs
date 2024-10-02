using System.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CRDWebAdminAPi.Services {
    public class LogCollector
    {
        private readonly AuthSession authenticatedSession;
        private readonly ServerNames serverNames;

        public LogCollector(AuthSession authenticatedSession, ServerNames serverNames)
        {
            this.authenticatedSession = authenticatedSession;
            this.serverNames = serverNames;
        }

        public async Task CollectLogs()
        {
            Console.WriteLine("Starting log collection");

            if (await this.authenticatedSession.LoginAsync())
            {
                Console.WriteLine("Logged in successfully");
                var serverNames = await this.serverNames.GetServerNames();
                foreach (var serverName in serverNames)
                {
                    string logUrl = $"/crts/diagnostics/logview.do?fileName=cr_server.html&serverId={serverName}";
                    var response = await authenticatedSession.Get(logUrl);
                    if (response != null)
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        string timestamp = DateTime.Now.ToString("dd_MM_yy_HH_mm");
                        string filename = $"./static/logs/{serverName}_server_{timestamp}.html";

                        // Ensure the directory exists
                        Directory.CreateDirectory("./static/logs/");
                        using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
                        {
                            writer.Write(content);
                        }

                        Console.WriteLine($"Saved log for {serverName} as {filename}");
                    }
                    else
                    {
                        Console.WriteLine($"No response for {serverName}");
                    }
                }
                await this.authenticatedSession.Logout();
            }
            else
            {
                Console.WriteLine("Failed to login"); 
            }
        }

        public async Task<List<Dictionary<string, object>>> GetAllLogsAsync(DateTime? startDate, DateTime? endDate)
        {
            Console.WriteLine("Starting log collection");
            DataTable logTable = new DataTable();

            if (await this.authenticatedSession.LoginAsync())
            {
                try
                {
                    var serverNames = await this.serverNames.GetServerNames();
                    foreach (var serverName in serverNames)
                    {
                        var serverLogs = await CollectJsonLogs(serverName);
                        logTable.Merge(serverLogs);
                    }

                    //DataView view = logTable.DefaultView;

                    //// Apply date filter if provided
                    //if (startDate.HasValue || endDate.HasValue)
                    //{
                    //    string filterExpression = "";

                    //    if (startDate.HasValue)
                    //    {
                    //        filterExpression += $"time >= #{startDate.Value.ToUniversalTime():yyyy-MM-dd HH:mm:ss}#";
                    //    }

                    //    if (endDate.HasValue)
                    //    {
                    //        if (!string.IsNullOrEmpty(filterExpression))
                    //        {
                    //            filterExpression += " AND ";
                    //        }
                    //        filterExpression += $"time <= #{endDate.Value.ToUniversalTime():yyyy-MM-dd HH:mm:ss}#";
                    //    }

                    //    view.RowFilter = filterExpression;
                    //    Console.WriteLine($"Applying filter: {filterExpression}");
                    //}

                    // Sort the view by time descending to get the most recent logs


                    DataView view = logTable.DefaultView;
                    view.Sort = "time DESC";
                    logTable = view.ToTable();
                        logTable = logTable.AsEnumerable().Skip(0).Take(200).CopyToDataTable();

                    //// Take only the top 200 rows
                    //DataTable filteredTable;
                    //if (view.Count > 200)
                    //{
                    //    filteredTable = view.ToTable(false, view.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray(), 0, 200);
                    //}
                    //else
                    //{
                    //    filteredTable = view.ToTable();
                    //}

                    // Convert DataTable to List<Dictionary<string, object>>
                    var result = ConvertDataTableToList(logTable);

                    Console.WriteLine($"Filtered log table contains {result.Count} rows");

                    await this.authenticatedSession.Logout();

                    return result;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in GetAllLogsAsync: {e.Message}");
                    Console.WriteLine($"Stack Trace: {e.StackTrace}");
                    return new List<Dictionary<string, object>>();
                }
            }
            else
            {
                Console.WriteLine("Failed to login");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<DataTable> CollectJsonLogs(string serverName)
        {
            Console.WriteLine("Starting JSON log collection");

            DataTable logTable = new DataTable();
            logTable.Columns.Add("serverName", typeof(string));
            logTable.Columns.Add("time", typeof(DateTime));
            logTable.Columns.Add("Level", typeof(string));
            logTable.Columns.Add("message", typeof(string));
            logTable.Columns.Add("exception", typeof(string));
            logTable.Columns.Add("user", typeof(string));
            logTable.Columns.Add("component", typeof(string));
            logTable.Columns.Add("thread", typeof(string));
            logTable.Columns.Add("workstatus", typeof(string));  // Changed to string
            logTable.Columns.Add("mdc", typeof(string));  // Changed to string

            string logUrl = $"/crts/diagnostics/logview.do?fileName=cr_server_json.log&serverId={serverName}";
            var response = await authenticatedSession.Get(logUrl);
            if (response != null)
            {
                string content = await response.Content.ReadAsStringAsync();
                ParseJsonLogContent(content, logTable, serverName);
                Console.WriteLine($"Parsed JSON log for {serverName}");
            }
            else
            {
                Console.WriteLine($"No response for {serverName}");
            }
            //WriteDataTableToJsonFile(logTable, $"./static/logs/{serverName}_server_json.log");

            return logTable;
        }

        private void ParseJsonLogContent(string content, DataTable logTable, string serverName)
        {
            // Extract content from within <pre> tags if present
            var match = Regex.Match(content, @"<pre>(.*?)</pre>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                content = match.Groups[1].Value;
            }

            var logEntries = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in logEntries)
            {
                try
                {
                    var logEntry = JsonSerializer.Deserialize<LogEntry>(entry);
                    File.AppendAllText("log_entries.log", entry + Environment.NewLine);
                    
                    // Provide default values for null fields
                    DateTime timestamp = DateTime.TryParse(logEntry.timestamp, out var parsedTimestamp) 
                        ? parsedTimestamp 
                        : DateTime.MinValue;

                    logTable.Rows.Add(
                        serverName,
                        timestamp,
                        logEntry.level ?? "Unknown",
                        logEntry.message ?? string.Empty,
                        logEntry.exception ?? string.Empty,
                        logEntry.user ?? string.Empty,
                        logEntry.component ?? string.Empty,
                        logEntry.thread ?? string.Empty,
                        JsonSerializer.Serialize(logEntry.workstatus),  // Serialize to JSON string
                        JsonSerializer.Serialize(logEntry.mdc)  // Serialize to JSON string
                    );
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing log entry: {ex.Message}");
                    Console.WriteLine($"Problematic entry: {entry}");
                }
            }
        }

        public void WriteDataTableToJsonFile(DataTable dataTable, string filePath)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    rowDict[col.ColumnName] = row[col];
                }
                rows.Add(rowDict);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonString = JsonSerializer.Serialize(rows, options);
            File.WriteAllText(filePath, jsonString);

            Console.WriteLine($"JSON file written to: {filePath}");
        }

        public class LogEntry
        {
            public string timestamp { get; set; }
            public string level { get; set; }
            public string message { get; set; }
            public string exception { get; set; }
            public WorkStatus workstatus { get; set; }  // Changed to string
            public string component { get; set; }
            public string thread { get; set; }
            public string logger { get; set; }
            public string location { get; set; }
            public MDC mdc { get; set; }
            public string user { get; set; }
        }

        public class MDC
        {
            public string RunId { get; set; }
        }

        public class WorkStatus
        {
            public string timestamp { get; set; }
            public string component { get; set; }
            public string runid { get; set; }
            public string duration { get; set; }
            public version version { get; set; }
            public string description { get; set; }
            public string jobid { get; set; }
            public string action { get; set; }
            public string thread { get; set; }
            public state state { get; set; }
        }

        public class version
        {
            public string name { get; set; }
            public string versionnumber { get; set; }
            public string build { get; set; }
        }

        public class state
        {
            public stacktrace stacktrace { get; set; }
            public List<frame> frame { get; set; }
        }

        public class stacktrace
        {
            public string msg { get; set; }
            public stack stack { get; set; }
        }

        public class stack
        {
            public string stfr { get; set; }
        }

        public class frame
        {
            public string description { get; set; }
            public List<variable> variable { get; set; }
            public string method { get; set; }
            public string @class { get; set; }
        }

        public class variable
        {
            public string description { get; set; }
            public string value { get; set; }
            public string type { get; set; }
        }

        private long CalculateDataSize(DataView view)
        {
            long size = 0;
            foreach (DataColumn col in view.Table.Columns)
            {
                if (col.DataType == typeof(string))
                {
                    size += view.Count * (view.Cast<DataRowView>().Max(row => row[col.ColumnName]?.ToString()?.Length ?? 0) * 2);
                }
                else if (col.DataType == typeof(DateTime))
                {
                    size += view.Count * 8; // Assuming 8 bytes for DateTime
                }
                else if (col.DataType == typeof(int) || col.DataType == typeof(float))
                {
                    size += view.Count * 4;
                }
                else if (col.DataType == typeof(long) || col.DataType == typeof(double))
                {
                    size += view.Count * 8;
                }
                else if (col.DataType == typeof(bool))
                {
                    size += view.Count * 1;
                }
                else
                {
                    // For other types, use a default size (e.g., 4 bytes)
                    size += view.Count * 4;
                }
            }
            return size;
        }

        private List<Dictionary<string, object>> ConvertDataTableToList(DataTable dt)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(WorkStatus) || col.DataType == typeof(MDC))
                    {
                        // Serialize complex objects to JSON strings
                        dict[col.ColumnName] = JsonSerializer.Serialize(row[col]);
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        // Convert DateTime to ISO 8601 string
                        dict[col.ColumnName] = ((DateTime)row[col]).ToString("o");
                    }
                    else
                    {
                        dict[col.ColumnName] = row[col]?.ToString();
                    }
                }
                result.Add(dict);
            }
            return result;
        }
    }
}