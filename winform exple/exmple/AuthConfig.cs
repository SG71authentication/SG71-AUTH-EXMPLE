using SG71AuthClient;

namespace exmple
{
    /// <summary>Edit these values to match your SG71 Auth panel application.</summary>
    public static class AuthConfig
    {
        public const string AdminId = "YOUR_ADMIN_ID";
        public const string AppName = "Your App Name";
        /// <summary>Must match App Version in Application Manager (checked via API).</summary>
        public const string AppVersion = "1.0";

#if DEBUG
        public static string ApiBaseUrl = SG71Client.LocalDevApiDirectUrl;
#else
        public static string ApiBaseUrl = SG71Client.ProductionApiBaseUrl;
#endif

        public static void Apply()
        {
            var fromEnv = Environment.GetEnvironmentVariable("SG71_API_URL");
            SG71Client.ApiBaseUrl = SG71Client.NormalizeApiBaseUrl(
                string.IsNullOrWhiteSpace(fromEnv) ? ApiBaseUrl : fromEnv);
        }
    }
}
