# SQL Server Backup Configurator

A Windows desktop application for configuring automated SQL Server backups with Azure Blob Storage integration.

## Overview

This application simplifies the setup and management of SQL Server backup jobs using SQL Server Agent. It creates automated jobs for full backups, differential backups, and cleanup of old backup files, with automatic upload to Azure Blob Storage using AzCopy.

## Features

- **Test SQL Connection**: Validates SQL Server connectivity, database status, and permissions
- **Test Azure Connection**: Verifies Azure Blob Storage access with SAS token
- **Install/Configure Jobs**: Creates or updates 3 SQL Agent jobs (idempotent operation):
  - Full weekly backup
  - Differential backup every N hours
  - Daily cleanup of old local backups
- **Remove All**: Removes all SQL Agent jobs and optionally deletes local backup folders
- **Configuration Management**: Save and load configuration from disk

## Prerequisites

### Required Software

1. **.NET 8 Runtime or SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0

2. **SQL Server 2019 (or later)**
   - SQL Server Agent must be running
   - SQL Authentication enabled

3. **AzCopy**
   - Download from: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10
   - Default path: `C:\Program Files\Microsoft\AzCopy\azcopy.exe`
   - Can be installed via Windows Package Manager: `winget install Microsoft.AzCopy`

4. **Azure Storage Account**
   - Container already created
   - SAS token with Read, Write, Delete, and List permissions
   - Recommended: 1 year expiration minimum

### SQL Server Permissions

The SQL user must have:
- **Recommended**: `sysadmin` role
- **Minimum**:
  - `db_backupoperator` role on the target database
  - `SQLAgentUserRole` in `msdb` database
  - Permission to create/delete jobs in SQL Agent

### Azure Storage Setup

1. Create a Storage Account (if not exists)
2. Create a Container (e.g., `sqlbackups`)
3. Generate a SAS token with:
   - Permissions: Read, Write, Delete, List
   - Allowed resource types: Container and Object
   - Expiration: At least 1 year
4. Note the Container URL: `https://<storageaccount>.blob.core.windows.net/<container>`

## Building the Application

```bash
# Clone the repository
git clone https://github.com/Juanprojas1982/BackupConfigurator.git
cd BackupConfigurator

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application
dotnet run --project BackupConfigurator.UI --configuration Release
```

The compiled executable will be in: `BackupConfigurator.UI/bin/Release/net8.0-windows/BackupConfigurator.UI.exe`

## Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| **Institution NIT** | Unique identifier for the institution | `123456789` |
| **SQL Server** | Server hostname/instance | `localhost\SQLEXPRESS` or `10.0.0.5,1433` |
| **SQL User** | SQL Authentication username | `sa` or `backup_user` |
| **SQL Password** | SQL Authentication password | `YourStrongPassword!` |
| **Database Name** | Target database to backup | `ProductionDB` |
| **Differential Interval (hours)** | Hours between differential backups | `6` (1-24) |
| **Full Backup Day** | Day of week for full backup | `Sunday` |
| **Full Backup Time** | Time for full backup (HH:mm) | `02:00` |
| **Local Base Path** | Root path for local backups | `D:\SqlBackups` |
| **Local Retention (days)** | Days to keep local backups | `14` |
| **Azure Container URL** | Azure Blob container URL | `https://mystorageaccount.blob.core.windows.net/sqlbackups` |
| **Azure SAS Token** | SAS token (with or without ?) | `?sv=2021-06-08&ss=b&srt=sco&sp=rwdl...` |
| **AzCopy Path** | Path to azcopy.exe | `C:\Program Files\Microsoft\AzCopy\azcopy.exe` |

## Usage

### 1. Configure Parameters

Open the application and fill in all configuration parameters on the **Configuration** tab.

### 2. Test Connections

Before installing jobs, verify connectivity:

- Click **Test SQL Connection** to validate:
  - SQL Server is accessible
  - Database exists and is ONLINE
  - msdb database is accessible (SQL Agent)
  - User has appropriate permissions

- Click **Test Azure Connection** to validate:
  - Azure container is accessible
  - SAS token has proper permissions
  - Can write and delete test blobs

### 3. Install/Configure Jobs

Click **Install/Configure Jobs** to:
1. Validate AzCopy installation
2. Create local backup folders
3. Generate upload and cleanup scripts
4. Create 3 SQL Agent jobs with schedules
5. Save configuration to disk

**Note**: This operation is idempotent - running it multiple times will update existing jobs.

### 4. Verify Jobs

Open SQL Server Management Studio (SSMS):
1. Connect to your SQL Server
2. Navigate to: SQL Server Agent → Jobs
3. Verify the following jobs exist:
   - `BK_{NIT}_{Database}_FULL_WEEKLY`
   - `BK_{NIT}_{Database}_DIFF_EVERY_{N}H`
   - `BK_{NIT}_{Database}_CLEANUP_DAILY`

### 5. Monitor Backups

- **Job History**: In SSMS, right-click on a job → View History
- **Local Backups**: Check `{LocalBasePath}\{NIT}\{Database}\FULL` and `DIFF` folders
- **Azure Backups**: Use Azure Storage Explorer or Azure Portal to verify blobs are being uploaded
- **Application Logs**: Check the **Logs** tab or `C:\ProgramData\BackupConfigurator\logs\`

## File and Folder Structure

### Local Folders Created

```
{LocalBasePath}\
  └── {NIT}\
      └── {DatabaseName}\
          ├── FULL\          (Full backup files: *.bak)
          └── DIFF\          (Differential backup files: *.dif)
