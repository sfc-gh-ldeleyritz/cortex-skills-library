@echo off
REM validate-spec.cmd -- Validate a YAML spec file before building
REM Usage: validate-spec.cmd spec.yaml [--strict]
setlocal

if "%~1"=="" (
    echo Usage: validate-spec.cmd ^<spec.yaml^> [--strict]
    exit /b 1
)

set SCRIPT_DIR=%~dp0
set DOTNET_DLL=%SCRIPT_DIR%dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll

dotnet "%DOTNET_DLL%" validate-spec %*
endlocal
