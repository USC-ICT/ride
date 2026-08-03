@echo off
rem Start vLLM WITH retrieval augmentation.
rem
rem Brings up two containers - the chat model on 8000 and an embedding model on 8001 (vLLM serves
rem one model per process) - and then the RIDE RAG proxy on 8080. vLLM keeps its own port, so
rem point clients at http://127.0.0.1:8080/v1 to get retrieval, or 8000 for plain answers.
rem Use load.bat instead for a plain LLM setup.
rem
rem GPU budget: the chat service reserves 0.90 of the card by default and will not fit alongside
rem the embeddings one - lower it in entrypoint.sh (roughly 0.75) before running both.
rem
rem The logic lives in ..\rag-proxy\load.bat so there is only one copy of the start ordering.

cd /d "%~dp0"
call "..\rag-proxy\load.bat" vllm
