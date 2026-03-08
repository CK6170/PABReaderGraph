@echo off
echo ================================================
echo PAB Reader Graph - Enterprise Deployment Script
echo Version 1.3.0 - Shekel Scales 2008 LTD
echo ================================================
echo.

set "INSTALL_DIR=C:\Program Files\PABReader"
set "DESKTOP_LINK=%USERPROFILE%\Desktop\PAB Reader Graph.lnk"

echo Installing PAB Reader Graph...
echo Installation Directory: %INSTALL_DIR%
echo.

:: Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    echo Created installation directory.
)

:: Copy executable
copy "PABReader.exe" "%INSTALL_DIR%\PABReader.exe" > nul
if %errorlevel% equ 0 (
    echo ✓ PABReader.exe copied successfully.
) else (
    echo ✗ Failed to copy PABReader.exe
    pause
    exit /b 1
)

:: Copy documentation
copy "README.md" "%INSTALL_DIR%\README.md" > nul
if %errorlevel% equ 0 (
    echo ✓ Documentation copied successfully.
)

:: Create desktop shortcut (using PowerShell)
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%DESKTOP_LINK%'); $Shortcut.TargetPath = '%INSTALL_DIR%\PABReader.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Description = 'PAB Reader Graph - Professional Load Cell Monitoring'; $Shortcut.Save()" 2>nul
if %errorlevel% equ 0 (
    echo ✓ Desktop shortcut created successfully.
)

echo.
echo ================================================
echo Installation completed successfully!
echo ================================================
echo.
echo Application installed to: %INSTALL_DIR%
echo Desktop shortcut created: PAB Reader Graph
echo.
echo To launch PAB Reader Graph:
echo 1. Double-click the desktop shortcut, or
echo 2. Navigate to %INSTALL_DIR% and run PABReader.exe
echo.
echo For documentation and support, see README.md
echo.
pause