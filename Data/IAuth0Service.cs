using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace QuizManager.Data
{
    public interface IAuth0Service
    {
        Task<Auth0UserDetails> GetUserDetailsAsync(string name, string userId = null);
        Task<Auth0UserDetails> GetUserDetailsByEmailAsync(string name);
        Task<string> GetUserBrowserInfoAsync(string userId);
        Task<List<Auth0User>> GetUsersByQueryAsync(string query);
        Task<string> GetUserIdByEmailRawAsync(string email);
        Task<bool> ResendVerificationEmailAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);

        // --- NEW METHOD ---
        Task<bool> VerifyUserPasswordAsync(string email, string password);
        Task<bool> UpdateUserPasswordAsync(string userId, string newPassword);
    }

    public class Auth0Service : IAuth0Service
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public Auth0Service(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        // --- NEW PASSWORD VERIFICATION IMPLEMENTATION ---
        // Inside Auth0Service.cs

        public async Task<bool> VerifyUserPasswordAsync(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var domain = _configuration["Auth0:Domain"];
                var url = $"https://{domain}/oauth/token";

                var requestBody = new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = email,
                    ["password"] = password,
                    ["client_id"] = _configuration["Auth0:ClientId"],
                    ["client_secret"] = _configuration["Auth0:ClientSecret"],
                    ["scope"] = "openid"
                    // Removed ["realm"] because we set "Default Directory" in Tenant Settings (Step 2)
                };

                var response = await client.PostAsync(url, new FormUrlEncodedContent(requestBody));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"--------------------------------------------------");
                    Console.WriteLine($"[PASSWORD VERIFICATION FAILED]");
                    Console.WriteLine($"Status: {response.StatusCode}");
                    Console.WriteLine($"Error Body: {errorContent}");
                    Console.WriteLine($"--------------------------------------------------");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] VerifyPassword: {ex.Message}");
                return false;
            }
        }

        // --- DELETE USER ---
        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();

                // Auth0 IDs often contain '|', which needs encoding to '%7C'
                var encodedId = Uri.EscapeDataString(userId).Replace("|", "%7C");

                var response = await client.DeleteAsync($"users/{encodedId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Auth0 Delete Error]: {response.StatusCode} - {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] DeleteUserAsync: {ex.Message}");
                return false;
            }
        }

        // --- EMAIL VERIFICATION LOGIC ---

        public async Task<string> GetUserIdByEmailRawAsync(string email)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();

                var query = $"email:\"{email}\"";
                var url = $"users?q={Uri.EscapeDataString(query)}&search_engine=v3";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Auth0 Error] GetUserIdRaw: {response.StatusCode} - {err}");
                    return null;
                }

                var users = await response.Content.ReadFromJsonAsync<List<Auth0User>>();
                var user = users?.FirstOrDefault();

                if (user == null)
                {
                    Console.WriteLine($"[Auth0] User not found via Search query: {email}");
                    return null;
                }

                return user.UserId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] GetUserIdByEmailRawAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ResendVerificationEmailAsync(string userId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = "jobs/verification-email";

                var requestBody = new { user_id = userId };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Auth0 Job Error]: {response.StatusCode} - {err}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] ResendVerificationEmailAsync: {ex.Message}");
                return false;
            }
        }

        // --- USER LOOKUP LOGIC ---

        public async Task<Auth0UserDetails> GetUserDetailsByEmailAsync(string email)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var query = $"email:\"{email}\"";
                var url = $"users?q={Uri.EscapeDataString(query)}&search_engine=v3";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Auth0 Error] GetUserDetails: {response.StatusCode} - {err}");
                    return null;
                }

                var users = await response.Content.ReadFromJsonAsync<List<Auth0User>>();

                // Use the most recently active user if duplicates exist
                var user = users?.OrderByDescending(u => u.LastLogin ?? u.CreatedAt).FirstOrDefault();

                if (user == null) return null;

                var logs = await GetUserLogsAsync(client, user.UserId);
                var mostRecentLog = logs?.FirstOrDefault();

                return MapToUserDetails(user, mostRecentLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] GetUserDetailsByEmailAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<Auth0UserDetails> GetUserDetailsAsync(string name, string userId = null)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var client = await GetAuthenticatedClientAsync();
                    var encodedId = Uri.EscapeDataString(userId).Replace("|", "%7C");
                    var userResponse = await client.GetAsync($"users/{encodedId}");

                    if (userResponse.IsSuccessStatusCode)
                    {
                        var user = await userResponse.Content.ReadFromJsonAsync<Auth0User>();
                        var logs = await GetUserLogsAsync(client, userId);
                        var mostRecentLog = logs?.FirstOrDefault();
                        return MapToUserDetails(user, mostRecentLog);
                    }
                }
                catch { /* Ignore ID errors */ }
            }
            return await GetUserDetailsByEmailAsync(name);
        }

        public async Task<List<Auth0User>> GetUsersByQueryAsync(string query)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"users?q={Uri.EscapeDataString(query)}&search_engine=v3";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Auth0User>>() ?? new List<Auth0User>();
                }
                return new List<Auth0User>();
            }
            catch { return new List<Auth0User>(); }
        }

        public async Task<string> GetUserBrowserInfoAsync(string userId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var logs = await GetUserLogsAsync(client, userId);
                return logs?.FirstOrDefault()?.UserAgent ?? "Unknown";
            }
            catch { return "Unknown"; }
        }

        // --- HELPERS ---

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var token = await GetCachedManagementApiTokenAsync();

            // Explicitly set BaseAddress to ensure no conflicts from Program.cs
            var client = _httpClientFactory.CreateClient();
            var managementDomain = _configuration["Auth0:Management:Domain"];
            client.BaseAddress = new Uri($"https://{managementDomain}/api/v2/");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<string> GetCachedManagementApiTokenAsync()
        {
            const string cacheKey = "Auth0_Management_Token";
            if (_cache.TryGetValue(cacheKey, out string cachedToken)) return cachedToken;

            var token = await GetManagementApiTokenAsync();
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(23));
            _cache.Set(cacheKey, token, cacheOptions);

            return token;
        }

        private async Task<string> GetManagementApiTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var managementDomain = _configuration["Auth0:Management:Domain"];
            var url = $"https://{managementDomain}/oauth/token";

            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _configuration["Auth0:Management:ClientId"],
                ["client_secret"] = _configuration["Auth0:Management:ClientSecret"],
                ["audience"] = $"https://{managementDomain}/api/v2/",
                // CRITICAL: Ensure delete:users scope is included here
                ["scope"] = "read:users read:user_idp_tokens read:logs update:users delete:users"
            };

            var response = await client.PostAsync(url, new FormUrlEncodedContent(requestBody));

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Token Request Failed: {response.StatusCode} | {responseBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<Auth0TokenResponse>(json);
            return tokenResponse?.AccessToken;
        }

        private async Task<List<Auth0LogEntry>> GetUserLogsAsync(HttpClient client, string userId)
        {
            try
            {
                var encodedId = Uri.EscapeDataString(userId).Replace("|", "%7C");
                var url = $"users/{encodedId}/logs?per_page=1";
                return await client.GetFromJsonAsync<List<Auth0LogEntry>>(url);
            }
            catch { return new List<Auth0LogEntry>(); }
        }

        private Auth0UserDetails MapToUserDetails(Auth0User user, Auth0LogEntry log)
        {
            return new Auth0UserDetails
            {
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                LoginTimes = user.LoginsCount?.ToString(),
                LastIp = log?.Ip ?? user.LastIpAddress,
                IsEmailVerified = user.EmailVerified,
                LoginBrowser = log?.UserAgent ?? "Unknown",
                IsMobile = log?.IsMobile ?? false,
                LocationInfo = log?.LocationInfo
            };
        }

        // Inside Auth0Service class

        public async Task<bool> UpdateUserPasswordAsync(string userId, string newPassword)
        {
            try
            {
                // 1. Get the client (this already has the Management Token & Base URL)
                var client = await GetAuthenticatedClientAsync();

                // 2. Encode the User ID (crucial for Auth0 IDs containing '|')
                var encodedId = Uri.EscapeDataString(userId).Replace("|", "%7C");

                // 3. Prepare the payload
                var requestBody = new
                {
                    password = newPassword,
                    // 'connection' is usually required to tell Auth0 WHICH database to update.
                    // "Username-Password-Authentication" is the default name. 
                    // If you get a "connection not found" error, check your Auth0 Dashboard -> Authentication -> Database.
                    connection = "Username-Password-Authentication"
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                // 4. Send PATCH request to /api/v2/users/{id}
                var response = await client.PatchAsync($"users/{encodedId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Auth0 Update Password Error]: {response.StatusCode} - {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] UpdateUserPasswordAsync: {ex.Message}");
                return false;
            }
        }


    }

    // --- MODELS ---
    public class Auth0UserDetails { public DateTime? CreatedAt { get; set; } public DateTime? LastLogin { get; set; } public string LoginTimes { get; set; } public string LastIp { get; set; } public bool? IsEmailVerified { get; set; } public string LoginBrowser { get; set; } public bool IsMobile { get; set; } public Models.LocationInfo LocationInfo { get; set; } }
    public class Auth0TokenResponse { [JsonPropertyName("access_token")] public string AccessToken { get; set; } [JsonPropertyName("token_type")] public string TokenType { get; set; } }
    public class Auth0LogEntry { [JsonPropertyName("date")] public DateTime Date { get; set; } [JsonPropertyName("type")] public string Type { get; set; } [JsonPropertyName("user_agent")] public string UserAgent { get; set; } [JsonPropertyName("ip")] public string Ip { get; set; } [JsonPropertyName("isMobile")] public bool IsMobile { get; set; } [JsonPropertyName("location_info")] public Models.LocationInfo LocationInfo { get; set; } }
    public class Auth0User { [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; } [JsonPropertyName("last_login")] public DateTime? LastLogin { get; set; } [JsonPropertyName("logins_count")] public int? LoginsCount { get; set; } [JsonPropertyName("last_ip")] public string LastIpAddress { get; set; } [JsonPropertyName("email_verified")] public bool? EmailVerified { get; set; } [JsonPropertyName("user_id")] public string UserId { get; set; } [JsonPropertyName("email")] public string Email { get; set; } }
}