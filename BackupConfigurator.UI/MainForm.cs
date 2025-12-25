using BackupConfigurator.Core.Models;
using BackupConfigurator.Core.Services;
using Serilog;

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
        }
    }

    private BackupConfiguration GetConfigurationFromForm()
    {
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
            AzureContainerUrl = txtAzureContainerUrl.Text.Trim(),
            AzureSasToken = txtAzureSasToken.Text.Trim(),
            AzCopyPath = txtAzCopyPath.Text.Trim()
        };
    }

    private void LoadConfigurationToForm(BackupConfiguration config)
    {
        txtNIT.Text = config.InstitutionNIT;
        txtSqlServer.Text = config.SqlServer;
        txtSqlUser.Text = config.SqlUser;
        txtSqlPassword.Text = config.SqlPassword;
        txtDatabaseName.Text = config.DatabaseName;
        numDiffInterval.Value = config.DifferentialIntervalHours;
        cmbFullBackupDay.SelectedIndex = (int)config.FullBackupDayOfWeek;
        txtFullBackupTime.Text = config.FullBackupTime;
        txtLocalBasePath.Text = config.LocalBasePath;
        numLocalRetention.Value = config.LocalRetentionDays;
        txtAzureContainerUrl.Text = config.AzureContainerUrl;
        txtAzureSasToken.Text = config.AzureSasToken;
        txtAzCopyPath.Text = config.AzCopyPath;
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

            // Validate AzCopy exists
            AddResult("Validating AzCopy installation...");
            var azCopyResult = _fileSystemManager.ValidateAzCopyExists(config.AzCopyPath);
            AddResult(azCopyResult.ToString());

            if (!azCopyResult.Success)
            {
                var continueAnyway = MessageBox.Show(
                    "AzCopy not found at specified path. Jobs will be created but may fail during upload.\n\nContinue anyway?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (continueAnyway != DialogResult.Yes)
                {
                    AddResult("Installation cancelled by user.");
                    return;
                }
            }

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

            // Save configuration
            AddResult("Saving configuration...");
            var saveResult = _configManager.SaveConfiguration(config);
            AddResult(saveResult.ToString());

            MessageBox.Show("Installation/configuration completed successfully!\n\nSQL Agent jobs have been created and scheduled.", 
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

    private async void BtnRemoveAll_Click(object? sender, EventArgs e)
    {
        try
        {
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

            var config = GetConfigurationFromForm();

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

    private void BtnLoadConfig_Click(object? sender, EventArgs e)
    {
        var (config, result) = _configManager.LoadConfiguration();
        if (config != null)
        {
            LoadConfigurationToForm(config);
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
        var result = _configManager.SaveConfiguration(config);

        if (result.Success)
        {
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

    private void BtnBrowseAzCopy_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        dialog.Title = "Select AzCopy executable";
        
        if (!string.IsNullOrWhiteSpace(txtAzCopyPath.Text))
        {
            var dirPath = Path.GetDirectoryName(txtAzCopyPath.Text);
            if (!string.IsNullOrWhiteSpace(dirPath))
            {
                dialog.InitialDirectory = dirPath;
            }
            dialog.FileName = Path.GetFileName(txtAzCopyPath.Text);
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtAzCopyPath.Text = dialog.FileName;
        }
    }

    private void BtnRefreshLogs_Click(object? sender, EventArgs e)
    {
        try
        {
            var logPath = Path.Combine(@"C:\ProgramData\BackupConfigurator\logs", $"log-{DateTime.Now:yyyyMMdd}.txt");
            
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                txtLogs.Text = logContent;
                txtLogs.SelectionStart = txtLogs.Text.Length;
                txtLogs.ScrollToCaret();
            }
            else
            {
                txtLogs.Text = "Log file not found for today.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading logs: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
