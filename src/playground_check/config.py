from __future__ import annotations

import base64
import os
from dataclasses import dataclass
from pathlib import Path

from dotenv import load_dotenv


PROJECT_ROOT = Path(__file__).resolve().parents[2]


def load_environment(path: Path | None = None) -> None:
    """Load project-local settings without overriding the process environment."""
    dotenv_path = path or Path.cwd() / ".env"
    if path is None and not dotenv_path.is_file():
        dotenv_path = PROJECT_ROOT / ".env"
    load_dotenv(dotenv_path, override=False)


load_environment()


def _bool(value: str | None, default: bool = False) -> bool:
    if value is None:
        return default
    return value.strip().lower() in {"1", "true", "yes", "on"}


def _npgsql_to_libpq(value: str) -> str:
    """Accept the original Npgsql key/value connection-string format as well."""
    if not value or ";" not in value:
        return value
    names = {
        "host": "host", "server": "host", "port": "port",
        "database": "dbname", "username": "user", "user id": "user",
        "password": "password", "ssl mode": "sslmode", "search path": "options",
    }
    result: list[str] = []
    for part in value.split(";"):
        if "=" not in part:
            continue
        key, raw = part.split("=", 1)
        target = names.get(key.strip().lower())
        if not target:
            continue
        raw = raw.strip().replace("\\", "\\\\").replace("'", "\\'")
        if target == "options":
            raw = f"-c search_path={raw}"
        result.append(f"{target}='{raw}'")
    return " ".join(result)


@dataclass(frozen=True)
class Settings:
    root: Path
    host: str
    port: int
    postgres_dsn: str
    security_key: str
    salt: bytes
    title: str
    wms_url: str
    token_issuer: str
    compatibility_bugs: bool
    service_worker_enabled: bool

    @classmethod
    def load(cls) -> "Settings":
        salt_raw = os.getenv("PLAYGROUND_SALT_BASE64", "cGxheWdyb3VuZC1jaGVjay1zYWx0")
        salt_raw = os.getenv("SaltBase64String", salt_raw)
        try:
            salt = base64.b64decode(salt_raw)
        except Exception:
            salt = b"playground-check-salt"
        return cls(
            root=PROJECT_ROOT,
            host=os.getenv("PLAYGROUND_HOST", "127.0.0.1"),
            port=int(os.getenv("PLAYGROUND_PORT", "8010")),
            postgres_dsn=_npgsql_to_libpq(os.getenv("PLAYGROUND_POSTGRES_DSN", os.getenv("Postgres__ConnectionString", ""))),
            security_key=os.getenv("PLAYGROUND_SECURITY_KEY", os.getenv("SecurityKey", "development-only-security-key")),
            salt=salt,
            title=os.getenv("PLAYGROUND_TITLE", "Spielplatzkontrolle"),
            wms_url=os.getenv("PLAYGROUND_WMS_URL", os.getenv("WMS__ServiceUrl", "http://stadtplan.winterthur.ch/wms/Spielplatzkarte")),
            token_issuer=os.getenv(
                "PLAYGROUND_SERVICE_URL",
                os.getenv("URL__ServiceDomain", "") + os.getenv("URL__ServiceBasePath", "/"),
            ),
            compatibility_bugs=_bool(os.getenv("PLAYGROUND_COMPATIBILITY_BUGS"), True),
            service_worker_enabled=_bool(os.getenv("PLAYGROUND_SERVICE_WORKER_ENABLED"), True),
        )


settings = Settings.load()
