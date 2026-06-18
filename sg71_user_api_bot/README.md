# SG71 User Management API — Python bot

Pro plan required. Generate your API key in the dashboard **API** tab.

## Setup

```bash
pip install -r requirements.txt
```

Set environment variables (or edit `CONFIG` in `bot.py`):

| Variable | Description |
|----------|-------------|
| `SG71_API_BASE` | `https://sg71auth.netlify.app/api` (or `http://localhost:3000/api` for local dev) |
| `SG71_ADMIN_ID` | Your Firebase UID (shown in API tab) |
| `SG71_APP_NAME` | Application name from Application Manager |
| `SG71_API_KEY` | Key from API tab (`sg71_...`) |

## Commands

```bash
python bot.py list
python bot.py create --username newuser --password secret123 --expires 2026-12-31
python bot.py update --username newuser --expires 2027-01-01
python bot.py reset-hwid --username newuser --password secret123
python bot.py delete --username newuser
```

Never commit or share your API key.
