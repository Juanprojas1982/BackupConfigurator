using BackupConfigurator.Core.Models;
using BackupConfigurator.Core.Services;
using Serilog;
using System.Diagnostics;

namespace BackupConfigurator.UI;

public partial class MainForm : Form
{
    private readonly ILogger _logger;
    private readonly SqlTester _sqlTester;
    private readonly AzureTester _azureTester;
    private readonly SqlJobProvisioner _jobProvisioner;
    private readonly FileSystemManager _fileSystemManager;
    private readonly ConfigurationManager _configManager;

    public MainForm()
    {
        _logger = Log.Logger;
        _sqlTester = new SqlTester(_logger);
        _azureTester = new AzureTester(_logger);
        _jobProvisioner = new SqlJobProvisioner(_logger);
        _fileSystemManager = new FileSystemManager(_logger);
        _configManager = new ConfigurationManager(_logger);

        InitializeComponent();

        // Try to load existing configuration
        var (config, result) = _configManager.LoadConfiguration();
        if (config != null)
        {
            LoadConfigurationToForm(config);
            AddResult($"Configuration loaded from disk:\n{result}");
            ReconfigureLogger(config);
        }
    }

    private void ReconfigureLogger(BackupConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.LocalBasePath) ||
            string.IsNullOrWhiteSpace(config.SanitizedDatabaseName))
        {
            return;
        }

        try
        {
            var logPath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "logs", $"log-{DateTime.Now:yyyyMMdd}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            Log.CloseAndFlush();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _logger.Information("Logger reconfigured to use database-specific log path: {LogPath}", logPath);
        }
        catch (Exception ex)
        {
            // If reconfiguration fails, keep using the temp logger
            _logger.Warning(ex, "Failed to reconfigure logger, using temporary log location");
        }
    }

    private BackupConfiguration GetConfigurationFromForm()
    {
        // Support pasting full URL (container + ? + sas) into the AzureContainerUrl field.
        var rawContainerInput = txtAzureContainerUrl.Text ?? string.Empty;

        // Remove any internal newlines or whitespace wrapping caused by the multiline textbox
        rawContainerInput = rawContainerInput.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();

        string containerUrl = rawContainerInput;
        string sasToken = string.Empty;

        if (!string.IsNullOrEmpty(rawContainerInput) && rawContainerInput.Contains('?'))
        {
            var idx = rawContainerInput.IndexOf('?');
            if (idx >= 0)
            {
                containerUrl = rawContainerInput.Substring(0, idx).TrimEnd('/');
                sasToken = rawContainerInput.Substring(idx + 1).Trim(); // exclude '?'
            }
        }

        // Try to load existing config to get saved values not in the form
        var (savedConfig, _) = _configManager.LoadConfiguration();
        var installationKeyHash = savedConfig?.InstallationKeyHash ?? string.Empty;
        var azCopyPath = savedConfig?.AzCopyPath ?? string.Empty;

        return new BackupConfiguration
        {
            InstitutionNIT = txtNIT.Text.Trim(),
            SqlServer = txtSqlServer.Text.Trim(),
            SqlUser = txtSqlUser.Text.Trim(),
            SqlPassword = txtSqlPassword.Text,
            DatabaseName = txtDatabaseName.Text.Trim(),
            DifferentialIntervalHours = (int)numDiffInterval.Value,
            FullBackupDayOfWeek = (DayOfWeek)cmbFullBackupDay.SelectedIndex,
            FullBackupTime = txtFullBackupTime.Text.Trim(),
            LocalBasePath = txtLocalBasePath.Text.Trim(),
            LocalRetentionDays = (int)numLocalRetention.Value,
            AzureContainerUrl = containerUrl,
            AzureSasToken = sasToken,
            AzCopyPath = azCopyPath, // Load from saved config
            InstallationKeyHash = installationKeyHash // Load from saved config
        };
    }

    private void LoadConfigurationToForm(BackupConfiguration config)
    {
        txtNIT.Text = config.InstitutionNIT;
        txtSqlServer.Text = config.SqlServer;
        txtSqlUser.Text = config.SqlUser;
        txtSqlPassword.Text = string.Empty; // Never load password from file
        txtDatabaseName.Text = config.DatabaseName;
        numDiffInterval.Value = config.DifferentialIntervalHours;
        cmbFullBackupDay.SelectedIndex = (int)config.FullBackupDayOfWeek;
        txtFullBackupTime.Text = config.FullBackupTime;
        txtLocalBasePath.Text = config.LocalBasePath;
        numLocalRetention.Value = config.LocalRetentionDays;

        // Show combined URL+SAS in the single AzureContainerUrl textbox
        if (!string.IsNullOrWhiteSpace(config.AzureSasToken))
        {
            txtAzureContainerUrl.Text = config.AzureContainerUrl.TrimEnd('/') + config.NormalizedSasToken;
        }
        else
        {
            txtAzureContainerUrl.Text = config.AzureContainerUrl;
        }
    }

    private void AddResult(string message)
    {
        txtResults.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\r\n");
        txtResults.AppendText(new string('-', 80) + "\r\n");
        txtResults.ScrollToCaret();
    }

    private async void BtnTestSql_Click(object? sender, EventArgs e)
    {
        try
        {
            btnTestSql.Enabled = false;
            txtResults.Clear();
            AddResult("Testing SQL Server connection...");

            var config = GetConfigurationFromForm();

            // Request SQL password
            if (string.IsNullOrWhiteSpace(config.SqlPassword))
            {
                using var passwordDialog = new SqlPasswordDialog(config.SqlServer, config.SqlUser);
                if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                {
                    AddResult("Operation cancelled by user.");
                    return;
                }
                config.SqlPassword = passwordDialog.EnteredPassword;
            }

            var result = await _sqlTester.TestConnectionAsync(config);

            AddResult(result.ToString());

            if (result.Success)
            {
                MessageBox.Show("SQL Server connection test successful!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"SQL Server connection test failed:\n{result.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during SQL test");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestSql.Enabled = true;
        }
    }

    private async void BtnTestAzure_Click(object? sender, EventArgs e)
    {
        try
        {
            btnTestAzure.Enabled = false;
            txtResults.Clear();
            AddResult("Testing Azure Blob Storage connection...");

            var config = GetConfigurationFromForm();
            var result = await _azureTester.TestConnectionAsync(config);

            AddResult(result.ToString());

            if (result.Success)
            {
                MessageBox.Show("Azure Blob Storage connection test successful!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Azure Blob Storage connection test failed:\n{result.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during Azure test");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTestAzure.Enabled = true;
        }
    }

    private async void BtnInstall_Click(object? sender, EventArgs e)
    {
        try
        {
            btnInstall.Enabled = false;
            txtResults.Clear();
            AddResult("Starting installation/configuration...");

            var config = GetConfigurationFromForm();

#if !DEBUG
            // ONLY require installation key in RELEASE mode
            if (string.IsNullOrWhiteSpace(config.InstallationKeyHash))
            {
                AddResult("? Installation key is not set.");
                MessageBox.Show("Installation key is required before creating jobs.\n\nPlease click 'Set Installation Key' button in the Configuration tab first.",
                    "Installation Key Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate installation key
            using var keyDialog = new InstallationKeyDialog("Enter Installation Key to proceed:");
            if (keyDialog.ShowDialog(this) != DialogResult.OK)
            {
                AddResult("Installation cancelled by user.");
                return;
            }

            if (!config.ValidateInstallationKey(keyDialog.EnteredKey))
            {
                AddResult("? Invalid installation key. Operation cancelled.");
                MessageBox.Show("Invalid installation key. Access denied.", "Authentication Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AddResult("? Installation key validated successfully");
#else
            // DEBUG mode: Skip installation key check
            AddResult("? DEBUG MODE: Installation key check skipped");
#endif

            // Request SQL password
            if (string.IsNullOrWhiteSpace(config.SqlPassword))
            {
                using var passwordDialog = new SqlPasswordDialog(config.SqlServer, config.SqlUser);
                if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                {
                    AddResult("Installation cancelled by user.");
                    return;
                }
                config.SqlPassword = passwordDialog.EnteredPassword;
                AddResult("? SQL password provided");
            }

            // Ensure AzCopy is available (download if necessary) - runs in background
            AddResult("Checking AzCopy availability...");
            var azCopyPath = await EnsureAzCopyIsAvailableAsync();
            
            if (string.IsNullOrEmpty(azCopyPath))
            {
                var continueAnyway = MessageBox.Show(
                    "AzCopy could not be found or downloaded. Jobs will be created but may fail during upload.\n\nContinue anyway?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (continueAnyway != DialogResult.Yes)
                {
                    AddResult("Installation cancelled by user.");
                    return;
                }
                // Use default path as fallback
                azCopyPath = @"C:\Program Files\Microsoft\AzCopy\azcopy.exe";
            }

            config.AzCopyPath = azCopyPath;
            AddResult($"Using AzCopy: {azCopyPath}");

            // Create backup folders
            AddResult("Creating backup folders...");
            var foldersResult = _fileSystemManager.CreateBackupFolders(config);
            AddResult(foldersResult.ToString());

            if (!foldersResult.Success)
            {
                MessageBox.Show($"Failed to create backup folders:\n{foldersResult.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create scripts
            AddResult("Creating upload scripts...");
            var scriptsResult = _fileSystemManager.CreateUploadScripts(config);
            AddResult(scriptsResult.ToString());

            AddResult("Creating cleanup script...");
            var cleanupScriptResult = _fileSystemManager.CreateCleanupScript(config);
            AddResult(cleanupScriptResult.ToString());

            // Install SQL Agent jobs
            AddResult("Installing SQL Agent jobs...");
            var jobsResult = await _jobProvisioner.InstallJobsAsync(config);
            AddResult(jobsResult.ToString());

            if (!jobsResult.Success)
            {
                MessageBox.Show($"Failed to install SQL Agent jobs:\n{jobsResult.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Save configuration (without password)
            AddResult("Saving configuration...");
            var saveResult = _configManager.SaveConfiguration(config);
            AddResult(saveResult.ToString());

            // Reconfigure logger to use database-specific path
            ReconfigureLogger(config);

            MessageBox.Show("Installation/configuration completed successfully!\n\nSQL Agent jobs have been created and scheduled.\n\nNote: SQL password is NOT saved for security reasons.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during installation");
            MessageBox.Show($"Error: {ex.Message}\n\nSee logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnInstall.Enabled = true;
        }
    }

    private async Task<string?> EnsureAzCopyIsAvailableAsync()
    {
        try
        {
            // Try common locations first
            var commonPaths = new[]
            {
                @"C:\Program Files\AzCopy\azcopy.exe",
                @"C:\Program Files (x86)\AzCopy\azcopy.exe",
                @"C:\Program Files\Microsoft\AzCopy\azcopy.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "azcopy.exe")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    AddResult($"? Found AzCopy at: {path}");
                    return path;
                }
            }

            // Not found, try to download
            AddResult("AzCopy not found, downloading automatically...");
            var result = await _fileSystemManager.EnsureAzCopyAvailableAsync(@"C:\Program Files\Microsoft\AzCopy\azcopy.exe");
            
            if (result.Success)
            {
                // Check Downloads folder
                var downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "azcopy.exe");
                if (File.Exists(downloadPath))
                {
                    AddResult($"? Downloaded AzCopy to: {downloadPath}");
                    return downloadPath;
                }
            }

            AddResult("? Could not find or download AzCopy");
            return null;
        }
        catch (Exception ex)
        {
            AddResult($"? Error checking AzCopy: {ex.Message}");
            return null;
        }
    }

    private async void BtnRemoveAll_Click(object? sender, EventArgs e)
    {
        try
        {
            var config = GetConfigurationFromForm();

#if !DEBUG
            // ONLY require installation key in RELEASE mode
            if (string.IsNullOrWhiteSpace(config.InstallationKeyHash))
            {
                MessageBox.Show("Installation key is required to remove jobs.\n\nPlease click 'Set Installation Key' button in the Configuration tab first.",
                    "Installation Key Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate installation key
            using var keyDialog = new InstallationKeyDialog("Enter Installation Key to remove jobs:");
            if (keyDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!config.ValidateInstallationKey(keyDialog.EnteredKey))
            {
                MessageBox.Show("Invalid installation key. Access denied.", "Authentication Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
#endif

            // Request SQL password
            if (string.IsNullOrWhiteSpace(config.SqlPassword))
            {
                using var passwordDialog = new SqlPasswordDialog(config.SqlServer, config.SqlUser);
                if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                config.SqlPassword = passwordDialog.EnteredPassword;
            }

            var confirmResult = MessageBox.Show(
                "This will remove all SQL Agent jobs created by this application.\n\nDo you also want to delete the local backup folders?",
                "Confirm Removal",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Cancel)
            {
                return;
            }

            bool deleteFolders = confirmResult == DialogResult.Yes;

            btnRemoveAll.Enabled = false;
            txtResults.Clear();
            AddResult("Starting removal process...");

            // Remove SQL Agent jobs
            AddResult("Removing SQL Agent jobs...");
            var jobsResult = await _jobProvisioner.RemoveJobsAsync(config);
            AddResult(jobsResult.ToString());

            // Remove local folders if requested
            if (deleteFolders)
            {
                AddResult("Removing local backup folders...");
                var foldersResult = _fileSystemManager.RemoveBackupFolders(config, confirmed: true);
                AddResult(foldersResult.ToString());
            }

            MessageBox.Show("Removal completed successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during removal");
            MessageBox.Show($"Error: {ex.Message}\n\nSee logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRemoveAll.Enabled = true;
        }
    }

    private async void BtnDownloadFromAzure_Click(object? sender, EventArgs e)
    {
        try
        {
            btnDownloadFromAzure.Enabled = false;
            txtResults.Clear();
            AddResult("Starting download from Azure...");

            var config = GetConfigurationFromForm();

#if !DEBUG
            // ONLY require installation key in RELEASE mode
            if (string.IsNullOrWhiteSpace(config.InstallationKeyHash))
            {
                AddResult("? Installation key is not set.");
                MessageBox.Show("Installation key is required to download backups from Azure.\n\nPlease click 'Set Installation Key' button in the Configuration tab first.",
                    "Installation Key Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate installation key
            using var keyDialog = new InstallationKeyDialog("Enter Installation Key to download from Azure:");
            if (keyDialog.ShowDialog(this) != DialogResult.OK)
            {
                AddResult("Download cancelled by user.");
                return;
            }

            if (!config.ValidateInstallationKey(keyDialog.EnteredKey))
            {
                AddResult("? Invalid installation key. Operation cancelled.");
                MessageBox.Show("Invalid installation key. Access denied.", "Authentication Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AddResult("? Installation key validated successfully");
#else
            // DEBUG mode: Skip installation key check
            AddResult("? DEBUG MODE: Installation key check skipped");
#endif

            // Validate required fields
            if (string.IsNullOrWhiteSpace(config.LocalBasePath))
            {
                AddResult("? Local Base Path is required");
                MessageBox.Show("Please configure Local Base Path first.", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.DatabaseName))
            {
                AddResult("? Database Name is required");
                MessageBox.Show("Please configure Database Name first.", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.AzureContainerUrl))
            {
                AddResult("? Azure Container URL is required");
                MessageBox.Show("Please configure Azure Container URL first.", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Try to get AzCopy path from config or find it automatically
            if (string.IsNullOrWhiteSpace(config.AzCopyPath))
            {
                AddResult("AzCopy path not configured, searching automatically...");
                var azCopyPath = await EnsureAzCopyIsAvailableAsync();
                if (string.IsNullOrWhiteSpace(azCopyPath))
                {
                    AddResult("? AzCopy not found");
                    MessageBox.Show("AzCopy is required for downloading from Azure.\n\nPlease install AzCopy or configure the path in Configuration tab.",
                        "AzCopy Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                config.AzCopyPath = azCopyPath;
                AddResult($"? Found AzCopy at: {azCopyPath}");
            }

            // Confirm download
            var downloadPath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "AzureDownload");
            var confirmResult = MessageBox.Show(
                $"This will download ALL backup files from Azure to:\n\n{downloadPath}\n\nThis may take several minutes depending on the number and size of backups.\n\nContinue?",
                "Confirm Download",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
            {
                AddResult("Download cancelled by user.");
                return;
            }

            AddResult($"Download destination: {downloadPath}");
            AddResult("Please wait, this may take several minutes...");

            var result = await _fileSystemManager.DownloadAllFromAzureAsync(config);
            AddResult(result.ToString());

            if (result.Success)
            {
                MessageBox.Show($"{result.Message}\n\nFiles are ready for delivery to the client.\n\nLocation: {downloadPath}",
                    "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Download failed:\n{result.Message}\n\nSee results for details.",
                    "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during Azure download");
            MessageBox.Show($"Error: {ex.Message}\n\nSee logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnDownloadFromAzure.Enabled = true;
        }
    }

    private void BtnSetKey_Click(object? sender, EventArgs e)
    {
        try
        {
            var currentConfig = GetConfigurationFromForm();

            // If a key already exists, ask for current key first
            if (!string.IsNullOrWhiteSpace(currentConfig.InstallationKeyHash))
            {
                using var verifyDialog = new InstallationKeyDialog("Enter current Installation Key to change it:");
                if (verifyDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (!currentConfig.ValidateInstallationKey(verifyDialog.EnteredKey))
                {
                    MessageBox.Show("Invalid current installation key. Access denied.", "Authentication Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Prompt for new key
            using var newKeyDialog = new InstallationKeyDialog("Enter NEW Installation Key:");
            if (newKeyDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var newKey = newKeyDialog.EnteredKey;

            if (string.IsNullOrWhiteSpace(newKey))
            {
                MessageBox.Show("Installation key cannot be empty.", "Invalid Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newKey.Length < 8)
            {
                MessageBox.Show("Installation key must be at least 8 characters long.", "Weak Key",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm new key
            using var confirmDialog = new InstallationKeyDialog("Confirm NEW Installation Key:");
            if (confirmDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (newKey != confirmDialog.EnteredKey)
            {
                MessageBox.Show("Keys do not match. Please try again.", "Mismatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Set the new key
            currentConfig.SetInstallationKey(newKey);

            // Save configuration
            var saveResult = _configManager.SaveConfiguration(currentConfig);

            if (saveResult.Success)
            {
                MessageBox.Show("Installation key set successfully!\n\nThis key will be required for all job installation and removal operations.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logger.Information("Installation key set successfully");
            }
            else
            {
                MessageBox.Show($"Failed to save configuration:\n{saveResult.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting installation key");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnLoadConfig_Click(object? sender, EventArgs e)
    {
        var (config, result) = _configManager.LoadConfiguration();
        if (config != null)
        {
            LoadConfigurationToForm(config);
            ReconfigureLogger(config);
            MessageBox.Show("Configuration loaded successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show($"Failed to load configuration:\n{result.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSaveConfig_Click(object? sender, EventArgs e)
    {
        var config = GetConfigurationFromForm();
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(config.LocalBasePath))
        {
            MessageBox.Show("Local Base Path is required to save configuration.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(config.InstitutionNIT))
        {
            MessageBox.Show("Institution NIT is required to save configuration.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(config.DatabaseName))
        {
            MessageBox.Show("Database Name is required to save configuration.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        var result = _configManager.SaveConfiguration(config);

        if (result.Success)
        {
            ReconfigureLogger(config);
            MessageBox.Show($"Configuration saved successfully!\n\n{result.Message}", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show($"Failed to save configuration:\n{result.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select base path for local backups";

        if (!string.IsNullOrWhiteSpace(txtLocalBasePath.Text))
        {
            dialog.SelectedPath = txtLocalBasePath.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtLocalBasePath.Text = dialog.SelectedPath;
        }
    }

    private void BtnRefreshLogs_Click(object? sender, EventArgs e)
    {
        try
        {
            var config = GetConfigurationFromForm();
            string logPath;

            // Try to use the configured log path first
            if (!string.IsNullOrWhiteSpace(config.LocalBasePath) && 
                !string.IsNullOrWhiteSpace(config.SanitizedDatabaseName))
            {
                logPath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "logs", $"log-{DateTime.Now:yyyyMMdd}.txt");
            }
            else
            {
                // Fall back to temp folder if configuration not set
                logPath = Path.Combine(Path.GetTempPath(), "BackupConfigurator", $"log-{DateTime.Now:yyyyMMdd}.txt");
            }

            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                txtLogs.Text = logContent;
                txtLogs.SelectionStart = txtLogs.Text.Length;
                txtLogs.ScrollToCaret();
            }
            else
            {
                txtLogs.Text = $"Log file not found at: {logPath}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading logs: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
