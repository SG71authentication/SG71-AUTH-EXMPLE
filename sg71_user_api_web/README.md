# SG71 User Management API — JavaScript (website backend)

Pro plan required. Use this on your **server** only — never expose `apiKey` in browser JavaScript.

## Files

- `sg71-user-api.js` — reusable client class
- `website-example.js` — Express bridge your frontend can call

## Setup

```bash
npm init -y
npm install express
```

Environment variables:

| Variable | Description |
|----------|-------------|
| `SG71_API_BASE` | `https://sg71auth.netlify.app/api` |
| `SG71_ADMIN_ID` | Your Firebase UID |
| `SG71_APP_NAME` | Application name |
| `SG71_API_KEY` | Key from API tab |

## Run bridge

```bash
node website-example.js
```

Your website frontend calls `http://localhost:4000/api/users` (your server), and the server talks to SG71 with the API key.

## Use in your own server

```js
import { SG71UserApi } from './sg71-user-api.js'

const api = new SG71UserApi({
  apiBase: process.env.SG71_API_BASE,
  adminId: process.env.SG71_ADMIN_ID,
  appName: process.env.SG71_APP_NAME,
  apiKey: process.env.SG71_API_KEY
})

const { users } = await api.listUsers()
```
