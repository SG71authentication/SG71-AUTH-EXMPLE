#include "SG71auth.h"
#include <curl/curl.h>
#include <iostream>
#include <sstream>
#include <cstring>
#include <fstream>
#include <iomanip>
#include <vector>
#include <iterator>
#ifdef _WIN32
#include <windows.h>
#include <iphlpapi.h>
#include <wincrypt.h>
#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "advapi32.lib")
#else
#include <ifaddrs.h>
#include <net/if.h>
#include <openssl/sha.h>
#include <unistd.h>
#include <sys/utsname.h>
#endif

namespace SG71 {


    UserData SG71Client::currentUser;
    bool SG71Client::isLoggedIn = false;


    struct WriteCallback {
        std::string data;
        static size_t Write(void* contents, size_t size, size_t nmemb, void* userp) {
            size_t totalSize = size * nmemb;
            ((WriteCallback*)userp)->data.append((char*)contents, totalSize);
            return totalSize;
        }
    };

    SG71Client::SG71Client(const std::string& adminId, const std::string& appName, const std::string& appVersion)
        : adminId(adminId), appName(appName), appVersion(appVersion), apiBaseUrl("https://sg71auth.netlify.app/api") {

#ifdef _WIN32
        char buffer[MAX_PATH];
        GetModuleFileNameA(NULL, buffer, MAX_PATH);
        appPath = std::string(buffer);
#else
        char buffer[1024];
        ssize_t len = readlink("/proc/self/exe", buffer, sizeof(buffer) - 1);
        if (len != -1) {
            buffer[len] = '\0';
            appPath = std::string(buffer);
        }
#endif
    }

    std::string SG71Client::GetHWID() {
        try {
            std::ostringstream hwidStream;

#ifdef _WIN32

            char computerName[MAX_COMPUTERNAME_LENGTH + 1];
            DWORD size = MAX_COMPUTERNAME_LENGTH + 1;
            GetComputerNameA(computerName, &size);
            hwidStream << computerName;


            PIP_ADAPTER_INFO adapterInfo = (IP_ADAPTER_INFO*)malloc(sizeof(IP_ADAPTER_INFO));
            ULONG bufLen = sizeof(IP_ADAPTER_INFO);

            if (GetAdaptersInfo(adapterInfo, &bufLen) == ERROR_BUFFER_OVERFLOW) {
                free(adapterInfo);
                adapterInfo = (IP_ADAPTER_INFO*)malloc(bufLen);
            }

            if (GetAdaptersInfo(adapterInfo, &bufLen) == NO_ERROR) {
                PIP_ADAPTER_INFO adapter = adapterInfo;
                if (adapter) {
                    hwidStream << std::hex;
                    for (UINT i = 0; i < adapter->AddressLength; i++) {
                        if (i == (adapter->AddressLength - 1))
                            hwidStream << std::setfill('0') << std::setw(2) << (int)adapter->Address[i];
                        else
                            hwidStream << std::setfill('0') << std::setw(2) << (int)adapter->Address[i] << ":";
                    }
                    hwidStream << std::dec;
                }
            }
            free(adapterInfo);
#else
            // Linux/Mac implementation
            struct utsname unameInfo;
            uname(&unameInfo);
            hwidStream << unameInfo.nodename << unameInfo.machine << unameInfo.sysname;

            // Get MAC address
            struct ifaddrs* ifaddr, * ifa;
            if (getifaddrs(&ifaddr) == 0) {
                for (ifa = ifaddr; ifa != nullptr; ifa = ifa->ifa_next) {
                    if (ifa->ifa_addr && ifa->ifa_addr->sa_family == AF_LINK) {
                        struct sockaddr_dl* sdl = (struct sockaddr_dl*)ifa->ifa_addr;
                        unsigned char* mac = (unsigned char*)LLADDR(sdl);
                        hwidStream << std::hex;
                        for (int i = 0; i < sdl->sdl_alen; i++) {
                            if (i > 0) hwidStream << ":";
                            hwidStream << std::setfill('0') << std::setw(2) << (int)mac[i];
                        }
                        hwidStream << std::dec;
                        break;
                    }
                }
                freeifaddrs(ifaddr);
            }
#endif

            std::string raw = hwidStream.str();

            // Simple hash (in production, use SHA256)
            unsigned long hash = 5381;
            for (size_t i = 0; i < raw.length(); i++) {
                hash = ((hash << 5) + hash) + raw[i];
            }

            std::ostringstream result;
            result << std::hex << hash;
            return result.str().substr(0, 32);
        }
        catch (...) {
            return "HWID-ERROR";
        }
    }

