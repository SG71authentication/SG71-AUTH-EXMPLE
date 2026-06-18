#!/usr/bin/env python3
"""
SG71 User Management API — Python bot / CLI helper.

Requires: pip install requests

Usage:
  python bot.py list
  python bot.py create --username newuser --password secret123 --expires 2026-12-31
  python bot.py update --username newuser --expires 2027-01-01
  python bot.py reset-hwid --username newuser --password secret123
  python bot.py delete --username newuser

Set env vars or edit CONFIG below.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from typing import Any

import requests

CONFIG = {
    "api_base": os.environ.get("SG71_API_BASE", "https://sg71auth.netlify.app/api"),
    "admin_id": os.environ.get("SG71_ADMIN_ID", "your-app-id"),
    "app_name": os.environ.get("SG71_APP_NAME", "your-app-name"),
    "api_key": os.environ.get("SG71_API_KEY", "sg71_your_api_key"),
}


class SG71UserApi:
    def __init__(
        self,
        api_base: str,
        admin_id: str,
        app_name: str,
        api_key: str,
    ) -> None:
        self.api_base = api_base.rstrip("/")
        self.admin_id = admin_id
        self.app_name = app_name
        self.api_key = api_key

    def _post(self, path: str, payload: dict[str, Any]) -> dict[str, Any]:
        body = {
            "adminId": self.admin_id,
            "appName": self.app_name,
            "apiKey": self.api_key,
            **payload,
        }
        url = f"{self.api_base}{path}"
        response = requests.post(url, json=body, timeout=30)
        data = response.json() if response.text else {}
        if not response.ok or not data.get("success", False):
            message = data.get("message") or response.text or "Request failed"
            raise RuntimeError(f"{response.status_code}: {message}")
        return data

    def list_users(self) -> list[dict[str, Any]]:
        data = self._post("/admin/api/users/list", {})
        return data.get("users", [])

    def create_user(
        self,
        username: str,
        password: str,
        expires: str | None = None,
        hwid: str | None = None,
        is_banned: bool = False,
    ) -> dict[str, Any]:
        return self._post(
            "/admin/api/users/create",
            {
                "username": username,
                "password": password,
                "expires": expires,
                "hwid": hwid,
                "isBanned": is_banned,
            },
        )

    def update_user(
        self,
        username: str,
        *,
        password: str | None = None,
        expires: str | None = None,
        hwid: str | None = None,
        is_banned: bool | None = None,
        reset_hwid: bool = False,
    ) -> dict[str, Any]:
        payload: dict[str, Any] = {"username": username}
        if password is not None:
            payload["password"] = password
        if expires is not None:
            payload["expires"] = expires
        if hwid is not None:
            payload["hwid"] = hwid
        if is_banned is not None:
            payload["isBanned"] = is_banned
        if reset_hwid:
            payload["resetHwid"] = True
        return self._post("/admin/api/users/update", payload)

    def reset_hwid(self, username: str, password: str) -> dict[str, Any]:
        return self._post(
            "/admin/api/users/reset-hwid",
            {"username": username, "password": password},
        )

    def delete_user(self, username: str) -> dict[str, Any]:
        return self._post("/admin/api/users/delete", {"username": username})


def build_client() -> SG71UserApi:
    missing = [k for k, v in CONFIG.items() if not v or "your-" in str(v)]
    if missing:
        print("Configure SG71_ADMIN_ID, SG71_APP_NAME, SG71_API_KEY (env or CONFIG).", file=sys.stderr)
        sys.exit(1)
    return SG71UserApi(**CONFIG)


def cmd_list(api: SG71UserApi) -> None:
    users = api.list_users()
    print(json.dumps(users, indent=2))
    print(f"\nTotal: {len(users)}")


def main() -> None:
    parser = argparse.ArgumentParser(description="SG71 User Management API bot")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("list", help="List all users")

    create = sub.add_parser("create", help="Create a user")
    create.add_argument("--username", required=True)
    create.add_argument("--password", required=True)
    create.add_argument("--expires", default=None)
    create.add_argument("--hwid", default=None)
    create.add_argument("--banned", action="store_true")

    update = sub.add_parser("update", help="Update a user")
    update.add_argument("--username", required=True)
    update.add_argument("--password", default=None)
    update.add_argument("--expires", default=None)
    update.add_argument("--hwid", default=None)
    update.add_argument("--banned", action="store_true", default=None)
    update.add_argument("--unban", action="store_true")

    reset = sub.add_parser("reset-hwid", help="Reset user HWID")
    reset.add_argument("--username", required=True)
    reset.add_argument("--password", required=True)

    delete = sub.add_parser("delete", help="Delete a user")
    delete.add_argument("--username", required=True)

    args = parser.parse_args()
    api = build_client()

    if args.command == "list":
        cmd_list(api)
    elif args.command == "create":
        result = api.create_user(
            args.username,
            args.password,
            expires=args.expires,
            hwid=args.hwid,
            is_banned=args.banned,
        )
        print(json.dumps(result, indent=2))
    elif args.command == "update":
        is_banned = True if args.banned else False if args.unban else None
        result = api.update_user(
            args.username,
            password=args.password,
            expires=args.expires,
            hwid=args.hwid,
            is_banned=is_banned,
        )
        print(json.dumps(result, indent=2))
    elif args.command == "reset-hwid":
        result = api.reset_hwid(args.username, args.password)
        print(json.dumps(result, indent=2))
    elif args.command == "delete":
        result = api.delete_user(args.username)
        print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
