#include "SG71Client.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <winhttp.h>
#include <wincrypt.h>
#include <sddl.h>
#include <sstream>
#include <fstream>
#include <algorithm>
#include <vector>
#include <thread>
#include <cstdio>
#include <shlobj.h>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "crypt32.lib")
#pragma comment(lib, "advapi32.lib")

namespace SG71 {

UserData SG71Client::currentUser;

static const wchar_t* kUserAgent = L"SG71Client/2.1";

struct ApiEndpoint {
    std::wstring host;
    int port = 80;
    bool ssl = false;
    std::wstring basePath = L"/api";
};

static ApiEndpoint g_api;
static std::string g_apiUrl = SG71Client::kProductionApiUrl;
static std::string g_hwidCache;
static std::wstring Utf8ToWide(const std::string& s);
static std::string WideToUtf8(const std::wstring& s);

static std::string GetHwidStorePath() {
    wchar_t localAppData[MAX_PATH] = {};
    if (SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, 0, localAppData) != S_OK)
        return "";
    std::wstring dir = std::wstring(localAppData) + L"\\SG71Auth";
    CreateDirectoryW(dir.c_str(), nullptr);
    return WideToUtf8(dir) + "\\hwid.txt";
}

static std::string ReadPersistedHwid() {
    std::string path = GetHwidStorePath();
    if (path.empty()) return "";
    std::ifstream in(path);
    if (!in) return "";
    std::string value;
    in >> value;
    if (value.size() == 32) return value;
    return "";
}

static void WritePersistedHwid(const std::string& hwid) {
    std::string path = GetHwidStorePath();
    if (path.empty() || hwid.empty()) return;
    std::ofstream out(path, std::ios::trunc);
    if (out) out << hwid;
}

static std::string NormalizeApiUrl(std::string url) {
    while (!url.empty() && (url.back() == '/' || url.back() == ' '))
        url.pop_back();
    if (url.size() < 4 || url.compare(url.size() - 4, 4, "/api") != 0)
        url += "/api";
    return url;
}

static ApiEndpoint ParseApiUrl(const std::string& url) {
    ApiEndpoint ep;
    std::string u = url;
    ep.ssl = u.rfind("https://", 0) == 0;
    auto schemeEnd = u.find("://");
    if (schemeEnd == std::string::npos)
        return ep;
    auto hostStart = schemeEnd + 3;
    auto pathStart = u.find('/', hostStart);
    std::string hostPort = pathStart == std::string::npos
        ? u.substr(hostStart)
        : u.substr(hostStart, pathStart - hostStart);
    std::string path = pathStart == std::string::npos ? "/api" : u.substr(pathStart);
    if (path.empty()) path = "/api";

    auto colon = hostPort.find(':');
    std::string host = colon == std::string::npos ? hostPort : hostPort.substr(0, colon);
    if (colon != std::string::npos)
        ep.port = std::stoi(hostPort.substr(colon + 1));
    else
        ep.port = ep.ssl ? 443 : 80;

    ep.host = Utf8ToWide(host);
    ep.basePath = Utf8ToWide(path);
    return ep;
}

static bool LooksLikeHtml(const std::string& body) {
    if (body.empty()) return false;
    auto t = body;
    while (!t.empty() && (t.front() == ' ' || t.front() == '\n' || t.front() == '\r'))
        t.erase(t.begin());
    return t.rfind("<!DOCTYPE", 0) == 0 || t.rfind("<html", 0) == 0;
}

void SG71Client::SetApiBaseUrl(const std::string& url) {
    g_apiUrl = NormalizeApiUrl(url);
    g_api = ParseApiUrl(g_apiUrl);
}

std::string SG71Client::GetApiBaseUrl() {
    return g_apiUrl;
}

bool ApiResponse::IsOk() const {
    return success && !updateRequired && statusCode >= 200 && statusCode < 300;
}

std::string ApiResponse::GetDisplayMessage() const {
    if (updateRequired) {
        std::string msg = message;
        if (msg.empty()) msg = "Update required.";
        if (!requiredVersion.empty())
            msg += " Server version: " + requiredVersion + ", yours: " +
                (clientVersion.empty() ? "?" : clientVersion) +
                ". Set AppVersion in AuthConfig.h or download the update.";
        return msg;
    }
    if (!message.empty()) return message;
    if (upToDate) return "Application is up to date.";
    if (LooksLikeHtml(rawBody))
        return "API returned HTML instead of JSON. Use URL ending with /api (e.g. http://localhost:3000/api). Run npm run start:api";
    if (statusCode > 0) return "HTTP " + std::to_string(statusCode);
    return "Unknown error";
}

