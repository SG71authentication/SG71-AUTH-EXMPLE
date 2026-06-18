using System;
using System.Threading.Tasks;
using SG71AuthClient;
using SG71Panel;

namespace SG71Panel
{
    /// <summary>Minimal panel startup — matches live API at sg71auth.netlify.app.</summary>
    internal static class Program
    {
        private static async Task<int> Main()
        {
            AuthConfig.Apply();

            var client = new SG71Client(AuthConfig.AdminId, AuthConfig.AppName, AuthConfig.AppVersion);

            // Exit codes: 0 OK | 1 local hash | 2 init fail | 3 version | 4 app name
            var code = await client.InitializeWithGuardAsync(IntegrityGuard.ExpectedAppName);
            if (code != 0)
            {
                Console.Error.WriteLine($"Initialize failed (exit {code}). Check AuthConfig and dashboard App Version.");
                return code;
            }

            Console.WriteLine("Initialized — ready for login.");
            Console.Write("Username: ");
            var user = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Password: ");
            var pass = ReadLineMasked();

            var login = await client.Login(user, pass);
            if (!login.IsOk)
            {
                Console.Error.WriteLine(login.GetDisplayMessage());
                if (!string.IsNullOrEmpty(login.ErrorCode))
                    Console.Error.WriteLine("Code: " + login.ErrorCode);
                return 2;
            }

            Console.WriteLine($"Logged in: {login.User?.Username} (expires: {login.User?.Expires ?? "—"})");
            return 0;
        }

        private static string ReadLineMasked()
        {
            var sb = new System.Text.StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
                if (key.Key == ConsoleKey.Backspace && sb.Length > 0) { sb.Length--; continue; }
                if (!char.IsControl(key.KeyChar)) sb.Append(key.KeyChar);
            }
            return sb.ToString();
        }
    }
}