    std::string SG71Client::GetAppHash() const {
        try {
            if (appPath.empty()) return "";

            std::ifstream file(appPath, std::ios::binary);
            if (!file.is_open()) return "";

#ifdef _WIN32
            // Windows - use CryptoAPI for SHA256
            std::vector<unsigned char> buffer((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
            file.close();

            HCRYPTPROV hProv = 0;
            HCRYPTHASH hHash = 0;
            unsigned char hash[32];
            DWORD hashLen = 32;

            if (CryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_AES, CRYPT_VERIFYCONTEXT)) {
                if (CryptCreateHash(hProv, CALG_SHA_256, 0, 0, &hHash)) {
                    CryptHashData(hHash, buffer.data(), buffer.size(), 0);
                    if (CryptGetHashParam(hHash, HP_HASHVAL, hash, &hashLen, 0)) {
                        std::ostringstream result;
                        result << std::hex << std::setfill('0');
                        for (int i = 0; i < hashLen; i++) {
                            result << std::setw(2) << (int)hash[i];
                        }
                        CryptDestroyHash(hHash);
                        CryptReleaseContext(hProv, 0);
                        return result.str();
                    }
                    CryptDestroyHash(hHash);
                }
                CryptReleaseContext(hProv, 0);
            }

            // Fallback to simple hash if CryptoAPI fails
            unsigned long simpleHash = 5381;
            for (size_t i = 0; i < buffer.size(); i++) {
                simpleHash = ((simpleHash << 5) + simpleHash) + (unsigned char)buffer[i];
            }
            std::ostringstream result;
            result << std::hex << simpleHash;
            std::string hashStr = result.str();
            while (hashStr.length() < 64) hashStr += "0";
            return hashStr.substr(0, 64);
#else
            // Linux/Mac - use OpenSSL SHA256
            std::vector<unsigned char> buffer((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
            file.close();

            unsigned char hash[SHA256_DIGEST_LENGTH];
            SHA256_CTX sha256;
            SHA256_Init(&sha256);
            SHA256_Update(&sha256, buffer.data(), buffer.size());
            SHA256_Final(hash, &sha256);

            std::ostringstream result;
            result << std::hex << std::setfill('0');
            for (int i = 0; i < SHA256_DIGEST_LENGTH; i++) {
                result << std::setw(2) << (int)hash[i];
            }
            return result.str();
#endif
        }
        catch (...) {
            return "";
        }
    }

    ApiResponse SG71Client::PostRequest(const std::string& endpoint, const std::string& jsonData) {
        ApiResponse response;
        response.success = false;

        CURL* curl = curl_easy_init();
        if (!curl) {
            response.message = "Failed to initialize CURL";
            return response;
        }

        WriteCallback callback;
        std::string url = apiBaseUrl + endpoint;

        curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, jsonData.c_str());
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback::Write);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &callback);

        struct curl_slist* headers = nullptr;
        headers = curl_slist_append(headers, "Content-Type: application/json");
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);

        CURLcode res = curl_easy_perform(curl);

