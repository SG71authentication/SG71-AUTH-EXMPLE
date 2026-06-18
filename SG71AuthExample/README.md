# SG71 Auth — C# console example

Full client flow: version check → init → login or register → optional self-update.

## Configure

Edit `AuthConfig.cs`:

- `AdminId`, `AppName`, `AppVersion` (must match panel **App Version** — checked via API)
- `ApiBaseUrl` — Debug uses `http://localhost:3000/api`, Release uses Netlify production

Or set environment variable `SG71_API_URL` to override.

## Run

```bash
# Terminal 1 — API
npm run start:api

# Terminal 2 — example
cd examples/SG71AuthExample
dotnet run
```

Open `SG71Auth.sln` → project **SG71AuthExample** → F5.

## Features

- `CheckForUpdateAsync` / `Initialize` with HTTP 426 handling
- Login & register with API messages (user not found, password mismatch, HWID mismatch)
- `SelfUpdater` — download beside EXE, replace, restart
- `GetDisplayMessage()` for all API responses
