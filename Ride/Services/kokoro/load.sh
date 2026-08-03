#!/bin/sh
cd "$(dirname "$0")"
if ! docker info >/dev/null 2>&1; then
    echo "Docker is not running. Start Docker Desktop (or the Docker daemon) and try again."
    exit 1
fi
if [ ! -f .env ]; then
    cp .env.example .env
    echo "Created .env from .env.example -- edit it before re-running if needed."
fi
docker compose up -d --build
