#include <iostream>
#include <string>
#include "SG71Client.h"
#include "AuthConfig.h"
#include "SelfUpdater.h"

static void PrintResponse(const char* label, const SG71::ApiResponse& r) {
    std::cout << label << " [" << r.statusCode << "] "
              << (r.IsOk() ? "OK" : "FAIL") << ": "
              << r.GetDisplayMessage() << "\n";
    if (!r.errorCode.empty())
        std::cout << "  Code: " << r.errorCode << "\n";
}

static bool PromptYesNo(const char* question, bool defaultYes = false) {
    std::cout << question << (defaultYes ? " (Y/n): " : " (y/N): ");
    std::string line;
    std::getline(std::cin, line);
    if (line.empty()) return defaultYes;
    return line[0] == 'y' || line[0] == 'Y';
}

static bool TryApplyUpdate(SG71::ApiResponse& update) {
    if (!update.updateRequired)
        return true;

    PrintResponse("Update", update);
    if (!update.requiredVersion.empty())
        std::cout << "  Server version: " << update.requiredVersion
                  << " | Yours: " << (update.clientVersion.empty() ? AuthConfig::AppVersion : update.clientVersion) << "\n";

    if (update.updateUrl.empty()) {
        std::cout << "  No update URL in panel — set Update URL in Application Manager,\n"
                  << "  or match AppVersion in AuthConfig.h.\n";
        return !update.forceUpdate;
    }

    std::cout << "  Download: " << update.updateUrl << "\n";
    if (!PromptYesNo("Download and install update now?", update.forceUpdate))
        return !update.forceUpdate;

    std::wstring path;
    std::cout << "\nDownloading update next to current EXE...\n";
    if (!SelfUpdater::DownloadUpdateBesideExe(update.updateUrl, path)) {
        std::cout << "Download failed.\n";
        return false;
    }
    std::wcout << L"Saved to: " << path << L"\nApplying update...\n";
    SelfUpdater::ApplyUpdateAndRestart(path);
    return false;
}

static bool RunVersionAndInit(SG71::SG71Client& client) {
    std::cout << "Checking version from panel...\n";
    auto updateCheck = client.CheckForUpdate();

    const bool endpointMissing =
        updateCheck.statusCode == 404 &&
        (updateCheck.rawBody.find("Cannot POST") != std::string::npos ||
         updateCheck.message.find("route missing") != std::string::npos);

    if (endpointMissing) {
        std::cout << "Version check failed — restart API: npm run start:api.\n";
        return false;
    }
    if (!TryApplyUpdate(updateCheck))
        return false;
    PrintResponse("Version check", updateCheck);

    std::cout << "\nInitializing...\n";
    auto init = client.Initialize();
    if (init.updateRequired && !TryApplyUpdate(init))
        return false;

    if (!init.IsOk()) {
        PrintResponse("Init", init);
        return false;
    }

    PrintResponse("Init", init);
    return true;
}

static bool RunLogin(SG71::SG71Client& client) {
    std::string username, password;
    std::cout << "\nUsername: ";
    std::getline(std::cin, username);
    std::cout << "Password: ";
    std::getline(std::cin, password);

    std::cout << "\nLogging in...\n";
    auto login = client.Login(username, password);
    if (!login.IsOk()) {
        PrintResponse("Login", login);
        return false;
    }

    PrintResponse("Login", login);
    if (login.user) {
        std::cout << "  User:    " << login.user->username << "\n";
        std::cout << "  Expires: " << (login.user->expires.empty() ? "—" : login.user->expires) << "\n";
        std::cout << "  HWID:    " << (login.user->hwid.empty() ? SG71::SG71Client::GetHWID() : login.user->hwid) << "\n";
    }
    return true;
}

static bool RunRegister(SG71::SG71Client& client) {
    std::string username, password, license;
    std::cout << "\nUsername: ";
    std::getline(std::cin, username);
    std::cout << "Password: ";
    std::getline(std::cin, password);
    std::cout << "License key: ";
    std::getline(std::cin, license);

    std::cout << "\nRegistering...\n";
    auto reg = client.Register(username, password, license);
    if (!reg.IsOk()) {
        PrintResponse("Register", reg);
        return false;
    }

    PrintResponse("Register", reg);
    std::cout << "You can now log in with the same credentials.\n";
    return true;
}

int main() {
    std::cout << "SG71 Auth C++ Example\n";
    std::cout << "=====================\n\n";

    AuthConfig::Apply();
    std::cout << "API:  " << SG71::SG71Client::GetApiBaseUrl() << "\n";
    std::cout << "App:  " << AuthConfig::AppName << " v" << AuthConfig::AppVersion << "\n";
    std::wcout << L"EXE:  " << SelfUpdater::GetExecutablePath() << L"\n";
    std::cout << "HWID: " << SG71::SG71Client::GetHWID() << "\n\n";

    SG71::SG71Client client(AuthConfig::AdminId, AuthConfig::AppName, AuthConfig::AppVersion);

    if (!RunVersionAndInit(client))
        return 1;

    std::cout << "\n1) Login\n2) Register\nChoice [1]: ";
    std::string choice;
    std::getline(std::cin, choice);

    if (choice == "2") {
        if (!RunRegister(client))
            return 1;
    } else {
        if (!RunLogin(client))
            return 1;
    }

    std::cout << "\nDone. Press Enter to exit.";
    std::cin.get();
    return 0;
}
