# PAB Reader Installer Instructions

## Prerequisites

1. **Download and Install Inno Setup 6**
   - Go to: https://jrsoftware.org/isinfo.php
   - Download and install the latest version of Inno Setup
   - Make sure it's installed in the default location: `C:\Program Files (x86)\Inno Setup 6\`

## Building the Installer

### Option 1: Automatic Build (Recommended)
1. Run `Build_Installer.bat`
2. The script will:
   - Build the release version
   - Create the installer automatically
   - Output will be in `.\Installer_Output\PABReader_Setup_v1.1.0.exe`

### Option 2: Manual Build
1. First, build the release:
   ```
   dotnet publish PABReaderGraph.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o bin\Release\SingleFile
   ```

2. Open `PABReader_Installer.iss` in Inno Setup
3. Click "Build" → "Compile"
4. The installer will be created in `.\Installer_Output\`

## Installer Features

✅ **Professional Windows Installer**
- Proper installation to Program Files
- Start Menu shortcuts
- Optional Desktop shortcut
- Automatic uninstaller
- Version checking and upgrade support
- Requires admin privileges (for proper installation)

✅ **Includes All Dependencies**
- Main executable (PABReader.exe)
- All required DLL files
- Application icon
- Debug symbols (PABReader.pdb)

✅ **Smart Installation**
- Detects and uninstalls older versions automatically
- 64-bit Windows 10+ support
- LZMA2 compression for smaller file size

## Customization

Edit `PABReader_Installer.iss` to customize:
- Company information
- Installation directory
- File associations
- Registry entries
- Additional files or documentation

## File Structure After Installation

```
C:\Program Files\PAB Reader\
├── PABReader.exe (main executable)
├── *.dll (all required libraries)
├── PABReader.pdb (debug symbols)
├── Shekel.ico (application icon)
└── settings.json (created by application)
```

## Distribution

The final installer (`PABReader_Setup_v1.1.0.exe`) can be distributed to end users. It's completely self-contained and includes everything needed to run the application.

## Troubleshooting

**Error: "Inno Setup not found"**
- Install Inno Setup from the official website
- Make sure it's installed in the default location

**Error: "Build failed"**
- Check that .NET 8 SDK is installed
- Verify all source files are present
- Run `dotnet restore` first if needed

**Installer won't run on target machine**
- Ensure target machine is Windows 10 or later (64-bit)
- User must have administrator privileges to install