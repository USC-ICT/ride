#!/usr/bin/env sh
set -eu

python -m app.download_models
exec "$@"
