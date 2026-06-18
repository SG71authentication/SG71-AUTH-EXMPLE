using SG71AuthClient;

namespace SG71Panel
{
    /// <summary>Edit to match your SG71 dashboard Application Manager settings.</summary>
    public static class AuthConfig
    {
        public const string AdminId = "YOUR_ADMIN_ID";
        public const string AppName = IntegrityGuard.ExpectedAppName;
        /// <summary>Must match App Version in Application Manager exactly.</summary>
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
