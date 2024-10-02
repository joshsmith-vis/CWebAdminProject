using System.Text;
using System.Text.RegularExpressions;
namespace CRDWebAdminAPi.Services {
    public class ServerNames
    {
        private AuthSession authenticatedSession;

        public ServerNames(AuthSession authenticatedSession)
        {
            this.authenticatedSession = authenticatedSession;
        }

        public async Task<List<string>> GetServerNames()
        {
            var response = await this.authenticatedSession.Get("/crts/db/main.do");
            if (response != null)
            {
                string content = await response.Content.ReadAsStringAsync();

                // Existing parsing logic
                var clusterListRegex = new Regex("<ul[^>]*id=[\"']clusterList[\"'][^>]*>(.*?)</ul>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var match = clusterListRegex.Match(content);

                if (match.Success)
                {
                    var ulContent = match.Groups[1].Value;

                    // Extract the li elements
                    var liRegex = new Regex("<li[^>]*>(.*?)</li>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    var liMatches = liRegex.Matches(ulContent);

                    List<string> serverNames = new List<string>();

                    foreach (Match liMatch in liMatches)
                    {
                        var liContent = liMatch.Groups[1].Value;

                        // Remove any HTML tags from liContent
                        var serverName = StripHtml(liContent).Trim();

                        if (!string.IsNullOrEmpty(serverName))
                        {
                            serverNames.Add(serverName);
                        }
                    }

                    return serverNames;
                }
                else
                {
                    Console.WriteLine("No clusterList elements found");
                }
            }
            return new List<string>();
        }

        // Helper method to strip HTML tags
        private string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}