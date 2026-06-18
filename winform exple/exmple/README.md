# SG71 Auth — WinForms example (.NET Framework 4.8)

Desktop login UI using `SG71Client.cs`.

## Setup

1. Edit **`AuthConfig.cs`** — set `AdminId`, `AppName`, `AppVersion`, and `ApiBaseUrl`.
2. Start the web API locally (from repo root):

```bash
npm run dev:all
```

3. Open **`SG71Auth.sln`** at the repo root (or `exmple.csproj` in Visual Studio).
4. Set **SG71AuthWinForms** as startup project → **F5**.

## Features

- **Form1** — init, version check, login, register (license key)
- **Form2** — dashboard (user, expiry, HWID), check updates, logout
- **Self-update** — downloads beside EXE with **progress bar and %**, replaces old EXE, restarts automatically

## Production API

In `AuthConfig.cs`:

```csharp
public static string ApiBaseUrl = SG71Client.ProductionApiBaseUrl;
```
