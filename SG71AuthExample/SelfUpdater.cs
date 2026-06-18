using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SG71AuthExample;

/// <summary>
/// Downloads an update next to the running EXE, then runs a hidden script that
/// waits for this process to exit, deletes the old EXE, moves the new file into place, and starts it.
/// </summary>
public static class SelfUpdater
{
    public const string UpdatedFlagArg = "--sg71-updated";

    public static string GetExecutablePath()
    {
#if !NETFRAMEWORK
        if (!string.IsNullOrEmpty(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            return Path.GetFullPath(Environment.ProcessPath);
#endif
        var main = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(main) && File.Exists(main))
            return Path.GetFullPath(main);

        var entry = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(entry) && File.Exists(entry))
            return Path.GetFullPath(entry);

        throw new InvalidOperationException("Could not resolve current executable path.");
    }

    /// <summary>
    /// Download update to "{CurrentExeName}.update.exe" in the same folder as the running app.
    /// </summary>
    public static async Task<(bool ok, string path)> DownloadUpdateBesideExeAsync(string updateUrl)
    {
        var currentExe = GetExecutablePath();
        var dir = Path.GetDirectoryName(currentExe)!;
        var baseName = Path.GetFileNameWithoutExtension(currentExe);
        var ext = Path.GetExtension(currentExe);
        if (string.IsNullOrEmpty(ext))
            ext = ".exe";

        var downloadPath = Path.Combine(dir, baseName + ".update" + ext);

        if (File.Exists(downloadPath))
        {
            try { File.Delete(downloadPath); }
            catch { /* best effort */ }
        }

        var ok = await SG71AuthClient.SG71Client.DownloadUpdateAsync(updateUrl, downloadPath);
        if (!ok || !File.Exists(downloadPath) || new FileInfo(downloadPath).Length == 0)
            return (false, downloadPath);

        return (true, downloadPath);
    }

    /// <summary>
    /// Launch updater script, then exit so the old EXE can be deleted and replaced.
    /// </summary>
    public static void ApplyUpdateAndRestart(string downloadedExePath, string[] extraArgs = null)
    {
        if (string.IsNullOrWhiteSpace(downloadedExePath) || !File.Exists(downloadedExePath))
            throw new FileNotFoundException("Downloaded update file not found.", downloadedExePath);

        var currentExe = GetExecutablePath();
        var currentDir = Path.GetDirectoryName(currentExe)!;
        var pid = Process.GetCurrentProcess().Id;

        var batchPath = Path.Combine(currentDir, $"sg71_update_{pid}.cmd");
        var argLine = BuildArgumentLine(extraArgs);

        var script = new StringBuilder();
        script.AppendLine("@echo off");
        script.AppendLine("setlocal EnableExtensions");
        script.AppendLine($"set \"OLD={EscapeCmd(currentExe)}\"");
        script.AppendLine($"set \"NEW={EscapeCmd(Path.GetFullPath(downloadedExePath))}\"");
        script.AppendLine($"set \"PID={pid}\"");
        script.AppendLine(":wait");
        script.AppendLine("tasklist /FI \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul");
        script.AppendLine("if %errorlevel%==0 (");
        script.AppendLine("  timeout /t 1 /nobreak >nul");
        script.AppendLine("  goto wait");
        script.AppendLine(")");
        script.AppendLine("if exist \"%OLD%\" del /F /Q \"%OLD%\"");
        script.AppendLine("move /Y \"%NEW%\" \"%OLD%\"");
        script.AppendLine("if errorlevel 1 exit /b 1");
        script.AppendLine($"start \"\" \"%OLD%\" {UpdatedFlagArg} {argLine}".TrimEnd());
        script.AppendLine("del /F /Q \"%~f0\"");

        File.WriteAllText(batchPath, script.ToString(), Encoding.UTF8);

        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            WorkingDirectory = currentDir,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        Environment.Exit(0);
    }

    private static string EscapeCmd(string path) => path.Replace("\"", "\"\"");

    private static string BuildArgumentLine(string[] args)
    {
        if (args == null || args.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var a in args)
        {
            if (string.IsNullOrEmpty(a) || a == UpdatedFlagArg)
                continue;
            sb.Append('"').Append(a.Replace("\"", "\\\"")).Append("\" ");
        }
        return sb.ToString().Trim();
    }
}
