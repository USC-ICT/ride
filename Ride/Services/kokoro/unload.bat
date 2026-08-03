@echo off
cd /d "%~dp0"
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo Docker is not running. Start Docker Desktop and try again.
    exit /b 1
)
docker compose down
