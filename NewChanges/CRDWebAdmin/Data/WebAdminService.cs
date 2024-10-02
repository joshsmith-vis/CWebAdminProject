using System.Net.Http;

namespace CRDWebAdmin.Data
{
    public class WebAdminService
    {
        private readonly HttpClient _httpClient;

        public WebAdminService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LogEntry[]> GetLogsAsync(DateTime startDate, DateTime endDate)
        {
            var response = await _httpClient.GetFromJsonAsync<LogEntry[]>($"api/admin/logs?startDate={startDate:o}&endDate={endDate:o}");
            return response;
        }

        public async Task<List<Dictionary<string, string>>> GetBlotterDataAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<Dictionary<string, string>>>("api/admin/blotter");
            return response;
        }
    }

    public class LogEntry
    {
        public string serverName { get; set; }
        public DateTime time { get; set; }
        public string Level { get; set; }
        public string message { get; set; }
        public string exception { get; set; }
        public string user { get; set; }
        public string component { get; set; }
        public string thread { get; set; }
    }
}