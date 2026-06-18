using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SG71AuthClient
{
    public class SG71Client
    {
        public const string ClientVersion = "2.1";

        public static class user_data
        {
            public static UserData CurrentUser { get; set; }
            public static bool IsLoggedIn => CurrentUser != null;
        }

        private static readonly HttpClient client = CreateHttpClient();
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public const string ProductionApiBaseUrl = "https://sg71auth.netlify.app/api";
        public const string LocalDevApiBaseUrl = "http://localhost:5173/api";
        public const string LocalDevApiDirectUrl = "http://localhost:3000/api";

        public static string ApiBaseUrl { get; set; } = ResolveApiBaseUrl();

        private readonly string _adminId;
        private readonly string _appName;
        private readonly string _appVersion;

        /// <summary>Last human-readable message from the API or transport layer.</summary>
        public string Response { get; private set; }

        public bool IsSuccess { get; private set; }
        public int StatusCode { get; private set; }
        public UserData UserInfo { get; private set; }
        public UpdateInfo LastUpdateInfo { get; private set; }

        /// <summary>Full parsed response from the last API call.</summary>
        public ApiResponse LastResponse { get; private set; }

        /// <summary>URL of the last API request.</summary>
        public string LastRequestUrl { get; private set; }

        public SG71Client(string adminId, string appName, string appVersion)
        {
            _adminId = adminId ?? throw new ArgumentNullException(nameof(adminId));
            _appName = appName ?? throw new ArgumentNullException(nameof(appName));
            _appVersion = string.IsNullOrWhiteSpace(appVersion) ? "1.0" : appVersion.Trim();
        }

        private static string ResolveApiBaseUrl() =>
            NormalizeApiBaseUrl(
                Environment.GetEnvironmentVariable("SG71_API_URL") ?? ProductionApiBaseUrl);

        /// <summary>Ensures base URL ends with /api (avoids receiving Vite index.html).</summary>
        public static string NormalizeApiBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return LocalDevApiDirectUrl;

            var normalized = url.Trim().TrimEnd('/');
            if (normalized.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                return normalized;

            if (normalized.EndsWith("/api/", StringComparison.OrdinalIgnoreCase))
                return normalized.TrimEnd('/');

            return normalized + "/api";
        }

        public static string BuildApiUrl(string endpoint)
        {
            var baseUrl = NormalizeApiBaseUrl(ApiBaseUrl);
            var path = endpoint.StartsWith("/") ? endpoint : "/" + endpoint;
            return baseUrl + path;
        }

        private static HttpClient CreateHttpClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SG71Client/" + ClientVersion);
            return http;
        }

        public Task<ApiResponse> CheckForUpdateAsync() =>
            PostAsync("/app/check-update", new
            {
                adminId = _adminId,
                appName = _appName,
                appVersion = _appVersion
            });

        public async Task<ApiResponse> Initialize()
        {
            var hash = GetAppHash();
            var response = await PostAsync("/init", new
            {
                adminId = _adminId,
                appName = _appName,
                appVersion = _appVersion,
                appHash = hash
            });

            if (response.Success && !string.IsNullOrWhiteSpace(hash) && hash.Length == 64)
                _ = SendAppHashAsync(hash);

            return response;
        }

        public async Task<bool> InitializeWithAutoUpdateAsync(string downloadPath)
        {
            var init = await Initialize();
            if (init.Success) return true;
            if (!init.UpdateRequired || string.IsNullOrWhiteSpace(init.UpdateUrl))
                return false;
            return await DownloadUpdateAsync(init.UpdateUrl, downloadPath);
        }

        public Task<ApiResponse> Login(string username, string password) =>
            PostAsync("/login", new
            {
                adminId = _adminId,
                appName = _appName,
                username,
                password,
                hwid = GetHWID(),
                appHash = GetAppHash(),
                appVersion = _appVersion
            });

        public Task<ApiResponse> Register(string username, string password, string licenseKey) =>
            PostAsync("/register", new
            {
                adminId = _adminId,
                appName = _appName,
                username,
                password,
                licenseKey
            });

        public static async Task<bool> DownloadUpdateAsync(string updateUrl, string destinationPath, IProgress<int> progress = null)
        {
            var result = await DownloadUpdateWithMessageAsync(updateUrl, destinationPath, progress);
            return result.ok;
        }

        public static async Task<(bool ok, string message)> DownloadUpdateWithMessageAsync(
            string updateUrl,
            string destinationPath,
            IProgress<int> progress = null)
        {
            if (string.IsNullOrWhiteSpace(updateUrl))
                return (false, "Update URL is empty.");
            if (string.IsNullOrWhiteSpace(destinationPath))
                return (false, "Destination path is empty.");

            try
            {
                var dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var response = await client.GetAsync(updateUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                        return (false, $"Download failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    progress?.Report(0);

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file = File.Create(destinationPath))
                    {
                        var buffer = new byte[81920];
                        long bytesRead = 0;
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, read);
                            bytesRead += read;
                            if (totalBytes > 0)
                            {
                                var pct = (int)Math.Min(100, bytesRead * 100 / totalBytes);
                                progress?.Report(pct);
                            }
                        }
                    }

                    if (totalBytes <= 0)
                        progress?.Report(100);
                }
                return (true, "Update downloaded successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Download error: " + ex.Message);
            }
        }

        public static string GetHWID()
        {
            if (!string.IsNullOrEmpty(_hwidCache))
                return _hwidCache;

            var persisted = ReadPersistedHwid();
            if (!string.IsNullOrEmpty(persisted))
            {
                _hwidCache = persisted;
                return _hwidCache;
            }

            try
            {
                // MachineGuid + Windows user SID only — stable with VPN (no MAC/IP/hostname).
                var raw = string.Join("|", new[]
                {
                    GetMachineGuid(),
                    WindowsIdentity.GetCurrent().User?.Value ?? "NOSID"
                });

                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                _hwidCache = BitConverter.ToString(hash).Replace("-", "").Substring(0, 32).ToLowerInvariant();
                WritePersistedHwid(_hwidCache);
                return _hwidCache;
            }
            catch
            {
                return "HWID-ERROR";
            }
        }

        private static string _hwidCache;

        public static void ClearHwidCache()
        {
            _hwidCache = null;
            try
            {
                var path = GetHwidStorePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        private static string GetHwidStorePath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SG71Auth");
            return Path.Combine(dir, "hwid.txt");
        }

        private static string ReadPersistedHwid()
        {
            try
            {
                var path = GetHwidStorePath();
                if (!File.Exists(path)) return null;
                var value = File.ReadAllText(path).Trim().ToLowerInvariant();
                return value.Length == 32 ? value : null;
            }
            catch { return null; }
        }

        private static void WritePersistedHwid(string hwid)
        {
            try
            {
                var path = GetHwidStorePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, hwid);
            }
            catch { }
        }

        private static string GetMachineGuid()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid") as string ?? "";
            }
            catch { return ""; }
        }

        /// <summary>Path to the running .exe (works when Assembly.Location is empty).</summary>
        public static string GetExecutablePath()
        {
            try
            {
#if !NETFRAMEWORK
                if (!string.IsNullOrEmpty(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
                    return Path.GetFullPath(Environment.ProcessPath);
#endif
                var main = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(main) && File.Exists(main))
                    return Path.GetFullPath(main);

                var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                if (!string.IsNullOrEmpty(entry) && File.Exists(entry))
                    return Path.GetFullPath(entry);

                var executing = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(executing) && File.Exists(executing))
                    return Path.GetFullPath(executing);
            }
            catch { }
            return null;
        }

        public static string GetAppHashFromPath(string appPath)
        {
            try
            {
                if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
                    return string.Empty;

                using var sha = SHA256.Create();
                using var stream = File.OpenRead(appPath);
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch { return string.Empty; }
        }

        public string GetAppHash() => GetAppHashFromPath(GetExecutablePath());

        /// <summary>
        /// Panel-style init: local integrity checks then /init.
        /// Returns 0 on success; 1 local hash, 2 init failed, 3 version mismatch, 4 app name mismatch.
        /// </summary>
        public async Task<int> InitializeWithGuardAsync(string expectedAppName, string allowedExeSha256HexLower = null)
        {
            if (!IntegrityGuard.VerifyLocalBuild(allowedExeSha256HexLower))
                return 1;

            var name = _appName?.Trim() ?? string.Empty;
            if (!string.Equals(name, expectedAppName?.Trim(), StringComparison.Ordinal))
                return 4;

            var response = await Initialize();
            if (response.UpdateRequired)
                return 3;
            return response.IsOk ? 0 : 2;
        }

        private async Task<ApiResponse> PostAsync(string endpoint, object data)
        {
            var url = BuildApiUrl(endpoint);
            LastRequestUrl = url;

            try
            {
                var json = JsonConvert.SerializeObject(data);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var httpResponse = await client.PostAsync(url, content);
                var body = await httpResponse.Content.ReadAsStringAsync();

                var apiResponse = ApiResponse.Parse(body, (int)httpResponse.StatusCode, httpResponse.IsSuccessStatusCode);
                ApplyResponseState(apiResponse, endpoint);
                return apiResponse;
            }
            catch (TaskCanceledException)
            {
                return ApplyResponseState(ApiResponse.Fail(0, "Request timed out. Check API URL and network.", url), endpoint);
            }
            catch (HttpRequestException ex)
            {
                return ApplyResponseState(ApiResponse.Fail(0, "HTTP error: " + ex.Message, url), endpoint);
            }
            catch (Exception ex)
            {
                return ApplyResponseState(ApiResponse.Fail(0, "Connection error: " + ex.Message, url), endpoint);
            }
        }

        private ApiResponse ApplyResponseState(ApiResponse apiResponse, string endpoint)
        {
            LastResponse = apiResponse;
            StatusCode = apiResponse.StatusCode;
            Response = apiResponse.GetDisplayMessage();
            IsSuccess = apiResponse.IsOk;
            UserInfo = apiResponse.User;
            LastUpdateInfo = apiResponse.UpdateRequired
                ? new UpdateInfo
                {
                    RequiredVersion = apiResponse.RequiredVersion,
                    ClientVersion = apiResponse.ClientVersion,
                    UpdateUrl = apiResponse.UpdateUrl,
                    Message = apiResponse.Message,
                    ForceUpdate = apiResponse.ForceUpdate
                }
                : null;

            if (IsSuccess && endpoint == "/login" && UserInfo != null)
                user_data.CurrentUser = UserInfo;

            return apiResponse;
        }

        public async Task<ApiResponse> SendAppHashAsync(string hash)
        {
            return await PostAsync("/app/update-hash", new
            {
                adminId = _adminId,
                appName = _appName,
                appHash = hash
            });
        }
    }

    public class ApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("user")]
        public UserData User { get; set; }

        [JsonProperty("updateRequired")]
        public bool UpdateRequired { get; set; }

        [JsonProperty("upToDate")]
        public bool UpToDate { get; set; }

        [JsonProperty("requiredVersion")]
        public string RequiredVersion { get; set; }

        [JsonProperty("clientVersion")]
        public string ClientVersion { get; set; }

        [JsonProperty("updateUrl")]
        public string UpdateUrl { get; set; }

        [JsonProperty("forceUpdate")]
        public bool ForceUpdate { get; set; }

        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("generatedPassword")]
        public string GeneratedPassword { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonIgnore]
        public int StatusCode { get; set; }

        [JsonIgnore]
        public string RawBody { get; set; }

        [JsonIgnore]
        public string RequestUrl { get; set; }

        [JsonIgnore]
        public bool HttpSuccess { get; set; }

        /// <summary>True when HTTP 2xx and success is true (not an update-required 426).</summary>
        [JsonIgnore]
        public bool IsOk => HttpSuccess && Success && !UpdateRequired;

        public static ApiResponse Parse(string body, int statusCode, bool httpSuccess)
        {
            var result = new ApiResponse
            {
                StatusCode = statusCode,
                RawBody = body ?? "",
                HttpSuccess = httpSuccess
            };

            if (string.IsNullOrWhiteSpace(body))
            {
                result.Message = httpSuccess
                    ? "Empty response from server."
                    : $"HTTP {statusCode} with no response body.";
                result.Success = false;
                return result;
            }

            if (!body.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                result.Message = LooksLikeHtml(body)
                    ? "API returned a web page (HTML), not JSON. " +
                      "Use ApiBaseUrl ending with /api (e.g. http://localhost:3000/api). " +
                      "Run: npm run dev:all or npm run start:api"
                    : Truncate(body, 300);
                result.Success = false;
                return result;
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<ApiResponse>(body, SG71Client.JsonSettings)
                             ?? new ApiResponse();
                result.Success = parsed.Success;
                result.Message = parsed.Message;
                result.User = NormalizeUser(parsed.User, body);
                result.UpdateRequired = parsed.UpdateRequired;
                result.UpToDate = parsed.UpToDate;
                result.RequiredVersion = parsed.RequiredVersion;
                result.ClientVersion = parsed.ClientVersion;
                result.UpdateUrl = parsed.UpdateUrl;
                result.ForceUpdate = parsed.ForceUpdate;
                result.Uid = parsed.Uid;
                result.Slug = parsed.Slug;
                result.GeneratedPassword = parsed.GeneratedPassword;
                result.Error = parsed.Error;
                result.ErrorCode = parsed.ErrorCode;

                if (string.IsNullOrWhiteSpace(result.Message))
                    result.Message = ReadToken(body, "message", "error", "detail");

                if (statusCode == 426 && !result.UpdateRequired)
                    result.UpdateRequired = true;

                if (result.UpdateRequired && string.IsNullOrWhiteSpace(result.Message))
                    result.Message = $"Update required. Server version: {result.RequiredVersion ?? "?"}.";

                if (!result.Success && string.IsNullOrWhiteSpace(result.Message))
                    result.Message = DefaultMessageForStatus(statusCode);

                return result;
            }
            catch (Exception ex)
            {
                result.Message = "Invalid JSON response: " + ex.Message;
                result.Success = false;
                return result;
            }
        }

        public static ApiResponse Fail(int statusCode, string message, string requestUrl = null) =>
            new ApiResponse
            {
                StatusCode = statusCode,
                Message = message,
                Success = false,
                HttpSuccess = false,
                RequestUrl = requestUrl
            };

        public string GetDisplayMessage()
        {
            if (!string.IsNullOrWhiteSpace(Message))
                return Message.Trim();
            if (!string.IsNullOrWhiteSpace(Error))
                return Error.Trim();
            if (UpdateRequired)
                return $"Update required (HTTP {StatusCode}). Version {RequiredVersion ?? "?"}.";
            if (UpToDate)
                return "Application is up to date.";
            if (Success && StatusCode > 0)
                return $"Success (HTTP {StatusCode}).";
            return StatusCode > 0 ? $"HTTP {StatusCode}" : "Unknown error.";
        }

        public override string ToString() => $"[{StatusCode}] {(Success ? "OK" : "FAIL")}: {GetDisplayMessage()}";

        private static UserData NormalizeUser(UserData user, string json)
        {
            if (user != null && !string.IsNullOrWhiteSpace(user.Username))
                return user;

            try
            {
                var jo = JObject.Parse(json);
                var u = jo["user"] ?? jo["User"];
                if (u == null) return user;

                return new UserData
                {
                    Username = (string)(u["Username"] ?? u["username"]),
                    Expires = (string)(u["Expires"] ?? u["expires"]),
                    HWID = (string)(u["HWID"] ?? u["hwid"]),
                    IsBanned = u["IsBanned"]?.Value<bool>() ?? u["isBanned"]?.Value<bool>() ?? false
                };
            }
            catch
            {
                return user;
            }
        }

        private static string ReadToken(string json, params string[] keys)
        {
            try
            {
                var jo = JObject.Parse(json);
                foreach (var key in keys)
                {
                    var val = jo[key]?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            catch { }
            return null;
        }

        private static string DefaultMessageForStatus(int code) => code switch
        {
            400 => "Bad request.",
            401 => "Unauthorized.",
            403 => "Forbidden.",
            404 => "Not found.",
            405 => "Method not allowed.",
            426 => "Update required.",
            500 => "Internal server error.",
            503 => "Service unavailable.",
            _ => $"HTTP {code}."
        };

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max) + "…";

        private static bool LooksLikeHtml(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return false;
            var t = body.TrimStart();
            return t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class UpdateInfo
    {
        public string RequiredVersion { get; set; }
        public string ClientVersion { get; set; }
        public string UpdateUrl { get; set; }
        public string Message { get; set; }
        public bool ForceUpdate { get; set; }

        public override string ToString() =>
            $"{Message} (required: {RequiredVersion}, yours: {ClientVersion})";
    }

    public class UserData
    {
        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Expires")]
        public string Expires { get; set; }

        [JsonProperty("HWID")]
        public string HWID { get; set; }

        [JsonProperty("IsBanned")]
        public bool IsBanned { get; set; }

        public override string ToString() => $"{Username} (expires: {Expires})";
    }
}