```

### Application Data

```
C:\ProgramData\BackupConfigurator\
  ├── config.json                    (Configuration saved here)
  ├── logs\
  │   └── log-YYYYMMDD.txt          (Daily log files)
  └── scripts\
      ├── upload_full.cmd            (Script to upload full backups)
      ├── upload_diff.cmd            (Script to upload differential backups)
      └── cleanup_local.cmd          (Script to cleanup old backups)
```

### Azure Blob Structure

```
{Container}/
  └── {NIT}/
      └── {DatabaseName}/
          ├── FULL/          (Full backup blobs)
          └── DIFF/          (Differential backup blobs)
```

## Backup File Naming Convention

- **Full Backup**: `{NIT}_{Database}_FULL_YYYYMMDD_HHmmss.bak`
- **Differential Backup**: `{NIT}_{Database}_DIFF_YYYYMMDD_HHmmss.dif`

Example: `123456789_ProductionDB_FULL_20231225_020015.bak`

## SQL Agent Job Details

### Full Backup Job (`BK_{NIT}_{Database}_FULL_WEEKLY`)

**Schedule**: Weekly on configured day and time (default: Sunday 02:00)

**Steps**:
1. **Backup Database**: T-SQL backup with COMPRESSION, CHECKSUM, INIT
2. **Upload to Azure**: Executes `upload_full.cmd` to upload .bak files via AzCopy

### Differential Backup Job (`BK_{NIT}_{Database}_DIFF_EVERY_{N}H`)

**Schedule**: Daily, every N hours (configurable, default: 6 hours)

**Steps**:
1. **Backup Database**: T-SQL differential backup with COMPRESSION, CHECKSUM, INIT
2. **Upload to Azure**: Executes `upload_diff.cmd` to upload .dif files via AzCopy

### Cleanup Job (`BK_{NIT}_{Database}_CLEANUP_DAILY`)

**Schedule**: Daily at 03:30

**Steps**:
1. **Cleanup Old Backups**: PowerShell script removes files older than configured retention days (default: 14 days)

## Troubleshooting

### "SQL Server connection failed"

- Verify SQL Server is running and accessible
- Check SQL Authentication is enabled
- Verify username/password are correct
- Check firewall allows connection on SQL Server port (default: 1433)
- Ensure SQL Server Agent service is running

### "Azure connection failed"

- Verify container URL is correct (no trailing slash)
- Check SAS token has proper permissions (Read, Write, Delete, List)
- Verify SAS token hasn't expired
- Ensure network can reach Azure (no proxy/firewall blocking)

### "AzCopy not found"

- Install AzCopy from Microsoft
- Update the AzCopy Path in configuration
- Verify the path points to `azcopy.exe`

### "Cannot create SQL Agent jobs"

- Ensure user has permissions to create jobs (sysadmin role recommended)
- Check SQL Server Agent service is running
- Verify msdb database is accessible

### Job fails during execution

- Check SQL Agent job history in SSMS
- Review job step output for specific error
- Common issues:
  - Disk space insufficient
  - Azure SAS token expired
  - AzCopy path incorrect
  - Network connectivity issues

## Uninstall

To completely remove all jobs and configuration:

1. Open the application
2. Load your configuration
3. Click **Remove All Jobs**
4. Choose **Yes** when asked about deleting local folders
5. Manually delete: `C:\ProgramData\BackupConfigurator` (if desired)

## Security Considerations

- **Passwords**: SQL password and SAS token are stored in plaintext in `config.json`
  - Protect this file with NTFS permissions (Administrators only)
  - Consider encrypting the configuration file for production use
- **SAS Token**: Use minimal required permissions and set appropriate expiration
- **SQL User**: Create a dedicated SQL user with minimum required permissions instead of using `sa`
- **Network**: Ensure SQL Server and Azure connections are encrypted (TLS)

## Retention and Lifecycle

- **Local Retention**: Configured in the application (default: 14 days)
- **Azure Retention**: Configure Azure Blob Storage lifecycle management policies for 1 year retention
  - See: https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview

## Support and Contribution

For issues, questions, or contributions, please visit the GitHub repository:
https://github.com/Juanprojas1982/BackupConfigurator

## License

This project is provided as-is for backup automation purposes.

## Technical Details

### Stack

- **.NET 8.0** (Long-Term Support)
- **WinForms** (Desktop UI)
- **Microsoft.Data.SqlClient** (SQL Server connectivity)
- **Azure.Storage.Blobs** (Azure Blob Storage client)
- **Serilog** (Logging framework)

### Architecture

- **BackupConfigurator.Core**: Business logic and services
  - Models: Configuration and validation results
  - Services: SQL tester, Azure tester, job provisioner, file system manager, configuration manager
- **BackupConfigurator.UI**: WinForms user interface
  - MainForm: Primary application window with tabs for configuration, actions, and logs

### Design Principles

- **Idempotent Operations**: Installing jobs multiple times updates existing jobs
- **Fail-Safe**: Validates prerequisites before making changes
- **Self-Contained Jobs**: Jobs operate independently after installation (app can be uninstalled)
- **Comprehensive Logging**: All operations logged to disk for troubleshooting
