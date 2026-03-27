@echo off
REM build-and-qa.cmd -- Full pipeline: validate-spec -> build -> validate -> export slides for QA
REM Usage: build-and-qa.cmd spec.yaml output.pptx
setlocal

if "%~1"=="" goto :usage
if "%~2"=="" goto :usage

set SCRIPT_DIR=%~dp0
set DOTNET_DLL=%SCRIPT_DIR%dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll
set SNOWFLAKE_TEMPLATE_PATH=%SCRIPT_DIR%snowflake\templates\SNOWFLAKE TEMPLATE JANUARY 2026.pptx

echo [1/4] Validating spec...
dotnet "%DOTNET_DLL%" validate-spec --strict "%~1"
if errorlevel 1 (
    echo [FAIL] Spec validation failed. Fix errors and warnings before building.
    exit /b 1
)

echo [2/4] Building PPTX...
dotnet "%DOTNET_DLL%" build --spec "%~1" --out "%~2"
if errorlevel 1 (
    echo [FAIL] Build failed.
    exit /b 1
)

echo [3/4] Validating output PPTX...
dotnet "%DOTNET_DLL%" validate "%~2"
if errorlevel 1 (
    echo [FAIL] Output PPTX has structural errors.
    exit /b 1
)

echo [4/4] Exporting slides for visual QA...
if not exist "temp\qa" mkdir "temp\qa"
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%export-slides.ps1" -PptxPath "%~2" -OutDir "temp\qa"
if errorlevel 1 (
    echo [WARN] Slide export failed -- run export-slides.ps1 manually.
) else (
    echo [OK] Slides exported to temp\qa\. Visually inspect all slides before declaring success.
)

echo.
echo *** REMINDER: Visually inspect all exported slides before declaring the task complete. ***
exit /b 0

:usage
echo Usage: build-and-qa.cmd ^<spec.yaml^> ^<output.pptx^>
exit /b 1
