#pragma once

#include <string>
#include <optional>

namespace SG71 {

struct UserData {
    std::string username;
    std::string expires;
    std::string hwid;
    bool isBanned = false;
};

struct ApiResponse {
    bool success = false;
    std::string message;
    std::optional<UserData> user;
    int statusCode = 0;
    std::string rawBody;

    bool updateRequired = false;
    bool upToDate = false;
    std::string requiredVersion;
    std::string clientVersion;
    std::string updateUrl;
    bool forceUpdate = false;
    std::string errorCode;

    bool IsOk() const;
    std::string GetDisplayMessage() const;
};

class SG71Client {
public:
    static constexpr const char* kProductionApiUrl = "https://sg71auth.netlify.app/api";
    static constexpr const char* kLocalDevApiUrl = "http://localhost:5173/api";
    static constexpr const char* kLocalDirectApiUrl = "http://localhost:3000/api";

    static UserData currentUser;

    static void SetApiBaseUrl(const std::string& url);
    static std::string GetApiBaseUrl();

    SG71Client(const std::string& adminId, const std::string& appName, const std::string& appVersion);

    ApiResponse CheckForUpdate();
    ApiResponse Initialize();
    ApiResponse Login(const std::string& username, const std::string& password);
    ApiResponse Register(const std::string& username, const std::string& password, const std::string& licenseKey);

    bool InitializeWithAutoUpdate(const std::wstring& destinationPath);
    static bool DownloadUpdate(const std::string& updateUrl, const std::wstring& destinationPath);

    static std::string GetHWID();
    static void ClearHwidCache();
    std::string GetAppHash() const;

    const std::string& LastResponse() const { return lastResponse_; }
    const ApiResponse& LastApiResponse() const { return lastApi_; }

private:
    std::string adminId_;
    std::string appName_;
    std::string appVersion_;
    std::string lastResponse_;
    ApiResponse lastApi_;

    ApiResponse Post(const std::string& endpoint, const std::string& jsonBody);
    void SendAppHashAsync(const std::string& hash);
};

} // namespace SG71
