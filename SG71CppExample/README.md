# SG71 Auth — C++ console example

Same flow as the C# console sample: version check → init → login or register → optional self-update.

## Configure

Edit `AuthConfig.h`:

- `AdminId`, `AppName`, `AppVersion` (must match panel **App Version** — checked via API)

## Build

Open `SG71CppExample.sln` in this folder → **x64 Debug** or **Release** (Release uses production API).

```bash
npm run start:api
```

Run `bin\x64\Debug\SG71CppExample.exe`.

## Features

- WinHTTP client in `cpp_client/SG71Client.cpp`
- VPN-stable HWID (MachineGuid + user SID, persisted locally)
- Custom update message from panel
- `errorCode` in API responses (e.g. `USER_NOT_FOUND`, `HWID_NOT_MATCH`)
