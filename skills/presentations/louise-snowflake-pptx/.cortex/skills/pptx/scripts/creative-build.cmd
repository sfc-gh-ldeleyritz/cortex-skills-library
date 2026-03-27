@echo off
setlocal

:: Creative PPTX build wrapper for Windows
:: Usage: creative-build.cmd build --spec spec.yaml --out output.pptx
::        creative-build.cmd validate spec.yaml

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%node\SnowflakeCreativePptx"

:: Auto-install node_modules if missing
if not exist "%PROJECT_DIR%\node_modules\" (
    echo [creative-build] Installing dependencies...
    pushd "%PROJECT_DIR%"
    call npm install
    popd
    if errorlevel 1 (
        echo [creative-build] npm install failed.
        exit /b 1
    )
)

:: Forward all arguments to the Node.js entry point
node "%PROJECT_DIR%\index.js" %*
exit /b %errorlevel%
