@echo off
rem Stop the RAG proxy and Ollama together, and restore Ollama's canonical port 11434 so the
rem next plain load.bat behaves normally.

cd /d "%~dp0"
call "..\rag-proxy\unload.bat" ollama