static ApiEndpoint& ActiveApi() {
    if (g_api.host.empty())
        g_api = ParseApiUrl(g_apiUrl);
    return g_api;
}

static std::string ReadMachineGuid() {
    HKEY key = nullptr;
    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Cryptography", 0,
            KEY_READ | KEY_WOW64_64KEY, &key) != ERROR_SUCCESS)
        return "";
    wchar_t buf[128] = {};
    DWORD size = sizeof(buf);
    std::string result;
    if (RegQueryValueExW(key, L"MachineGuid", nullptr, nullptr, (LPBYTE)buf, &size) == ERROR_SUCCESS)
        result = WideToUtf8(buf);
    RegCloseKey(key);
    return result;
}

static std::string ReadSystemVolumeSerial() {
    wchar_t root[] = L"C:\\";
    wchar_t sysDir[MAX_PATH] = {};
    if (GetWindowsDirectoryW(sysDir, MAX_PATH) > 0) {
        if (sysDir[1] == L':')
            root[0] = sysDir[0];
    }
    DWORD serial = 0;
    if (!GetVolumeInformationW(root, nullptr, 0, &serial, nullptr, nullptr, nullptr, 0))
        return "";
    char hex[16];
    sprintf_s(hex, "%08X", serial);
    return hex;
}

static std::string ReadUserSid() {
    HANDLE token = nullptr;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token))
        return "NOSID";
    DWORD len = 0;
    GetTokenInformation(token, TokenUser, nullptr, 0, &len);
    std::vector<BYTE> buffer(len);
    if (!GetTokenInformation(token, TokenUser, buffer.data(), len, &len)) {
        CloseHandle(token);
        return "NOSID";
    }
    auto* user = reinterpret_cast<TOKEN_USER*>(buffer.data());
    LPWSTR sidStr = nullptr;
    std::string result = "NOSID";
    if (ConvertSidToStringSidW(user->User.Sid, &sidStr)) {
        result = WideToUtf8(sidStr);
        LocalFree(sidStr);
    }
    CloseHandle(token);
    return result;
}

static std::string Sha256Hex32(const std::string& input) {
    HCRYPTPROV prov = 0;
    HCRYPTHASH hash = 0;
    BYTE digest[32];
    DWORD digestLen = 32;
    std::string out;
    if (!CryptAcquireContext(&prov, nullptr, nullptr, PROV_RSA_AES, CRYPT_VERIFYCONTEXT) ||
        !CryptCreateHash(prov, CALG_SHA_256, 0, 0, &hash) ||
        !CryptHashData(hash, (const BYTE*)input.data(), (DWORD)input.size(), 0) ||
        !CryptGetHashParam(hash, HP_HASHVAL, digest, &digestLen, 0)) {
        if (hash) CryptDestroyHash(hash);
        if (prov) CryptReleaseContext(prov, 0);
        return "";
    }
    static const char hex[] = "0123456789abcdef";
    out.reserve(32);
    for (DWORD i = 0; i < 16; ++i) {
        out += hex[digest[i] >> 4];
        out += hex[digest[i] & 0xf];
    }
    CryptDestroyHash(hash);
    CryptReleaseContext(prov, 0);
    return out;
}

static std::wstring Utf8ToWide(const std::string& s) {
    if (s.empty()) return L"";
    int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, nullptr, 0);
    std::wstring out(len - 1, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, out.data(), len);
    return out;
}

static std::string WideToUtf8(const std::wstring& s) {
    if (s.empty()) return "";
    int len = WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, nullptr, 0, nullptr, nullptr);
    std::string out(len - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, s.c_str(), -1, out.data(), len, nullptr, nullptr);
    return out;
}

static std::string EscapeJson(const std::string& s) {
    std::string o;
    o.reserve(s.size() + 8);
    for (char c : s) {
        switch (c) {
            case '"': o += "\\\""; break;
            case '\\': o += "\\\\"; break;
            case '\n': o += "\\n"; break;
            case '\r': o += "\\r"; break;
            case '\t': o += "\\t"; break;
            default: o += c;
        }
    }
    return o;
}

static std::string JsonGetString(const std::string& json, const std::string& key) {
    std::string search = "\"" + key + "\"";
    auto pos = json.find(search);
    if (pos == std::string::npos) return "";
    pos = json.find(':', pos);
    if (pos == std::string::npos) return "";
    pos = json.find('"', pos);
    if (pos == std::string::npos) return "";
    auto end = json.find('"', pos + 1);
    if (end == std::string::npos) return "";
    return json.substr(pos + 1, end - pos - 1);
}

