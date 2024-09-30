using Microsoft.AspNetCore.Mvc;
using Vis.Common.Framework.Logging;
using Vis.TestMode.Services.CRDWebAdmin;

namespace Vis.TestMode.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CRDWebAdminController : ControllerBase
    {
        private readonly ServerLogCollectorService _logCollectorService;

        public ServerLogController(ILogFactory logFactory) : base(logFactory)
        {
            // Use your real URL, username, and password
            var baseUrl = "https://example.com";
            var username = "your-username";
            var password = "your-password";

            _logCollectorService = new ServerLogCollectorService(baseUrl, username, password);
        }

        [HttpGet("collect")]
        public async Task<IActionResult> CollectLogs()
        {
            var logFiles = await _logCollectorService.CollectLogsAsync();

            if (logFiles.Count == 0)
            {
                return BadRequest("Failed to collect any logs.");
            }

            return Ok(logFiles);
        }

        [HttpGet("download/{filename}")]
        public async Task<IActionResult> DownloadLog(string filename)
        {
            var filePath = $"./static/logs/{filename}";
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Log file not found.");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "text/html", filename);
        }
    }
}