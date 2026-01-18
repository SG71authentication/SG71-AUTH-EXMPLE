#pragma once
#ifndef SG71_CLIENT_H
#define SG71_CLIENT_H

#include <string>
#include <memory>

namespace SG71 {

    struct ApiResponse {
        bool success;
        std::string message;
        std::string username;
        std::string expires;
        std::string hwid;
        bool isBanned;
    };

    struct UserData {
        std::string username;
        std::string expires;
        std::string hwid;
        bool isBanned;
    };

    class SG71Client {
    private:
        std::string adminId;
        std::string appName;
        std::string appVersion;
        std::string appPath;
        std::string apiBaseUrl;

        static std::string GetHWID();
        std::string GetAppHash() const;
        ApiResponse PostRequest(const std::string& endpoint, const std::string& jsonData);

    public:

        static UserData currentUser;
        static bool isLoggedIn;

        SG71Client(const std::string& adminId, const std::string& appName, const std::string& appVersion);

        ApiResponse Initialize();
        ApiResponse Login(const std::string& username, const std::string& password);
        ApiResponse Register(const std::string& username, const std::string& password, const std::string& licenseKey);

        void SetApiBaseUrl(const std::string& url) { apiBaseUrl = url; }
        std::string GetApiBaseUrl() const { return apiBaseUrl; }
    };

}

#endif // SG71_CLIENT_H
#pragma once
