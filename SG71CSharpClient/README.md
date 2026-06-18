# SG71 Panel — C# client (production)

Uses shared `examples/SG71Client.cs` + `IntegrityGuard.cs` against **https://sg71auth.netlify.app/api**.

## Configure

Edit `AuthConfig.cs`:

| Field | Dashboard source |
|-------|------------------|
| `AdminId` | Profile / API tab (Firebase UID) |
| `AppName` | Application Manager app name (must match `IntegrityGuard.ExpectedAppName`) |
| `AppVersion` | Application Manager **App Version** (e.g. `1.3`) |

## Build

```bash
cd examples/SG71CSharpClient
dotnet build -c Release
```

## API endpoints used

- `POST /app/check-update` — optional version check
- `POST /init` — init + version + optional hash
- `POST /login` — user auth + HWID
- `POST /register` — license registration
- `POST /app/update-hash` — first-run exe hash upload

## Initialize exit codes

| Code | Meaning |
|------|---------|
| 0 | OK |
| 1 | Local exe hash mismatch |
| 2 | `/init` failed |
| 3 | Version mismatch — update `AppVersion` or dashboard |
| 4 | App name mismatch |

## Copy into your panel

Link in your `.csproj`:

```xml
<Compile Include="..\SG71Client.cs" Link="SG71Client.cs" />
<Compile Include="..\IntegrityGuard.cs" Link="IntegrityGuard.cs" />
```

Then call `AuthConfig.Apply()` and `await client.InitializeWithGuardAsync(IntegrityGuard.ExpectedAppName)`.