static bool JsonGetBool(const std::string& json, const std::string& key, bool defaultVal = false) {
    std::string search = "\"" + key + "\"";
    auto pos = json.find(search);
    if (pos == std::string::npos) return defaultVal;
    pos = json.find(':', pos);
    if (pos == std::string::npos) return defaultVal;
    auto tail = json.substr(pos + 1, 12);
    if (tail.find("true") != std::string::npos) return true;
    if (tail.find("false") != std::string::npos) return false;
    return defaultVal;
}

static ApiResponse ParseApiResponse(const std::string& body, int statusCode) {
    ApiResponse r;
    r.statusCode = statusCode;
    r.rawBody = body;
    if (statusCode == 426)
        r.updateRequired = true;

    if (!body.empty() && body[0] != '{') {
        r.success = false;
        if (LooksLikeHtml(body)) {
            if (body.find("Cannot POST") != std::string::npos)
                r.message = "API route missing. Restart local server: npm run start:api";
            else
                r.message = "API returned HTML, not JSON. Use URL ending with /api (e.g. http://localhost:3000/api).";
        } else {
            r.message = body.substr(0, body.size() > 200 ? 200 : body.size());
        }
        return r;
    }
    r.success = JsonGetBool(body, "success", false);
    r.message = JsonGetString(body, "message");
    r.updateRequired = JsonGetBool(body, "updateRequired", false);
    r.upToDate = JsonGetBool(body, "upToDate", false);
    r.requiredVersion = JsonGetString(body, "requiredVersion");
    r.clientVersion = JsonGetString(body, "clientVersion");
    r.updateUrl = JsonGetString(body, "updateUrl");
    r.forceUpdate = JsonGetBool(body, "forceUpdate", false);
    r.errorCode = JsonGetString(body, "errorCode");

    if (r.success && body.find("\"user\"") != std::string::npos) {
        UserData u;
        u.username = JsonGetString(body, "Username");
        if (u.username.empty()) u.username = JsonGetString(body, "username");
        u.expires = JsonGetString(body, "Expires");
        if (u.expires.empty()) u.expires = JsonGetString(body, "expires");
        u.hwid = JsonGetString(body, "HWID");
        if (u.hwid.empty()) u.hwid = JsonGetString(body, "hwid");
        r.user = u;
    }
    return r;
}

static bool HttpRequest(
    const std::wstring& host,
    int port,
    bool useSsl,
    const std::wstring& path,
    const std::string& method,
    const std::string& body,
    std::string& responseOut,
    int& statusOut)
{
    responseOut.clear();
    statusOut = 0;

    HINTERNET session = WinHttpOpen(kUserAgent,
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!session) return false;

    HINTERNET connect = WinHttpConnect(session, host.c_str(), (INTERNET_PORT)port, 0);
    if (!connect) {
        WinHttpCloseHandle(session);
        return false;
    }

    DWORD flags = useSsl ? WINHTTP_FLAG_SECURE : 0;
    HINTERNET request = WinHttpOpenRequest(connect, Utf8ToWide(method).c_str(), path.c_str(),
        nullptr, WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, flags);
    if (!request) {
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return false;
    }

    const wchar_t* headers = L"Content-Type: application/json\r\n";
    BOOL ok = WinHttpSendRequest(request, headers, (DWORD)-1L,
        body.empty() ? WINHTTP_NO_REQUEST_DATA : (LPVOID)body.data(),
        body.empty() ? 0 : (DWORD)body.size(), body.empty() ? 0 : (DWORD)body.size(), 0);

    if (ok) ok = WinHttpReceiveResponse(request, nullptr);

    if (ok) {
        DWORD status = 0, size = sizeof(status);
        WinHttpQueryHeaders(request, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
            WINHTTP_HEADER_NAME_BY_INDEX, &status, &size, WINHTTP_NO_HEADER_INDEX);
        statusOut = (int)status;

        DWORD available = 0;
        do {
            if (!WinHttpQueryDataAvailable(request, &available)) break;
            if (available == 0) break;
            std::vector<char> buf(available);
            DWORD read = 0;
            if (!WinHttpReadData(request, buf.data(), available, &read)) break;
            responseOut.append(buf.data(), read);
        } while (available > 0);
    }

    WinHttpCloseHandle(request);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);
    return ok == TRUE;
}

