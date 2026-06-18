#include "SelfUpdater.h"
#include "SG71Client.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <fstream>
#include <sstream>
#include <vector>

namespace SelfUpdater {

std::wstring GetExecutablePath() {
    wchar_t path[MAX_PATH] = {};
    if (GetModuleFileNameW(nullptr, path, MAX_PATH))
        return path;
    return L"";
}

bool DownloadUpdateBesideExe(const std::string& updateUrl, std::wstring& outPath) {
    outPath = GetExecutablePath();
    if (outPath.empty()) return false;

    size_t slash = outPath.find_last_of(L"\\/");
    std::wstring dir = slash != std::wstring::npos ? outPath.substr(0, slash) : L".";
    std::wstring file = slash != std::wstring::npos ? outPath.substr(slash + 1) : outPath;

    size_t dot = file.find_last_of(L'.');
    std::wstring base = dot != std::wstring::npos ? file.substr(0, dot) : file;
    std::wstring ext = dot != std::wstring::npos ? file.substr(dot) : L".exe";

    outPath = dir + L"\\" + base + L".update" + ext;
    return SG71::SG71Client::DownloadUpdate(updateUrl, outPath);
}

void ApplyUpdateAndRestart(const std::wstring& downloadedExe) {
    std::wstring current = GetExecutablePath();
    if (current.empty() || downloadedExe.empty()) return;

    DWORD pid = GetCurrentProcessId();
    size_t slash = current.find_last_of(L"\\/");
    std::wstring dir = slash != std::wstring::npos ? current.substr(0, slash) : L".";

    std::wostringstream batch;
    batch << L"@echo off\r\n"
          << L"set \"OLD=" << current << L"\"\r\n"
          << L"set \"NEW=" << downloadedExe << L"\"\r\n"
          << L"set \"PID=" << pid << L"\"\r\n"
          << L":wait\r\n"
          << L"tasklist /FI \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul\r\n"
          << L"if %errorlevel%==0 (timeout /t 1 /nobreak >nul & goto wait)\r\n"
          << L"if exist \"%OLD%\" del /F /Q \"%OLD%\"\r\n"
          << L"move /Y \"%NEW%\" \"%OLD%\"\r\n"
          << L"start \"\" \"%OLD%\"\r\n"
          << L"del /F /Q \"%~f0\"\r\n";

    std::wstring batchPath = dir + L"\\sg71_update_" + std::to_wstring(pid) + L".cmd";
    FILE* f = nullptr;
    if (_wfopen_s(&f, batchPath.c_str(), L"w") != 0 || !f) return;
    fwprintf(f, L"%s", batch.str().c_str());
    fclose(f);

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    PROCESS_INFORMATION pi{};
    std::wstring cmd = L"cmd.exe /c \"" + batchPath + L"\"";
    std::vector<wchar_t> cmdLine(cmd.begin(), cmd.end());
    cmdLine.push_back(L'\0');
    if (CreateProcessW(nullptr, cmdLine.data(), nullptr, nullptr, FALSE, CREATE_NO_WINDOW,
            nullptr, dir.c_str(), &si, &pi)) {
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
    }
    ExitProcess(0);
}

} // namespace SelfUpdater
