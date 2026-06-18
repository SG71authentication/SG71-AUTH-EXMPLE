# SG71 client examples

All clients target **https://sg71auth.netlify.app/api** (production) or **http://localhost:3000/api** (local `npm run start:api`).

## Shared C# library

| File | Purpose |
|------|---------|
| `SG71Client.cs` | Main API client — init, login, register, updates |
| `IntegrityGuard.cs` | Optional local app-name + exe hash checks for panels |

## Projects

| Folder | Language | Use case |
|--------|----------|----------|
| `SG71CSharpClient/` | C# | **Panel exe** — `InitializeWithGuardAsync` + login |
| `SG71AuthExample/` | C# console | Full demo with self-update |
| `winform exple/` | C# WinForms | GUI login sample |
| `SG71CppExample/` | C++ | Console + self-update |
| `cpp_client/` | C++ | Shared C++ client library |
| `sg71_user_api_bot/` | Python | User Management API (Pro) |
| `sg71_user_api_web/` | JavaScript | Website backend bridge (Pro) |

## API endpoints (exe clients)

```
POST /app/check-update   { adminId, appName, appVersion }
POST /init               { adminId, appName, appVersion, appHash? }
POST /login              { adminId, appName, appVersion?, username, password, hwid, appHash? }
POST /register           { adminId, appName, username, password, licenseKey }
POST /app/update-hash    { adminId, appName, appHash }
```

## Required dashboard match

1. **Admin ID** = your Firebase UID  
2. **App name** = exact string in Application Manager  
3. **App version** = exact string in Application Manager (e.g. `1.3`)  
4. **Pause** = OFF  
5. **Check Hash** = OFF until exe hash is registered  

## Environment override

```bash
set SG71_API_URL=https://sg71auth.netlify.app/api
```

## Download packages (Guide tab)

Paste these in **Guide → Client Source Downloads** (superadmin), or use the Download buttons after deploy:

| Package | URL |
|---------|-----|
| C# | `https://sg71auth.netlify.app/downloads/sg71-csharp.zip` |
| C++ | `https://sg71auth.netlify.app/downloads/sg71-cpp.zip` |

Rebuild zips after editing examples:

```bash
cd deploy
npm run package:examples
```

## Panel quick start (C#)

```csharp
AuthConfig.Apply();
var client = new SG71Client(AuthConfig.AdminId, AuthConfig.AppName, AuthConfig.AppVersion);
var code = await client.InitializeWithGuardAsync(IntegrityGuard.ExpectedAppName);
if (code != 0) Environment.Exit(code);
var login = await client.Login(username, password);
```
