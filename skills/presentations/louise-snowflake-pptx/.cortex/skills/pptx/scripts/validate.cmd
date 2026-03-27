@echo off
REM validate.cmd -- Validate a rendered PPTX file using OpenXML validation
REM Usage: validate.cmd output.pptx
setlocal

if "%~1"=="" (
    echo Usage: validate.cmd ^<output.pptx^>
    exit /b 1
)

set SCRIPT_DIR=%~dp0
set DOTNET_DLL=%SCRIPT_DIR%dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll

dotnet "%DOTNET_DLL%" validate %*
endlocal
