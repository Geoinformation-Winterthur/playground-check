#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")"
if [ ! -x .venv/bin/python ]; then
  python3 -m venv .venv
fi
if ! .venv/bin/python -c "import psycopg, dotenv" >/dev/null 2>&1; then
  .venv/bin/python -m pip install -e .
fi
exec .venv/bin/python -m playground_check --host "${PLAYGROUND_HOST:-127.0.0.1}" --port "${PLAYGROUND_PORT:-8010}"
