using System;
using System.IO;

namespace SG71AuthClient
{
    /// <summary>
    /// Optional local checks before calling /init. App version is enforced by the API.
    /// Link this file with SG71Client.cs for panel-style executables.
    /// </summary>
    public static class IntegrityGuard
    {
        /// <summary>Must match app name in dashboard and SG71Client constructor.</summary>
        public const string ExpectedAppName = "Your App Name";

        /// <summary>SHA-256 of release .exe (64 lowercase hex). Empty = skip local hash check.</summary>
        public const string AllowedExeSha256HexLower = "";

        public static bool VerifyLocalBuild(string allowedExeSha256HexLower = null)
        {
            var allowed = allowedExeSha256HexLower ?? AllowedExeSha256HexLower;
            try
            {
                var path = SG71Client.GetExecutablePath();
                var haveFile = !string.IsNullOrEmpty(path) && File.Exists(path);
                if (!haveFile)
                    return string.IsNullOrEmpty(allowed);

                if (!string.IsNullOrEmpty(allowed))
                {
                    if (allowed.Length != 64)
                        return false;
                    var h = SG71Client.GetAppHashFromPath(SG71Client.GetExecutablePath());
                    if (string.IsNullOrEmpty(h) ||
                        !string.Equals(h, allowed, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ComputeExeSha256Hex() =>
            SG71Client.GetAppHashFromPath(SG71Client.GetExecutablePath());

        public static void VerifyAppNameOrExit(string appName)
        {
            var name = appName?.Trim() ?? string.Empty;
            if (!string.Equals(name, ExpectedAppName, StringComparison.Ordinal))
                Environment.Exit(4);
        }

        public static void VerifyLocalBuildOrExit(string allowedExeSha256HexLower = null)
        {
            if (!VerifyLocalBuild(allowedExeSha256HexLower))
                Environment.Exit(1);
        }
    }
}
