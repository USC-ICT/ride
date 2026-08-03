@echo off
rem Start Ollama WITH retrieval augmentation.
rem
rem Moves Ollama to internal port 11436, pulls the embedding model, and puts the RIDE RAG proxy
rem on Ollama's canonical 11434 - so existing clients keep their normal configuration and gain
rem retrieval transparently. Use load.bat instead for a plain LLM setup on 11434.
rem
rem The logic lives in ..\rag-proxy\load.bat so there is only one copy of the start ordering.

cd /d "%~dp0"
call "..\rag-proxy\load.bat" ollama
