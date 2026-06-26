#!/usr/bin/env bash
set -euo pipefail

APP_DIR="/home/specials/projects/MarketZone/mzSpecials/build"
APP_BIN="$APP_DIR/mzSpecials"
STATE_DIR="${XDG_STATE_HOME:-$HOME/.local/state}/mzSpecials"
LOCK_FILE="$STATE_DIR/mzSpecials.lock"
LOG_FILE="$STATE_DIR/mzSpecials.log"

mkdir -p "$STATE_DIR"

exec 9>"$LOCK_FILE"
if ! flock -n 9; then
    exit 0
fi

export DISPLAY="${DISPLAY:-:0}"

cd "$APP_DIR"
exec "$APP_BIN" >>"$LOG_FILE" 2>&1
