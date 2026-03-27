@echo off
REM build.cmd -- Snowflake PPTX build wrapper for Windows cmd.exe
REM Usage: build.cmd <spec.yaml> <output.pptx>
REM Example: .cortex\skills\pptx\scripts\build.cmd spec.yaml output.pptx
setlocal

if "%~1"=="" (
    echo Usage: build.cmd ^<spec.yaml^> ^<output.pptx^>
    exit /b 1
)
if "%~2"=="" (
    echo Usage: build.cmd ^<spec.yaml^> ^<output.pptx^>
    exit /b 1
)

set SCRIPT_DIR=%~dp0
set SNOWFLAKE_TEMPLATE_PATH=%SCRIPT_DIR%snowflake\templates\SNOWFLAKE TEMPLATE JANUARY 2026.pptx
set DOTNET_DLL=%SCRIPT_DIR%dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll

dotnet "%DOTNET_DLL%" build --spec "%~1" --out "%~2"
endlocal
