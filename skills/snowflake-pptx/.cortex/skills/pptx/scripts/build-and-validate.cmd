@echo off
REM build-and-validate.cmd -- Full pipeline: validate-spec -> build -> validate
REM Usage: build-and-validate.cmd spec.yaml output.pptx
setlocal

if "%~1"=="" goto :usage
if "%~2"=="" goto :usage

set SCRIPT_DIR=%~dp0
set DOTNET_DLL=%SCRIPT_DIR%dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll
set SNOWFLAKE_TEMPLATE_PATH=%SCRIPT_DIR%snowflake\templates\SNOWFLAKE TEMPLATE JANUARY 2026.pptx

echo [1/3] Validating spec...
dotnet "%DOTNET_DLL%" validate-spec --strict "%~1"
if errorlevel 1 (
    echo [FAIL] Spec validation failed. Fix errors and warnings before building.
    exit /b 1
)

echo [2/3] Building PPTX...
dotnet "%DOTNET_DLL%" build --spec "%~1" --out "%~2"
if errorlevel 1 (
    echo [FAIL] Build failed.
    exit /b 1
)

echo [3/3] Validating output PPTX...
dotnet "%DOTNET_DLL%" validate "%~2"
if errorlevel 1 (
    echo [FAIL] Output PPTX has structural errors.
    exit /b 1
)

echo [OK] All steps passed.
exit /b 0

:usage
echo Usage: build-and-validate.cmd ^<spec.yaml^> ^<output.pptx^>
exit /b 1