        long httpCode = 0;
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &httpCode);

        if (res == CURLE_OK) {
            // Simple JSON parsing for response
            response.success = (callback.data.find("\"success\":true") != std::string::npos);

            // Parse message
            size_t msgPos = callback.data.find("\"message\":\"");
            if (msgPos != std::string::npos) {
                msgPos += 11;
                size_t msgEnd = callback.data.find("\"", msgPos);
                if (msgEnd != std::string::npos) {
                    response.message = callback.data.substr(msgPos, msgEnd - msgPos);
                }
            }

            // Parse user data if present
            if (callback.data.find("\"user\":") != std::string::npos) {
                // Parse username
                size_t usernamePos = callback.data.find("\"Username\":\"");
                if (usernamePos != std::string::npos) {
                    usernamePos += 12;
                    size_t usernameEnd = callback.data.find("\"", usernamePos);
                    if (usernameEnd != std::string::npos) {
                        response.username = callback.data.substr(usernamePos, usernameEnd - usernamePos);
                    }
                }

                // Parse expires
                size_t expiresPos = callback.data.find("\"Expires\":\"");
                if (expiresPos != std::string::npos) {
                    expiresPos += 11;
                    size_t expiresEnd = callback.data.find("\"", expiresPos);
                    if (expiresEnd != std::string::npos) {
                        response.expires = callback.data.substr(expiresPos, expiresEnd - expiresPos);
                    }
                }

                // Parse HWID
                size_t hwidPos = callback.data.find("\"HWID\":\"");
                if (hwidPos != std::string::npos) {
                    hwidPos += 8;
                    size_t hwidEnd = callback.data.find("\"", hwidPos);
                    if (hwidEnd != std::string::npos) {
                        response.hwid = callback.data.substr(hwidPos, hwidEnd - hwidPos);
                    }
                }

                // Parse IsBanned
                response.isBanned = (callback.data.find("\"IsBanned\":true") != std::string::npos);
            }
        }
        else {
            response.message = "Connection Error: " + std::string(curl_easy_strerror(res));
        }

        curl_slist_free_all(headers);
        curl_easy_cleanup(curl);

        return response;
    }

    ApiResponse SG71Client::Initialize() {
        // Build JSON manually
        std::string jsonData = "{\"adminId\":\"" + adminId + "\",\"appName\":\"" + appName + "\",\"appVersion\":\"" + appVersion + "\"";
        std::string hash = GetAppHash();
        if (!hash.empty()) {
            jsonData += ",\"appHash\":\"" + hash + "\"";
        }
        jsonData += "}";

        ApiResponse response = PostRequest("/init", jsonData);

        if (response.success && !hash.empty() && hash.length() == 64) {
            // Build update JSON manually
            std::string updateJson = "{\"adminId\":\"" + adminId + "\",\"appName\":\"" + appName + "\",\"appHash\":\"" + hash + "\"}";
            PostRequest("/app/update-hash", updateJson);
        }

        return response;
    }

    ApiResponse SG71Client::Login(const std::string& username, const std::string& password) {
        // Build JSON manually
        std::string jsonData = "{\"adminId\":\"" + adminId + "\",\"appName\":\"" + appName + "\",\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"hwid\":\"" + GetHWID() + "\"";
        std::string hash = GetAppHash();
        if (!hash.empty()) {
            jsonData += ",\"appHash\":\"" + hash + "\"";
        }
        jsonData += "}";

        ApiResponse response = PostRequest("/login", jsonData);

        if (response.success) {
            currentUser.username = response.username;
            currentUser.expires = response.expires;
            currentUser.hwid = response.hwid;
            currentUser.isBanned = response.isBanned;
            isLoggedIn = true;
        }

        return response;
    }

    ApiResponse SG71Client::Register(const std::string& username, const std::string& password, const std::string& licenseKey) {
        // Build JSON manually
        std::string jsonData = "{\"adminId\":\"" + adminId + "\",\"appName\":\"" + appName + "\",\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"licenseKey\":\"" + licenseKey + "\"}";

        return PostRequest("/register", jsonData);
    }

}
