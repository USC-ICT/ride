@echo off
powershell -NoProfile -Command "Invoke-RestMethod http://127.0.0.1:5101/health | ConvertTo-Json"
