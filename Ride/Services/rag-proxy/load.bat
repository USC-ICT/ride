@echo off
rem Start the RAG proxy, optionally bringing its upstream LLM endpoint up first.
rem
rem   load.bat            proxy only - the upstream must already be running
rem   load.bat ollama     Ollama on the internal port + embedding model, then the proxy
rem   load.bat vllm       vLLM chat + embeddings containers, then the proxy
rem
rem Ordering is why this script exists: the proxy binds the port clients use and forwards to the
rem upstream on an internal one, so the upstream has to be listening first. unload.bat reverses it.
rem
rem The per-service wrappers (..\ollama\load-with-rag.bat, ..\vllm\load-with-rag.bat) just call
rem this, so there is one copy of the logic.

setlocal
cd /d "%~dp0"

set "UPSTREAM=%~1"

tasklist /fi "imagename eq RagProxy.exe" | find /i "RagProxy.exe" >nul
if %errorlevel% equ 0 (
    echo RAG proxy is already running. Run unload.bat first to restart it.
    exit /b 1
)

if /i "%UPSTREAM%"=="" goto :proxy_only
if /i "%UPSTREAM%"=="ollama" goto :ollama
if /i "%UPSTREAM%"=="vllm" goto :vllm
echo Unknown upstream "%UPSTREAM%". Use: load.bat [ollama^|vllm]
exit /b 1

rem ---------------------------------------------------------------- Ollama
:ollama
echo [rag] configuring Ollama for RAG (internal port 11436 + embedding model)...
if not exist "..\ollama\.env" copy "..\ollama\.env.example" "..\ollama\.env" >nul

rem Rewrite only the two keys we own, so custom model choices in .env survive.
powershell -NoProfile -Command ^
  "$p='..\ollama\.env'; $t=@(Get-Content $p);" ^
  "if ($t -match '^^OLLAMA_PORT=') { $t = $t -replace '^^OLLAMA_PORT=.*','OLLAMA_PORT=11436' } else { $t += 'OLLAMA_PORT=11436' };" ^
  "if ($t -match '^^OLLAMA_MODEL_EMBED=') { $t = $t -replace '^^OLLAMA_MODEL_EMBED=.*','OLLAMA_MODEL_EMBED=nomic-embed-text' } else { $t += 'OLLAMA_MODEL_EMBED=nomic-embed-text' };" ^
  "Set-Content $p $t -Encoding ascii"
if %errorlevel% neq 0 ( echo Failed to update ..\ollama\.env & exit /b 1 )

call "..\ollama\load.bat"
if %errorlevel% neq 0 exit /b 1
cd /d "%~dp0"

set "RAG_PORT=11434"
set "UPSTREAM_URL=http://127.0.0.1:11436"
set "EMBED_URL=http://127.0.0.1:11436"
set "EMBED_MODEL=nomic-embed-text"

echo [rag] waiting for Ollama on 11436 ^(first run pulls models, this can take minutes^)...
call :wait_for "http://127.0.0.1:11436/v1/models" 120
if %errorlevel% neq 0 exit /b 1
goto :proxy

rem ---------------------------------------------------------------- vLLM
:vllm
echo [rag] starting vLLM chat + embeddings containers...
if not exist "..\vllm\.env" copy "..\vllm\.env.example" "..\vllm\.env" >nul

rem The embeddings container lives in the "rag" compose profile; COMPOSE_PROFILES enables it
rem without needing a separate load script on the vLLM side.
set "COMPOSE_PROFILES=rag"
call "..\vllm\load.bat"
if %errorlevel% neq 0 exit /b 1
cd /d "%~dp0"

set "RAG_PORT=8080"
set "UPSTREAM_URL=http://127.0.0.1:8000"
set "EMBED_URL=http://127.0.0.1:8001"
set "EMBED_MODEL=vhtoolkit-embed"

echo [rag] waiting for vLLM chat on 8000...
call :wait_for "http://127.0.0.1:8000/health" 180
if %errorlevel% neq 0 exit /b 1
echo [rag] waiting for vLLM embeddings on 8001...
call :wait_for "http://127.0.0.1:8001/health" 180
if %errorlevel% neq 0 exit /b 1
echo [rag] NOTE: the proxy is on 8080, not 8000 - vLLM keeps its own port, so point clients at
echo [rag]       http://127.0.0.1:8080/v1 to get retrieval.
goto :proxy

rem ---------------------------------------------------------------- proxy
:proxy_only
echo [rag] starting proxy only - assuming the upstream is already up.

:proxy
dotnet build --nologo -v quiet
if %errorlevel% neq 0 (
    echo Build failed. If the error mentions a locked RagProxy.exe, the proxy is still
    echo running -- run unload.bat first.
    exit /b 1
)

start "RIDE RAG proxy" /min dotnet run --no-build
echo [rag] proxy starting. Check http://127.0.0.1:%RAG_PORT%/rag/status for index state
echo [rag] ^(semanticReady true means the embedding endpoint answered^).
exit /b 0

rem ---------------------------------------------------------------- helpers
rem :wait_for <url> <attempts>  - polls once a second, returns 1 on timeout
:wait_for
set "URL=%~1"
set /a ATTEMPTS=%~2
set /a TRIES=0
:wait_loop
curl -sf -o nul "%URL%" 2>nul
if %errorlevel% equ 0 exit /b 0
set /a TRIES+=1
if %TRIES% geq %ATTEMPTS% (
    echo [rag] timed out waiting for %URL%
    exit /b 1
)
timeout /t 1 /nobreak >nul
goto :wait_loop
