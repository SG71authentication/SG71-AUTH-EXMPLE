using SG71AuthClient;

namespace SG71AuthExample;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.Title = "SG71 Auth — C# Example";
        var justUpdated = args.Contains(SelfUpdater.UpdatedFlagArg, StringComparer.OrdinalIgnoreCase);

        AuthConfig.Apply();

        Console.WriteLine("SG71 Auth C# Example");
        Console.WriteLine("====================");
        Console.WriteLine($"API:      {SG71Client.ApiBaseUrl}");
        Console.WriteLine($"App:      {AuthConfig.AppName} v{AuthConfig.AppVersion}");
        Console.WriteLine($"EXE:      {SelfUpdater.GetExecutablePath()}");
        Console.WriteLine($"HWID:     {SG71Client.GetHWID()}");
        if (justUpdated)
            Console.WriteLine("(Restarted after self-update)");
        Console.WriteLine();

        var client = new SG71Client(AuthConfig.AdminId, AuthConfig.AppName, AuthConfig.AppVersion);

        if (!await RunVersionAndInitAsync(client))
            return 1;

        Console.WriteLine("\n1) Login\n2) Register\nChoice [1]: ");
        var choice = Console.ReadLine()?.Trim();
        if (choice == "2")
        {
            if (!await RunRegisterAsync(client))
                return 1;
        }
        else
        {
            if (!await RunLoginAsync(client))
                return 1;
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey(true);
        return 0;
    }

    private static void PrintResponse(string label, ApiResponse r)
    {
        var status = r.IsOk ? "OK" : "FAIL";
        Console.WriteLine($"{label} [{r.StatusCode}] {status}: {r.GetDisplayMessage()}");
        if (!string.IsNullOrWhiteSpace(r.ErrorCode))
            Console.WriteLine($"  Code: {r.ErrorCode}");
    }

    private static async Task<bool> RunVersionAndInitAsync(SG71Client client)
    {
        Console.WriteLine("Checking version from panel...");
        var updateCheck = await client.CheckForUpdateAsync();

        if (updateCheck.UpdateRequired)
        {
            if (!await TryApplyUpdateAsync(updateCheck))
                return false;
        }
        else if (!updateCheck.IsOk)
        {
            PrintResponse("Version check", updateCheck);
        }
        else
        {
            PrintResponse("Version check", updateCheck);
        }

        Console.WriteLine("\nInitializing...");
        var init = await client.Initialize();
        if (init.UpdateRequired)
        {
            if (!await TryApplyUpdateAsync(init))
                return false;
        }

        if (!init.IsOk)
        {
            PrintResponse("Init", init);
            return false;
        }

        PrintResponse("Init", init);
        return true;
    }

    private static async Task<bool> TryApplyUpdateAsync(ApiResponse update)
    {
        PrintResponse("Update", update);
        if (!string.IsNullOrWhiteSpace(update.RequiredVersion))
            Console.WriteLine($"  Server version: {update.RequiredVersion} | Yours: {update.ClientVersion ?? AuthConfig.AppVersion}");

        if (string.IsNullOrWhiteSpace(update.UpdateUrl))
        {
            Console.WriteLine("  No update URL in panel — set Update URL in Application Manager, or match AppVersion in AuthConfig.cs.");
            return !update.ForceUpdate;
        }

        Console.WriteLine($"  Download: {update.UpdateUrl}");
        Console.Write("Download and install update now? (y/n): ");
        var answer = Console.ReadLine()?.Trim();
        if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
            return !update.ForceUpdate;

        Console.WriteLine("\nDownloading update next to current EXE...");
        var (downloaded, newExePath) = await SelfUpdater.DownloadUpdateBesideExeAsync(update.UpdateUrl);
        if (!downloaded)
        {
            Console.WriteLine("Download failed.");
            return false;
        }

        Console.WriteLine($"Saved to: {newExePath}");
        Console.WriteLine("Applying update (old EXE will be replaced)...");
        await Task.Delay(400);
        SelfUpdater.ApplyUpdateAndRestart(newExePath);
        return false;
    }

    private static async Task<bool> RunLoginAsync(SG71Client client)
    {
        Console.Write("\nUsername: ");
        var username = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Password: ");
        var password = ReadPassword();

        Console.WriteLine("\nLogging in...");
        var login = await client.Login(username, password);
        if (!login.IsOk)
        {
            PrintResponse("Login", login);
            return false;
        }

        PrintResponse("Login", login);
        if (login.User != null)
        {
            Console.WriteLine($"  User:    {login.User.Username}");
            Console.WriteLine($"  Expires: {login.User.Expires ?? "—"}");
            Console.WriteLine($"  HWID:    {login.User.HWID ?? SG71Client.GetHWID()}");
        }
        return true;
    }

    private static async Task<bool> RunRegisterAsync(SG71Client client)
    {
        Console.Write("\nUsername: ");
        var username = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Password: ");
        var password = ReadPassword();
        Console.Write("\nLicense key: ");
        var license = Console.ReadLine()?.Trim() ?? "";

        Console.WriteLine("\nRegistering...");
        var reg = await client.Register(username, password, license);
        if (!reg.IsOk)
        {
            PrintResponse("Register", reg);
            return false;
        }

        PrintResponse("Register", reg);
        Console.WriteLine("You can now log in with the same credentials.");
        return true;
    }

    private static string ReadPassword()
    {
        var pwd = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
            {
                pwd.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                pwd.Append(key.KeyChar);
        }
        return pwd.ToString();
    }
}
