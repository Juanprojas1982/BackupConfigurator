namespace BackupConfigurator.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            
            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 750);
            this.Text = "SQL Server Backup Configurator";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Main TabControl
            tabControl = new TabControl();
            tabControl.Location = new System.Drawing.Point(12, 12);
            tabControl.Size = new System.Drawing.Size(876, 690);
            tabControl.TabIndex = 0;

            // Tab 1: Configuration
            tabPageConfig = new TabPage("Configuration");
            tabControl.TabPages.Add(tabPageConfig);

            // Tab 2: Actions
            tabPageActions = new TabPage("Actions");
            tabControl.TabPages.Add(tabPageActions);

            // Tab 3: Logs
            tabPageLogs = new TabPage("Logs");
            tabControl.TabPages.Add(tabPageLogs);

            this.Controls.Add(tabControl);

            InitializeConfigTab();
            InitializeActionsTab();
            InitializeLogsTab();

            this.ResumeLayout(false);
        }

        private void InitializeConfigTab()
        {
            int yPos = 20;
            int labelWidth = 200;
            int inputWidth = 600;
            int xLabel = 20;
            int xInput = xLabel + labelWidth + 10;
            int lineHeight = 30;

            // Institution NIT
            var lblNIT = new Label { Text = "Institution NIT:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtNIT = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth };
            tabPageConfig.Controls.Add(lblNIT);
            tabPageConfig.Controls.Add(txtNIT);
            yPos += lineHeight;

            // SQL Server
            var lblSqlServer = new Label { Text = "SQL Server (host\\instance):", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtSqlServer = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth };
            tabPageConfig.Controls.Add(lblSqlServer);
            tabPageConfig.Controls.Add(txtSqlServer);
            yPos += lineHeight;

            // SQL User
            var lblSqlUser = new Label { Text = "SQL User:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtSqlUser = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth };
            tabPageConfig.Controls.Add(lblSqlUser);
            tabPageConfig.Controls.Add(txtSqlUser);
            yPos += lineHeight;

            // SQL Password
            var lblSqlPassword = new Label { Text = "SQL Password:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtSqlPassword = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth, UseSystemPasswordChar = true };
            tabPageConfig.Controls.Add(lblSqlPassword);
            tabPageConfig.Controls.Add(txtSqlPassword);
            yPos += lineHeight;

            // Database Name
            var lblDatabaseName = new Label { Text = "Database Name:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtDatabaseName = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth };
            tabPageConfig.Controls.Add(lblDatabaseName);
            tabPageConfig.Controls.Add(txtDatabaseName);
            yPos += lineHeight;

            // Differential Interval Hours
            var lblDiffInterval = new Label { Text = "Differential Interval (hours):", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            numDiffInterval = new NumericUpDown { Location = new System.Drawing.Point(xInput, yPos), Width = 100, Minimum = 1, Maximum = 24, Value = 6 };
            tabPageConfig.Controls.Add(lblDiffInterval);
            tabPageConfig.Controls.Add(numDiffInterval);
            yPos += lineHeight;

            // Full Backup Day
            var lblFullDay = new Label { Text = "Full Backup Day:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            cmbFullBackupDay = new ComboBox { Location = new System.Drawing.Point(xInput, yPos), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFullBackupDay.Items.AddRange(new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
            cmbFullBackupDay.SelectedIndex = 0;
            tabPageConfig.Controls.Add(lblFullDay);
            tabPageConfig.Controls.Add(cmbFullBackupDay);
            yPos += lineHeight;

            // Full Backup Time
            var lblFullTime = new Label { Text = "Full Backup Time (HH:mm):", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtFullBackupTime = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = 100, Text = "02:00" };
            tabPageConfig.Controls.Add(lblFullTime);
            tabPageConfig.Controls.Add(txtFullBackupTime);
            yPos += lineHeight;

            // Local Base Path
            var lblLocalPath = new Label { Text = "Local Base Path:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtLocalBasePath = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = 500 };
            var btnBrowse = new Button { Text = "Browse...", Location = new System.Drawing.Point(xInput + 510, yPos - 2), Width = 90 };
            btnBrowse.Click += BtnBrowse_Click;
            tabPageConfig.Controls.Add(lblLocalPath);
            tabPageConfig.Controls.Add(txtLocalBasePath);
            tabPageConfig.Controls.Add(btnBrowse);
            yPos += lineHeight;

            // Local Retention Days
            var lblRetention = new Label { Text = "Local Retention (days):", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            numLocalRetention = new NumericUpDown { Location = new System.Drawing.Point(xInput, yPos), Width = 100, Minimum = 1, Maximum = 365, Value = 14 };
            tabPageConfig.Controls.Add(lblRetention);
            tabPageConfig.Controls.Add(numLocalRetention);
            yPos += lineHeight;

            // Azure Container URL
            var lblAzureUrl = new Label { Text = "Azure Container URL:", Location = new System.Drawing.Point(xLabel, yPos), Width = labelWidth };
            txtAzureContainerUrl = new TextBox { Location = new System.Drawing.Point(xInput, yPos), Width = inputWidth, Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
            tabPageConfig.Controls.Add(lblAzureUrl);
            tabPageConfig.Controls.Add(txtAzureContainerUrl);
            yPos += 70;

            // Buttons
            btnLoadConfig = new Button { Text = "Load Config", Location = new System.Drawing.Point(xLabel, yPos), Width = 120 };
            btnLoadConfig.Click += BtnLoadConfig_Click;
            tabPageConfig.Controls.Add(btnLoadConfig);

            btnSaveConfig = new Button { Text = "Save Config", Location = new System.Drawing.Point(xLabel + 130, yPos), Width = 120 };
            btnSaveConfig.Click += BtnSaveConfig_Click;
            tabPageConfig.Controls.Add(btnSaveConfig);

            var btnSetKey = new Button { Text = "Set Installation Key", Location = new System.Drawing.Point(xLabel + 260, yPos), Width = 150 };
            btnSetKey.Click += BtnSetKey_Click;
            tabPageConfig.Controls.Add(btnSetKey);
        }

        private void InitializeActionsTab()
        {
            int yPos = 20;

            // Test SQL Button
            btnTestSql = new Button { Text = "Test SQL Connection", Location = new System.Drawing.Point(20, yPos), Width = 200, Height = 40 };
            btnTestSql.Click += BtnTestSql_Click;
            tabPageActions.Controls.Add(btnTestSql);
            yPos += 60;

            // Test Azure Button
            btnTestAzure = new Button { Text = "Test Azure Connection", Location = new System.Drawing.Point(20, yPos), Width = 200, Height = 40 };
            btnTestAzure.Click += BtnTestAzure_Click;
            tabPageActions.Controls.Add(btnTestAzure);
            yPos += 60;

            // Install/Configure Button
            btnInstall = new Button { Text = "Install/Configure Jobs", Location = new System.Drawing.Point(20, yPos), Width = 200, Height = 40 };
            btnInstall.Click += BtnInstall_Click;
            tabPageActions.Controls.Add(btnInstall);
            yPos += 60;

            // Download All from Azure Button
            btnDownloadFromAzure = new Button { Text = "Download All from Azure", Location = new System.Drawing.Point(20, yPos), Width = 200, Height = 40 };
            btnDownloadFromAzure.Click += BtnDownloadFromAzure_Click;
            btnDownloadFromAzure.BackColor = System.Drawing.Color.LightBlue;
            tabPageActions.Controls.Add(btnDownloadFromAzure);
            yPos += 60;

            // Remove All Button
            btnRemoveAll = new Button { Text = "Remove All Jobs", Location = new System.Drawing.Point(20, yPos), Width = 200, Height = 40 };
            btnRemoveAll.Click += BtnRemoveAll_Click;
            btnRemoveAll.BackColor = System.Drawing.Color.IndianRed;
            tabPageActions.Controls.Add(btnRemoveAll);
            yPos += 60;

            // Results TextBox
            var lblResults = new Label { Text = "Results:", Location = new System.Drawing.Point(20, yPos), Width = 100 };
            tabPageActions.Controls.Add(lblResults);
            yPos += 25;

            txtResults = new TextBox
            {
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 350),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            tabPageActions.Controls.Add(txtResults);
        }

        private void InitializeLogsTab()
        {
            txtLogs = new TextBox
            {
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(820, 600),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            tabPageLogs.Controls.Add(txtLogs);

            var btnRefreshLogs = new Button { Text = "Refresh Logs", Location = new System.Drawing.Point(20, 625), Width = 120 };
            btnRefreshLogs.Click += BtnRefreshLogs_Click;
            tabPageLogs.Controls.Add(btnRefreshLogs);
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabPageConfig;
        private TabPage tabPageActions;
        private TabPage tabPageLogs;

        // Config controls
        private TextBox txtNIT;
        private TextBox txtSqlServer;
        private TextBox txtSqlUser;
        private TextBox txtSqlPassword;
        private TextBox txtDatabaseName;
        private NumericUpDown numDiffInterval;
        private ComboBox cmbFullBackupDay;
        private TextBox txtFullBackupTime;
        private TextBox txtLocalBasePath;
        private NumericUpDown numLocalRetention;
        private TextBox txtAzureContainerUrl;
        private Button btnLoadConfig;
        private Button btnSaveConfig;

        // Action controls
        private Button btnTestSql;
        private Button btnTestAzure;
        private Button btnInstall;
        private Button btnDownloadFromAzure;
        private Button btnRemoveAll;
        private TextBox txtResults;

        // Log controls
        private TextBox txtLogs;
    }
}
