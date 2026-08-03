@echo off
cd /d "%~dp0"
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo Docker is not running. Start Docker Desktop and try again.
    exit /b 1
)
if not exist .env (
    copy .env.example .env
    echo Created .env from .env.example -- edit it before re-running if needed.
)
docker compose up -d --build
if %errorlevel% neq 0 exit /b 1

rem Report the port actually in use: load-with-rag.bat moves Ollama to 11436 and puts the RAG
rem proxy on 11434, and a plain load.bat afterwards would otherwise look like it had failed.
findstr /b /c:"OLLAMA_PORT=11436" .env >nul
if %errorlevel% equ 0 (
    echo.
    echo NOTE: .env still has OLLAMA_PORT=11436 from a RAG run, so Ollama is NOT on its
    echo       canonical port. Run unload-with-rag.bat to restore 11434, or edit .env.
)
