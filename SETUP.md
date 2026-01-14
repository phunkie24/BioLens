# BioLens Setup Guide

## Quick Start (5 Minutes)

### 1. Prerequisites

**Required:**
- .NET 10.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2024 or VS Code
- Gemini 3 API Key ([Get from Google AI Studio](https://makersuite.google.com/app/apikey))

**Optional (for mobile development):**
- Android SDK (for Android development)
- Xcode (for iOS development, macOS only)

### 2. Clone and Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/biolens.git
cd biolens

# Restore NuGet packages
dotnet restore

# Trust the HTTPS development certificate
dotnet dev-certs https --trust
```

### 3. Configure Gemini API Key

**Option A: User Secrets (Recommended for Development)**
```bash
cd src/BioLens.Infrastructure
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY_HERE"
```

**Option B: appsettings.json (Not recommended - do not commit!)**
```json
{
  "Gemini": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

### 4. Initialize Database

```bash
# From solution root
cd src/BioLens.Infrastructure
dotnet ef database update --startup-project ../BioLens.Mobile
```

### 5. Run the Application

**Mobile App:**
```bash
cd src/BioLens.Mobile
dotnet build
dotnet run
```

**API (Optional Backend):**
```bash
cd src/BioLens.API
dotnet run
```

## Detailed Setup

### Setting Up Development Environment

#### Visual Studio 2024

1. Install Visual Studio 2024 with:
   - .NET Multi-platform App UI development
   - Mobile development with .NET
   - ASP.NET and web development

2. Open `BioLens.sln`

3. Set startup project:
   - Right-click `BioLens.Mobile`
   - Select "Set as Startup Project"

4. Select target platform (Android/iOS)

5. Press F5 to run

#### VS Code

1. Install extensions:
   - C# Dev Kit
   - .NET MAUI
   - SQLite Viewer

2. Open folder: `File → Open Folder → BioLens`

3. Run with: `Ctrl+F5`

### Android Development Setup

#### Windows/macOS/Linux

```bash
# Install Android SDK via Visual Studio Installer or:
dotnet workload install android

# List available Android emulators
dotnet build -t:Run -f net10.0-android

# Run on specific emulator
dotnet build -t:Run -f net10.0-android -p:AndroidEmulator="pixel_5_-_api_33"
```

#### Troubleshooting Android

**Issue**: "Android SDK not found"
```bash
# Set ANDROID_HOME environment variable
export ANDROID_HOME=$HOME/Library/Android/sdk  # macOS
export ANDROID_HOME=$HOME/Android/Sdk          # Linux
```

**Issue**: "Emulator doesn't start"
```bash
# Enable virtualization in BIOS
# Or use physical device with USB debugging enabled
```

### iOS Development Setup (macOS Only)

```bash
# Install iOS workload
dotnet workload install ios

# List available simulators
xcrun simctl list devices

# Run on simulator
dotnet build -t:Run -f net10.0-ios
```

### Database Setup

#### SQLite Location

**Development:**
- Windows: `%LOCALAPPDATA%\BioLens\biolens.db`
- macOS: `~/Library/Application Support/BioLens/biolens.db`
- Android: `/data/data/com.biolens.mobile/files/biolens.db`
- iOS: `~/Library/BioLens/biolens.db`

#### Viewing Database

**Option 1: DB Browser for SQLite**
1. Download from [sqlitebrowser.org](https://sqlitebrowser.org/)
2. Open database file
3. Browse data visually

**Option 2: Command Line**
```bash
sqlite3 biolens.db
.tables
SELECT * FROM DiagnosticCases;
.quit
```

#### Database Migrations

```bash
# Create new migration
cd src/BioLens.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../BioLens.Mobile

# Apply migration
dotnet ef database update --startup-project ../BioLens.Mobile

# Rollback migration
dotnet ef database update PreviousMigrationName --startup-project ../BioLens.Mobile

# Remove last migration
dotnet ef migrations remove --startup-project ../BioLens.Mobile
```

## Configuration

### appsettings.json

```json
{
  "Gemini": {
    "ApiKey": "YOUR_KEY_HERE",
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "Model": "gemini-3-pro",
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  },
  "Database": {
    "ConnectionString": "Data Source=biolens.db",
    "EnableSensitiveDataLogging": false
  },
  "Offline": {
    "EnableMLCache": true,
    "CacheConfidenceThreshold": 0.7,
    "ModelPath": "models/offline-diagnostic.mlnet"
  },
  "Sync": {
    "AutoSyncEnabled": true,
    "SyncIntervalMinutes": 30,
    "ConflictResolution": "LastWriteWins"
  },
  "Security": {
    "EncryptionEnabled": true,
    "RequireAuthentication": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BioLens.Agents": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment-Specific Configuration

Create additional configuration files:
- `appsettings.Development.json`
- `appsettings.Production.json`
- `appsettings.Staging.json`

Example `appsettings.Development.json`:
```json
{
  "Database": {
    "EnableSensitiveDataLogging": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Testing

### Run Unit Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/BioLens.Domain.Tests

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Integration Tests

```bash
# Requires Docker for test databases
docker-compose -f tests/docker-compose.test.yml up -d

# Run integration tests
dotnet test tests/BioLens.Integration.Tests

# Cleanup
docker-compose -f tests/docker-compose.test.yml down
```

### Manual Testing

#### Test Case 1: Visual Diagnosis
1. Launch app
2. Create new case
3. Add patient info (Age: 5, Sex: Female)
4. Capture/upload skin rash image
5. Record audio: "Child has red itchy rash for 2 days"
6. Set location context (e.g., Kenya)
7. Request diagnosis
8. Verify: Diagnosis returned with treatment protocol

#### Test Case 2: Offline Mode
1. Enable airplane mode
2. Create new case
3. Add images and audio
4. Request diagnosis
5. Verify: Offline diagnosis provided
6. Disable airplane mode
7. Verify: Case syncs to cloud

## Troubleshooting

### Common Issues

#### Build Errors

**Error**: "The type or namespace name 'X' could not be found"
```bash
# Solution: Clean and restore
dotnet clean
dotnet restore
dotnet build
```

#### Runtime Errors

**Error**: "Unable to connect to Gemini API"
- Check API key configuration
- Verify internet connection
- Check API rate limits

**Error**: "Database locked"
```bash
# Solution: Close all connections and restart
rm biolens.db-wal biolens.db-shm
```

#### Agent Errors

**Error**: "Agent execution timeout"
- Check internet connectivity
- Increase timeout in configuration
- Enable offline mode

### Debug Logging

Enable detailed logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "BioLens.Agents": "Trace"
    }
  }
}
```

View logs:
- Visual Studio: Output window
- VS Code: Debug Console
- File: `logs/biolens-{date}.log`

## Performance Tuning

### Optimize for Low-End Devices

```json
{
  "Performance": {
    "MaxParallelAgents": 2,
    "ImageMaxResolution": 1024,
    "EnableImageCompression": true,
    "CacheSize": "50MB"
  }
}
```

### Network Optimization

```json
{
  "Network": {
    "EnableCompression": true,
    "RequestTimeout": 60,
    "MaxRetries": 5,
    "BackoffMultiplier": 2
  }
}
```

## Deployment

### Mobile App Deployment

#### Android (Google Play)

```bash
# Create release build
dotnet build -c Release -f net10.0-android

# Sign APK
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 \
  -keystore my-release-key.keystore \
  bin/Release/net10.0-android/com.biolens.mobile-Signed.apk \
  alias_name

# Create AAB for Play Store
dotnet publish -c Release -f net10.0-android \
  -p:AndroidPackageFormat=aab
```

#### iOS (App Store)

```bash
# Create archive
dotnet build -c Release -f net10.0-ios

# Submit to App Store via Xcode
open bin/Release/net10.0-ios/BioLens.iOS.app
```

### Backend API Deployment

#### Azure App Service

```bash
az login
az group create --name biolens-rg --location eastus
az appservice plan create --name biolens-plan --resource-group biolens-rg
az webapp create --name biolens-api --plan biolens-plan --resource-group biolens-rg

dotnet publish -c Release -o ./publish
cd publish
zip -r deploy.zip .
az webapp deployment source config-zip --resource-group biolens-rg --name biolens-api --src deploy.zip
```

#### Docker

```bash
# Build image
docker build -t biolens-api:latest .

# Run container
docker run -d -p 8080:80 \
  -e Gemini__ApiKey="YOUR_KEY" \
  biolens-api:latest
```

## Support

### Getting Help

1. **Documentation**: Check README.md and ARCHITECTURE.md
2. **Issues**: Open GitHub issue with:
   - Error message
   - Steps to reproduce
   - Environment info (`dotnet --info`)
3. **Community**: Join Discord/Slack channel

### Reporting Bugs

Include:
- BioLens version
- .NET version
- Operating system
- Device model (if mobile)
- Log files
- Screenshots

---

**Need more help?** Contact support@biolens.com
