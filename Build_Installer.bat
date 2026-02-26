@echo off
echo ========================================
echo PAB Reader Installer Builder
echo ========================================
echo.

REM Check if Inno Setup is installed
if not exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    echo ERROR: Inno Setup 6 not found!
    echo Please download and install Inno Setup from: https://jrsoftware.org/isinfo.php
    echo.
    pause
    exit /b 1
)

REM Build the release first
echo Building release version...
dotnet publish PABReaderGraph.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o bin\Release\SingleFile
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo Build completed successfully.
echo.

REM Create installer
echo Creating installer...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" PABReader_Installer.iss
if errorlevel 1 (
    echo ERROR: Installer creation failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS! Installer created in .\Installer_Output\
echo ========================================
echo.
pause