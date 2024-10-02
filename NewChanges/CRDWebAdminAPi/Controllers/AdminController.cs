using Microsoft.AspNetCore.Mvc;
using CRDWebAdminAPi.Services;
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly LogCollector _serverLogCollector;
    private readonly ServerNames _serverNames;
    private readonly BlotterScraper _blotterScraper;

    public AdminController(ILogger<AdminController> logger, LogCollector serverLogCollector, ServerNames serverNames, BlotterScraper blotterScraper)
    {
        _logger = logger;
        _serverLogCollector = serverLogCollector;
        _serverNames = serverNames;
        _blotterScraper = blotterScraper;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var logs = await _serverLogCollector.GetAllLogsAsync(startDate, endDate);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs");
            return StatusCode(500, "An error occurred while retrieving logs. Please try again later.");
        }
    }

    [HttpGet("servers")]
    public async Task<IActionResult> GetServerNames()
    {
        try
        {
            var serverNames = await _serverNames.GetServerNames();
            return Ok(serverNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server names");
            return StatusCode(500, "An error occurred while retrieving server names. Please try again later.");
        }
    }

    [HttpGet("blotter")]
    public async Task<IActionResult> GetBlotterData()
    {
        try
        {
            var blotterData = await _blotterScraper.ScrapeAsync();
            return Ok(blotterData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blotter data");
            return StatusCode(500, "An error occurred while retrieving blotter data. Please try again later.");
        }
    }
}
