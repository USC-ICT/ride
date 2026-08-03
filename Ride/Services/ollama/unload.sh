#!/bin/sh
cd "$(dirname "$0")"
if ! docker info >/dev/null 2>&1; then
    echo "Docker is not running. Start Docker Desktop (or the Docker daemon) and try again."
    exit 1
fi
docker compose down
