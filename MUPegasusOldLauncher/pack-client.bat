@echo off
title MuEldryn - Montar pasta do client
set ROOT=%~dp0
set OUT=%ROOT%pack\client
set MUMAIN=%ROOT%..\MuMain\out\build\windows-x86\src\RelWithDebInfo

echo Criando %OUT% ...
if not exist "%OUT%" mkdir "%OUT%"
if not exist "%OUT%\Data\Local" mkdir "%OUT%\Data\Local"

copy /Y "%ROOT%Launcher\bin\Release\Launcher.exe" "%OUT%\"
copy /Y "%ROOT%Launcher\bin\Release\MuEldrynLaunch.exe" "%OUT%\"
copy /Y "%ROOT%Launcher\bin\Release\MuUpdater.exe" "%OUT%\"
copy /Y "%ROOT%Launcher\bin\Release\Data\Local\Launcher.bmd" "%OUT%\Data\Local\"
if exist "%ROOT%Launcher\bin\Release\imagebk2.jpg" copy /Y "%ROOT%Launcher\bin\Release\imagebk2.jpg" "%OUT%\"

if exist "%MUMAIN%\Main.exe" (
  echo Copiando Main.exe + dependencias de %MUMAIN%
  xcopy /E /I /Y "%MUMAIN%\*" "%OUT%\"
) else (
  echo AVISO: Main.exe nao encontrado em %MUMAIN%
  echo Copie manualmente o client compilado para %OUT%
)

echo.
echo Pronto: %OUT%
echo Publique updates em http://170.80.224.11/update/MiniUpdate/
pause
