using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Text;

namespace CRDWebAdminAPi.Services
{
    public class AuthenticatedSession
    {
        private string baseUrl;
        private string loginUrl;
        private string logoutUrl;
        private HttpClient httpClient;
        private HttpClientHandler handler;
        private Dictionary<string, string> payload;
        private Dictionary<string, string> loginHeaders;
        private CookieContainer cookieContainer;

        public AuthenticatedSession(string baseUrl, string username, string password)
        {
            this.baseUrl = baseUrl;
            this.loginUrl = $"{baseUrl}/crts/j_security_check";
            this.logoutUrl = $"{baseUrl}/crts/logout.do";
            this.cookieContainer = new CookieContainer();
            this.cookieContainer.Add(new Uri(baseUrl), new Cookie("vtf", "byg"));


            // Configure HTTP client handler with cookie management and SSL bypass
            this.handler = new HttpClientHandler()
            {
                CookieContainer = this.cookieContainer,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }, // Ignore SSL errors,
                AllowAutoRedirect = false,
            };
            this.httpClient = new HttpClient(handler);

            // Set base address
            this.httpClient.BaseAddress = new Uri(baseUrl);

            // Set initial cookies

            // Initialize payload
            this.payload = new Dictionary<string, string>()
            {
                { "j_username", username },
                { "j_password", password },
                { "j_application", "Web_Admin" }
            };

            // Initialize headers
            this.loginHeaders = new Dictionary<string, string>()
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Cache-Control", "no-cache" },
                { "Connection", "keep-alive" },
                { "Content-Type", "application/x-www-form-urlencoded" },
                { "DNT", "1" },
                { "Origin", baseUrl },
                { "Pragma", "no-cache" },
                { "Referer", $"{baseUrl}/crts/" },
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "same-origin" },
                { "Sec-Fetch-User", "?1" },
                { "Upgrade-Insecure-Requests", "1" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" },
                { "sec-ch-ua", "\"Microsoft Edge\";v=\"125\", \"Chromium\";v=\"125\", \"Not.A/Brand\";v=\"24\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "Windows" }
            };
        }

        // Method to perform login
        public async Task<bool> Login()
        {
            try
            {
                // Perform a GET request to the login page
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{this.baseUrl}/crts/");
                foreach (var header in this.loginHeaders)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();

                // Get cookies from the response
                //var cookies = response.Headers.GetValues("Set-Cookie");
                //if (cookies != null && cookies.Any())
                //{
                //    foreach (var cookie in cookies)
                //    {
                //        httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
                //    }
                //}
                //else
                //{
                //    Console.WriteLine("No cookies received in the response.");
                //}

                httpClient.DefaultRequestHeaders.Add("Cookie", "vtf=byg");

                // Parse hidden fields from the login page
                var hiddenFields = ExtractHiddenFields(responseContent);

                foreach (var field in hiddenFields)
                {
                    if (field.Key != "j_username")
                    {
                        this.payload[field.Key] = field.Value;
                    }
                }

                // Prepare POST request for login
                var loginRequest = new HttpRequestMessage(HttpMethod.Post, this.loginUrl);
                foreach (var header in this.loginHeaders)
                {
                    loginRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                loginRequest.Content = new FormUrlEncodedContent(this.payload);

                var loginResponse = await httpClient.SendAsync(loginRequest);

                if (loginResponse.StatusCode == HttpStatusCode.RedirectMethod)
                {
                    var redirectUrl = loginResponse.Headers.Location;
                    if (redirectUrl != null && !string.IsNullOrEmpty(redirectUrl.ToString()) && !redirectUrl.IsAbsoluteUri)
                    {
                        redirectUrl = new Uri(this.baseUrl + redirectUrl.ToString());
                    }

                    var redirectRequest = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
                    foreach (var header in this.loginHeaders)
                    {
                        redirectRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    var redirectResponse = await httpClient.SendAsync(redirectRequest);
                    redirectResponse.EnsureSuccessStatusCode();

                    Console.WriteLine($"Login successful, redirected to: {redirectUrl}");
                }
                else
                {
                    Console.WriteLine($"Login failed with status code: {loginResponse.StatusCode}");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Login request failed: {e.Message}");
                return false;
            }
        }

        // Method to perform logout
        public async Task Logout()
        {
            try
            {
                var logoutRequest = new HttpRequestMessage(HttpMethod.Get, this.logoutUrl);
                foreach (var header in this.loginHeaders)
                {
                    logoutRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var logoutResponse = await httpClient.SendAsync(logoutRequest);
                logoutResponse.EnsureSuccessStatusCode();

                Console.WriteLine("Logout successful.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Logout request failed: {e.Message}");
            }
        }

        // Method to perform GET requests
        public async Task<HttpResponseMessage> Get(string url)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in this.loginHeaders)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                // Check if the content is compressed
                if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                {
                    // Decompress the content
                    using (var compressed = await response.Content.ReadAsStreamAsync())
                    using (var decompressed = new GZipStream(compressed, CompressionMode.Decompress))
                    using (var reader = new StreamReader(decompressed))
                    {
                        var decompressedContent = await reader.ReadToEndAsync();
                        // Create a new HttpResponseMessage with decompressed content
                        var newResponse = new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(decompressedContent, Encoding.UTF8, "text/html")
                        };
                        // Copy headers from the original response
                        foreach (var header in response.Headers)
                        {
                            newResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                        return newResponse;
                    }
                }
                else
                {
                    return response;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"GET request failed: {e.Message}");
                return null;
            }
        }

        // Method to perform POST requests
        public async Task<HttpResponseMessage> Post(string url, Dictionary<string, string> data)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                foreach (var header in this.loginHeaders)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                requestMessage.Content = new FormUrlEncodedContent(data);

                var response = await httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine($"POST request failed: {e.Message}");
                return null;
            }
        }

        // Method to extract hidden fields from HTML content
        private Dictionary<string, string> ExtractHiddenFields(string htmlContent)
        {
            var hiddenFields = new Dictionary<string, string>();

            // Regex to extract input fields with type="hidden"
            var regex = new Regex("<input[^>]+type=[\"']hidden[\"'][^>]*>", RegexOptions.IgnoreCase);

            var matches = regex.Matches(htmlContent);

            foreach (Match match in matches)
            {
                var inputTag = match.Value;
                var nameMatch = Regex.Match(inputTag, "name=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);
                var valueMatch = Regex.Match(inputTag, "value=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase);

                if (nameMatch.Success)
                {
                    var name = nameMatch.Groups[1].Value;
                    var value = valueMatch.Success ? valueMatch.Groups[1].Value : "";
                    hiddenFields[name] = value;
                }
            }

            return hiddenFields;
        }
    }
}