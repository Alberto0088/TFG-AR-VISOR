@echo off
title TFG AR Visor - GPS Tools Launcher

REM ============================================================
REM TFG AR VISOR - GPS TOOLS LAUNCHER
REM ------------------------------------------------------------
REM Este script abre automaticamente las herramientas necesarias
REM para probar el envio de GPS desde el smartphone hacia Unity.
REM
REM 1. Abre el servidor local Python en el puerto 5000.
REM 2. Abre ngrok para exponer ese servidor mediante HTTPS.
REM
REM El movil enviara el GPS a la URL HTTPS de ngrok.
REM Unity leera los datos desde http://127.0.0.1:5000/latest
REM ============================================================

set "REPO_DIR=%~dp0"

echo Iniciando servidor GPS...
start "GPS Server" powershell -NoExit -Command "Set-Location -LiteralPath '%REPO_DIR%'; python 'tools/gps-server/gps_server.py'"

timeout /t 2 > nul

echo Iniciando ngrok...
start "ngrok GPS Tunnel" powershell -NoExit -Command "& 'D:\ngrok.exe' http 5000"

echo.
echo Herramientas iniciadas.
echo.
echo 1. Copia la URL HTTPS que aparezca en la ventana de ngrok.
echo 2. Abrela en el movil.
echo 3. Permite la ubicacion.
echo 4. En Unity usa: http://127.0.0.1:5000/latest
echo.
pause