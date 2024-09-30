using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Vis.TestMode.Services.CRDWebAdmin
{
    public class LogCollectorService
    {
        private readonly AuthenticatedSession _authenticatedSession;
        private readonly ServerNamesHelper _serverNamesHelper;

        public LogCollectorService(string baseUrl, string username, string password)
        {
            _authenticatedSession = new AuthenticatedSession(baseUrl, username, password);
            _serverNamesHelper = new ServerNamesHelper(_authenticatedSession);
        }

        public async Task<List<LogFile>> CollectLogsAsync()
        {
            Console.WriteLine("Starting log collection");
            var logFilesCollected = new List<LogFile>();

            if (await _authenticatedSession.LoginAsync())
            {
                Console.WriteLine("Logged in successfully");
                var serverNames = await _serverNamesHelper.GetServerNamesAsync();
                foreach (var serverName in serverNames)
                {
                    var logUrl = $"/crts/diagnostics/logview.do?fileName=cr_server.html&serverId={serverName}";
                    var response = await _authenticatedSession.GetAsync(logUrl);

                    if (response != null)
                    {
                        var timestamp = DateTime.Now.ToString("dd_MM_yy_HH_mm");
                        var filename = $"./static/logs/{serverName}_server_{timestamp}.html";

                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                        await File.WriteAllTextAsync(filename, await response.Content.ReadAsStringAsync());

                        Console.WriteLine($"Saved log for {serverName} as {filename}");

                        logFilesCollected.Add(new LogFile
                        {
                            Filename = filename,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "Josh Smith"
                        });
                    }
                    else
                    {
                        Console.WriteLine($"No response for {serverName}");
                    }
                }

                await _authenticatedSession.LogoutAsync();
                return logFilesCollected;
            }
            else
            {
                Console.WriteLine("Failed to login");
                return new List<LogFile>();
            }
        }
    }

    public class LogFile
    {
        public string Filename { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
    
}