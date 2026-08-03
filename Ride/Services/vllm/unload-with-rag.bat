@echo off
rem Stop the RAG proxy and both vLLM containers (chat + embeddings) together.

cd /d "%~dp0"
call "..\rag-proxy\unload.bat" vllm