SG71Client::SG71Client(const std::string& adminId, const std::string& appName, const std::string& appVersion)
    : adminId_(adminId), appName_(appName), appVersion_(appVersion.empty() ? "1.0" : appVersion) {}

ApiResponse SG71Client::CheckForUpdate() {
    std::ostringstream oss;
    oss << "{"
        << "\"adminId\":\"" << EscapeJson(adminId_) << "\","
        << "\"appName\":\"" << EscapeJson(appName_) << "\","
        << "\"appVersion\":\"" << EscapeJson(appVersion_) << "\""
        << "}";
    return Post("/app/check-update", oss.str());
}

ApiResponse SG71Client::Initialize() {
    std::string hash = GetAppHash();
    std::ostringstream oss;
    oss << "{"
        << "\"adminId\":\"" << EscapeJson(adminId_) << "\","
        << "\"appName\":\"" << EscapeJson(appName_) << "\","
        << "\"appVersion\":\"" << EscapeJson(appVersion_) << "\","
        << "\"appHash\":\"" << EscapeJson(hash) << "\""
        << "}";
    auto resp = Post("/init", oss.str());
    if (resp.success && hash.size() == 64) {
        std::thread([this, hash]() { SendAppHashAsync(hash); }).detach();
    }
    return resp;
}

bool SG71Client::InitializeWithAutoUpdate(const std::wstring& destinationPath) {
    auto init = Initialize();
    if (init.success) return true;
    if (!init.updateRequired || init.updateUrl.empty()) return false;
    return DownloadUpdate(init.updateUrl, destinationPath);
}

ApiResponse SG71Client::Login(const std::string& username, const std::string& password) {
    std::string hash = GetAppHash();
    std::ostringstream oss;
    oss << "{"
        << "\"adminId\":\"" << EscapeJson(adminId_) << "\","
        << "\"appName\":\"" << EscapeJson(appName_) << "\","
        << "\"username\":\"" << EscapeJson(username) << "\","
        << "\"password\":\"" << EscapeJson(password) << "\","
        << "\"hwid\":\"" << EscapeJson(GetHWID()) << "\","
        << "\"appHash\":\"" << EscapeJson(hash) << "\","
        << "\"appVersion\":\"" << EscapeJson(appVersion_) << "\""
        << "}";
    auto resp = Post("/login", oss.str());
    if (resp.success && resp.user) {
        currentUser = *resp.user;
    }
    return resp;
}

ApiResponse SG71Client::Register(const std::string& username, const std::string& password, const std::string& licenseKey) {
    std::ostringstream oss;
    oss << "{"
        << "\"adminId\":\"" << EscapeJson(adminId_) << "\","
        << "\"appName\":\"" << EscapeJson(appName_) << "\","
        << "\"username\":\"" << EscapeJson(username) << "\","
        << "\"password\":\"" << EscapeJson(password) << "\","
        << "\"licenseKey\":\"" << EscapeJson(licenseKey) << "\","
        << "\"hwid\":\"" << EscapeJson(GetHWID()) << "\""
        << "}";
    return Post("/register", oss.str());
}

bool SG71Client::DownloadUpdate(const std::string& updateUrl, const std::wstring& destinationPath) {
    if (updateUrl.empty() || destinationPath.empty()) return false;

    std::wstring url = Utf8ToWide(updateUrl);
    URL_COMPONENTS parts{};
    parts.dwStructSize = sizeof(parts);
    wchar_t host[256] = {}, path[2048] = {};
    parts.lpszHostName = host;
    parts.dwHostNameLength = 256;
    parts.lpszUrlPath = path;
    parts.dwUrlPathLength = 2048;

    if (!WinHttpCrackUrl(url.c_str(), 0, 0, &parts)) return false;

    bool ssl = parts.nScheme == INTERNET_SCHEME_HTTPS;
    std::string response;
    int status = 0;

    HINTERNET session = WinHttpOpen(kUserAgent, WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!session) return false;

    HINTERNET connect = WinHttpConnect(session, host, parts.nPort, 0);
    if (!connect) {
        WinHttpCloseHandle(session);
        return false;
    }

    DWORD flags = ssl ? WINHTTP_FLAG_SECURE : 0;
    HINTERNET request = WinHttpOpenRequest(connect, L"GET", path, nullptr, WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES, flags);
    if (!request) {
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return false;
    }

    BOOL ok = WinHttpSendRequest(request, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
        WINHTTP_NO_REQUEST_DATA, 0, 0, 0);
    if (ok) ok = WinHttpReceiveResponse(request, nullptr);
    if (!ok) {
        WinHttpCloseHandle(request);
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return false;
    }

    std::ofstream out(destinationPath, std::ios::binary);
    if (!out) {
        WinHttpCloseHandle(request);
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return false;
    }

    DWORD available = 0;
    do {
        if (!WinHttpQueryDataAvailable(request, &available)) break;
        if (available == 0) break;
        std::vector<char> buf(available);
        DWORD read = 0;
        if (!WinHttpReadData(request, buf.data(), available, &read)) break;
        out.write(buf.data(), read);
    } while (available > 0);

    out.close();
    WinHttpCloseHandle(request);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);
    return out.good();
}

