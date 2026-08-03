@echo off
rem Stop the RAG proxy, optionally taking its upstream down with it.
rem
rem   unload.bat          proxy only
rem   unload.bat ollama   proxy, then Ollama - and restore Ollama's canonical port for
rem                       non-RAG clients, so the next plain load.bat behaves normally
rem   unload.bat vllm     proxy, then both vLLM containers
rem
rem Always run this before rebuilding: a running proxy holds bin\RagProxy.exe and the build
rem fails on the copy step (MSB3027) rather than on any compile error.

setlocal
cd /d "%~dp0"

set "UPSTREAM=%~1"

taskkill /f /im RagProxy.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo [rag] proxy stopped.
) else (
    echo [rag] proxy was not running.
)

if /i "%UPSTREAM%"=="" exit /b 0
if /i "%UPSTREAM%"=="ollama" goto :ollama
if /i "%UPSTREAM%"=="vllm" goto :vllm
echo Unknown upstream "%UPSTREAM%". Use: unload.bat [ollama^|vllm]
exit /b 1

:ollama
call "..\ollama\unload.bat"
cd /d "%~dp0"
echo [rag] restoring Ollama's canonical port 11434 in ..\ollama\.env
powershell -NoProfile -Command ^
  "$p='..\ollama\.env'; if (Test-Path $p) { $t=@(Get-Content $p) -replace '^^OLLAMA_PORT=.*','OLLAMA_PORT=11434'; Set-Content $p $t -Encoding ascii }"
exit /b 0

:vllm
set "COMPOSE_PROFILES=rag"
call "..\vllm\unload.bat"
exit /b 0
