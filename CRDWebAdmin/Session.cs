using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class AuthenticatedSession
{
    private readonly string _baseUrl;
    private readonly string _loginUrl;
    private readonly string _logoutUrl;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _payload;

    public AuthenticatedSession(string baseUrl, string username, string password)
    {
        _baseUrl = baseUrl;
        _loginUrl = $"{baseUrl}/crts/j_security_check";
        _logoutUrl = $"{baseUrl}/crts/logout.do";

        // Initialize HttpClient with handler to ignore SSL certificate verification
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);

        _payload = new Dictionary<string, string>
        {
            { "j_username", username },
            { "j_password", password },
            { "j_application", "Web_Admin" }
        };

        // Set initial cookies
        _httpClient.DefaultRequestHeaders.Add("Cookie", "vtf=byg");

        // Set login headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
        _httpClient.DefaultRequestHeaders.Add("Host", baseUrl.Replace("https://", "").Replace("http://", ""));
        _httpClient.DefaultRequestHeaders.Add("Origin", baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{baseUrl}/crts/");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Microsoft Edge\";v=\"125\", \"Chromium\";v=\"125\", \"Not.A/Brand\";v=\"24\"");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "Windows");
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            var loginPageResponse = await _httpClient.GetAsync($"{_baseUrl}/crts/");
            loginPageResponse.EnsureSuccessStatusCode();

            // Parse the login page HTML
            var pageContent = await loginPageResponse.Content.ReadAsStringAsync();

            // Extract hidden fields using a Regex
            var hiddenFieldPattern = new Regex("<input[^>]*type=['\"]hidden['\"][^>]*name=['\"]([^'\"]*)['\"][^>]*value=['\"]([^'\"]*)['\"][^>]*>");
            var matches = hiddenFieldPattern.Matches(pageContent);

            foreach (Match match in matches)
            {
                var fieldName = match.Groups[1].Value;
                var fieldValue = match.Groups[2].Value;

                if (fieldName != "j_username")
                {
                    _payload[fieldName] = fieldValue;
                }
            }

            // Perform the login request
            var content = new FormUrlEncodedContent(_payload);
            var loginResponse = await _httpClient.PostAsync(_loginUrl, content);

            if (loginResponse.StatusCode == HttpStatusCode.Redirect || loginResponse.StatusCode == HttpStatusCode.SeeOther)
            {
                var redirectUrl = _baseUrl + loginResponse.Headers.Location;
                var redirectResponse = await _httpClient.GetAsync(redirectUrl);
                Console.WriteLine($"Login successful, redirected to: {redirectUrl}");
            }
            else
            {
                Console.WriteLine($"Login failed with status code: {loginResponse.StatusCode}");
                return false;
            }

            return true;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Login request failed: {e.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var logoutResponse = await _httpClient.GetAsync(_logoutUrl);
            logoutResponse.EnsureSuccessStatusCode();
            Console.WriteLine("Logout successful.");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Logout request failed: {e.Message}");
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        try
        {
            var fullUrl = $"{_baseUrl}{url}";
            var response = await _httpClient.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"GET request successful: {fullUrl}");
            return response;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"GET request failed: {e.Message}");
            return null;
        }
    }

    public async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> data)
    {
        try
        {
            var fullUrl = $"{_baseUrl}{url}";
            var content = new FormUrlEncodedContent(data);
            var response = await _httpClient.PostAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"POST request successful: {fullUrl}");
            return response;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"POST request failed: {e.Message}");
            return null;
        }
    }
}