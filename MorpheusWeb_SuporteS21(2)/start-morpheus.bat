@echo off
cd /d "%~dp0"
echo Starting Morpheus on http://127.0.0.1:8090
echo Site:   http://127.0.0.1:8090/
echo Admin:  http://127.0.0.1:8090/admin/login
echo User: admin / Pass: 12345
C:\php74\php.exe -S 127.0.0.1:8090 router.php
