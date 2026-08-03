@echo off
docker compose -f compose.yaml -f compose.gpu.yaml up -d --build
