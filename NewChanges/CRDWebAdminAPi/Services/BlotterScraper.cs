using System.Text.RegularExpressions;
using System.Text.Json;

namespace CRDWebAdminAPi.Services
{
    public class BlotterScraper
    {
        private readonly AuthSession _authenticatedSession;
        private readonly ServerNames _serverNames;

        public BlotterScraper(AuthSession authenticatedSession, ServerNames serverNames)
        {
            _authenticatedSession = authenticatedSession;
            _serverNames = serverNames;
        }

        public async Task<List<Dictionary<string, string>>> ScrapeAsync()
        {
            List<Dictionary<string, string>> blotterData = new List<Dictionary<string, string>>();

            if (await _authenticatedSession.LoginAsync())
            {
                try
                {
                    var serverIds = await _serverNames.GetServerNames();
                    if (serverIds.Any())
                    {
                        blotterData = await ScrapeBlotterDataAsync(serverIds);
                    }
                    else
                    {
                        Console.WriteLine("No server IDs found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting blotter data: {ex.Message}");
                }
                finally
                {
                    await _authenticatedSession.Logout();
                }

                return blotterData;

            }
            else
            {
                Console.WriteLine("Failed to login");
                return new List<Dictionary<string, string>>();
            }
        }

        private async Task<List<Dictionary<string, string>>> ScrapeBlotterDataAsync(List<string> serverIds)
        {
            var masterList = new List<Dictionary<string, string>>();

            foreach (var serverId in serverIds)
            {
                var pageUrl = $"/crts/diagnostics/blotter.do?serverId={serverId}";
                var response = await _authenticatedSession.Get(pageUrl);

                if (response != null)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var tableData = ParseTable(html);
                    
                    // Add server ID to each row
                    foreach (var row in tableData)
                    {
                        row["ServerId"] = serverId;
                    }
                    
                    masterList.AddRange(tableData);
                }
                else
                {
                    Console.WriteLine($"No response for {serverId}");
                }
            }

            return masterList;
        }

        private List<Dictionary<string, string>> ParseTable(string html)
        {
            var result = new List<Dictionary<string, string>>();

            // Find the table with the expandAll button
            var expandAllRegex = new Regex(@"<img\s+id=""expandAll"".*?<table\s+class=""dataTbl"".*?>(.*?)</table>", RegexOptions.Singleline);
            var tableMatch = expandAllRegex.Match(html);

            if (tableMatch.Success)
            {
                var tableHtml = tableMatch.Groups[1].Value;
                var headerRegex = new Regex(@"<th.*?>(.*?)</th>");
                var headers = headerRegex.Matches(tableHtml)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h) && h != "&nbsp;")
                    .ToList();

                var rowRegex = new Regex(@"<tr.*?>(.*?)</tr>");
                var rows = rowRegex.Matches(tableHtml).Cast<Match>().Skip(1); // Skip header row

                foreach (var row in rows)
                {
                    var cellRegex = new Regex(@"<td.*?>(.*?)</td>");
                    var cells = cellRegex.Matches(row.Groups[1].Value)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .ToList();

                    if (cells.Count >= headers.Count)
                    {
                        var rowData = new Dictionary<string, string>();
                        for (int i = 0; i < headers.Count; i++)
                        {
                            rowData[headers[i]] = cells[i + 2]; // Skip the first two cells (expand/collapse and consistency check)
                        }
                        result.Add(rowData);
                    }
                }
            }
            else
            {
                Console.WriteLine("Could not find the blotter data table");
            }

            return result;
        }

        public void SaveToFile(List<Dictionary<string, string>> data, string filename)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            System.IO.File.WriteAllText(filename, json);
        }
    }
}