std::string SG71Client::GetHWID() {
    if (!g_hwidCache.empty())
        return g_hwidCache;

    std::string persisted = ReadPersistedHwid();
    if (!persisted.empty()) {
        g_hwidCache = persisted;
        return g_hwidCache;
    }

    try {
        // MachineGuid + Windows user SID only — stable with VPN (no MAC/IP/hostname).
        std::ostringstream raw;
        raw << ReadMachineGuid() << '|' << ReadUserSid();

        g_hwidCache = Sha256Hex32(raw.str());
        if (!g_hwidCache.empty()) {
            WritePersistedHwid(g_hwidCache);
            return g_hwidCache;
        }
    } catch (...) {}

    return "HWID-ERROR";
}

void SG71Client::ClearHwidCache() {
    g_hwidCache.clear();
    std::string path = GetHwidStorePath();
    if (!path.empty())
        DeleteFileA(path.c_str());
}

std::string SG71Client::GetAppHash() const {
    wchar_t path[MAX_PATH] = {};
    if (!GetModuleFileNameW(nullptr, path, MAX_PATH)) return "";

    std::ifstream file(path, std::ios::binary);
    if (!file) return "";

    HCRYPTPROV prov = 0;
    HCRYPTHASH hash = 0;
    BYTE digest[32];
    DWORD digestLen = 32;
    if (!CryptAcquireContext(&prov, nullptr, nullptr, PROV_RSA_AES, CRYPT_VERIFYCONTEXT) ||
        !CryptCreateHash(prov, CALG_SHA_256, 0, 0, &hash)) {
        return "";
    }

    char buf[8192];
    while (file.read(buf, sizeof(buf)) || file.gcount() > 0) {
        CryptHashData(hash, (BYTE*)buf, (DWORD)file.gcount(), 0);
    }

    if (!CryptGetHashParam(hash, HP_HASHVAL, digest, &digestLen, 0)) {
        CryptDestroyHash(hash);
        CryptReleaseContext(prov, 0);
        return "";
    }

    static const char hex[] = "0123456789abcdef";
    std::string out;
    out.reserve(64);
    for (DWORD i = 0; i < digestLen; ++i) {
        out += hex[digest[i] >> 4];
        out += hex[digest[i] & 0xf];
    }
    CryptDestroyHash(hash);
    CryptReleaseContext(prov, 0);
    return out;
}

ApiResponse SG71Client::Post(const std::string& endpoint, const std::string& jsonBody) {
    auto& api = ActiveApi();
    std::wstring path = api.basePath;
    std::wstring ep = Utf8ToWide(endpoint);
    if (!ep.empty() && ep[0] != L'/') path += L"/";
    path += ep;

    std::string response;
    int status = 0;
    if (!HttpRequest(api.host, api.port, api.ssl, path, "POST", jsonBody, response, status)) {
        lastResponse_ = "Connection Error";
        lastApi_ = ApiResponse{};
        lastApi_.message = lastResponse_;
        return lastApi_;
    }

    lastResponse_ = response;
    lastApi_ = ParseApiResponse(response, status);
    if (lastApi_.message.empty())
        lastApi_.message = lastApi_.GetDisplayMessage();
    if (status == 426 && !lastApi_.updateRequired)
        lastApi_.updateRequired = true;
    return lastApi_;
}

void SG71Client::SendAppHashAsync(const std::string& hash) {
    std::ostringstream oss;
    oss << "{"
        << "\"adminId\":\"" << EscapeJson(adminId_) << "\","
        << "\"appName\":\"" << EscapeJson(appName_) << "\","
        << "\"appHash\":\"" << EscapeJson(hash) << "\""
        << "}";
    std::string resp;
    int status = 0;
    auto& api = ActiveApi();
    std::wstring path = api.basePath;
    path += L"/app/update-hash";
    HttpRequest(api.host, api.port, api.ssl, path, "POST", oss.str(), resp, status);
}

} // namespace SG71
