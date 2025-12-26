using BackupConfigurator.Core.Models;
using Microsoft.Data.SqlClient;
using Serilog;

namespace BackupConfigurator.Core.Services;

public class SqlJobProvisioner
{
    private readonly ILogger _logger;
    private const string AppDataFolder = @"C:\ProgramData\BackupConfigurator";

    public SqlJobProvisioner(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> InstallJobsAsync(BackupConfiguration config)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Installing SQL Agent jobs for {NIT}/{Database}", config.SanitizedNIT, config.SanitizedDatabaseName);

            var connectionString = SqlTester.BuildConnectionString(config);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Change to msdb context
            using var useCmd = new SqlCommand("USE msdb", connection);
            await useCmd.ExecuteNonQueryAsync();

            // Job names
            var fullJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_FULL_WEEKLY";
            var diffJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_DIFF_EVERY_{config.DifferentialIntervalHours}H";
            var cleanupJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_CLEANUP_DAILY";

            // Remove existing jobs (idempotent)
            await RemoveJobIfExistsAsync(connection, fullJobName);
            await RemoveJobIfExistsAsync(connection, diffJobName);
            await RemoveJobIfExistsAsync(connection, cleanupJobName);

            // Create FULL backup job
            await CreateFullBackupJobAsync(connection, config, fullJobName);
            result.AddDetail($"✓ Created job: {fullJobName}");

            // Create DIFF backup job
            await CreateDiffBackupJobAsync(connection, config, diffJobName);
            result.AddDetail($"✓ Created job: {diffJobName}");

            // Create CLEANUP job
            await CreateCleanupJobAsync(connection, config, cleanupJobName);
            result.AddDetail($"✓ Created job: {cleanupJobName}");

            result.Success = true;
            result.Message = "SQL Agent jobs installed successfully";

            _logger.Information("SQL Agent jobs installed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to install SQL Agent jobs");
            return ValidationResult.Fail($"Failed to install jobs: {ex.Message}");
        }
    }

    public async Task<ValidationResult> RemoveJobsAsync(BackupConfiguration config)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Removing SQL Agent jobs for {NIT}/{Database}", config.SanitizedNIT, config.SanitizedDatabaseName);

            var connectionString = SqlTester.BuildConnectionString(config);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Change to msdb context
            using var useCmd = new SqlCommand("USE msdb", connection);
            await useCmd.ExecuteNonQueryAsync();

            // Job names
            var fullJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_FULL_WEEKLY";
            var diffJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_DIFF_EVERY_{config.DifferentialIntervalHours}H";
            var cleanupJobName = $"BK_{config.SanitizedNIT}_{config.SanitizedDatabaseName}_CLEANUP_DAILY";

            var removed = 0;
            if (await RemoveJobIfExistsAsync(connection, fullJobName))
            {
                result.AddDetail($"✓ Removed job: {fullJobName}");
                removed++;
            }

            if (await RemoveJobIfExistsAsync(connection, diffJobName))
            {
                result.AddDetail($"✓ Removed job: {diffJobName}");
                removed++;
            }

            if (await RemoveJobIfExistsAsync(connection, cleanupJobName))
            {
                result.AddDetail($"✓ Removed job: {cleanupJobName}");
                removed++;
            }

            if (removed == 0)
            {
                result.AddDetail("No jobs found to remove");
            }

            result.Success = true;
            result.Message = $"Removed {removed} SQL Agent job(s)";

            _logger.Information("SQL Agent jobs removed successfully. Count: {Count}", removed);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove SQL Agent jobs");
            return ValidationResult.Fail($"Failed to remove jobs: {ex.Message}");
        }
    }

    private async Task<bool> RemoveJobIfExistsAsync(SqlConnection connection, string jobName)
    {
        try
        {
            var checkSql = "SELECT job_id FROM msdb.dbo.sysjobs WHERE name = @JobName";
            using var checkCmd = new SqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("@JobName", jobName);
            var jobId = await checkCmd.ExecuteScalarAsync();

            if (jobId != null)
            {
                var deleteSql = "EXEC msdb.dbo.sp_delete_job @job_name = @JobName";
                using var deleteCmd = new SqlCommand(deleteSql, connection);
                deleteCmd.Parameters.AddWithValue("@JobName", jobName);
                await deleteCmd.ExecuteNonQueryAsync();

                _logger.Information("Removed existing job: {JobName}", jobName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error checking/removing job {JobName}", jobName);
            return false;
        }
    }

    private async Task CreateFullBackupJobAsync(SqlConnection connection, BackupConfiguration config, string jobName)
    {
        // Parse full backup time
        var timeParts = config.FullBackupTime.Split(':');
        var hour = int.Parse(timeParts[0]);
        var minute = int.Parse(timeParts[1]);

        var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName, "FULL");
        var uploadScriptPath = Path.Combine(AppDataFolder, "scripts", "upload_full.cmd");

        // Step 1: Backup to disk
        var backupSql = $@"
DECLARE @FileName NVARCHAR(500)
SET @FileName = '{basePath}\{config.SanitizedNIT}_{config.SanitizedDatabaseName}_FULL_' + 
    CONVERT(VARCHAR, GETDATE(), 112) + '_' + 
    REPLACE(CONVERT(VARCHAR, GETDATE(), 108), ':', '') + '.bak'

BACKUP DATABASE [{config.DatabaseName}]
TO DISK = @FileName
WITH COMPRESSION, CHECKSUM, INIT
";

        // Create job
        var createJobSql = @"
EXEC msdb.dbo.sp_add_job 
    @job_name = @JobName,
    @enabled = 1,
    @description = 'Full backup job created by BackupConfigurator'
";
        using var createCmd = new SqlCommand(createJobSql, connection);
        createCmd.Parameters.AddWithValue("@JobName", jobName);
        await createCmd.ExecuteNonQueryAsync();

        // Add Step 1: Backup
        var addStep1Sql = @"
EXEC msdb.dbo.sp_add_jobstep
    @job_name = @JobName,
    @step_name = 'Backup Database',
    @step_id = 1,
    @subsystem = 'TSQL',
    @command = @Command,
    @database_name = @DatabaseName,
    @on_success_action = 3,
    @on_fail_action = 2
";
        using var step1Cmd = new SqlCommand(addStep1Sql, connection);
        step1Cmd.Parameters.AddWithValue("@JobName", jobName);
        step1Cmd.Parameters.AddWithValue("@Command", backupSql);
        step1Cmd.Parameters.AddWithValue("@DatabaseName", config.DatabaseName);
        await step1Cmd.ExecuteNonQueryAsync();

        // Add Step 2: Upload to Azure
        var uploadCmd = $@"cmd.exe /c ""{uploadScriptPath}""";
        var addStep2Sql = @"
EXEC msdb.dbo.sp_add_jobstep
    @job_name = @JobName,
    @step_name = 'Upload to Azure',
    @step_id = 2,
    @subsystem = 'CmdExec',
    @command = @Command,
    @on_success_action = 1,
    @on_fail_action = 2
";
        using var step2Cmd = new SqlCommand(addStep2Sql, connection);
        step2Cmd.Parameters.AddWithValue("@JobName", jobName);
        step2Cmd.Parameters.AddWithValue("@Command", uploadCmd);
        await step2Cmd.ExecuteNonQueryAsync();

        // Create schedule (weekly on specified day and time)
        var scheduleName = $"{jobName}_Schedule";
        var dayOfWeek = (int)config.FullBackupDayOfWeek + 1; // SQL Server uses 1=Sunday

        var addScheduleSql = @"
EXEC msdb.dbo.sp_add_schedule
    @schedule_name = @ScheduleName,
    @freq_type = 8,
    @freq_interval = @DayOfWeek,
    @active_start_time = @ActiveStartTime
";
        using var scheduleCmd = new SqlCommand(addScheduleSql, connection);
        scheduleCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        scheduleCmd.Parameters.AddWithValue("@DayOfWeek", dayOfWeek);
        scheduleCmd.Parameters.AddWithValue("@ActiveStartTime", hour * 10000 + minute * 100);
        await scheduleCmd.ExecuteNonQueryAsync();

        // Attach schedule to job
        var attachSql = @"
EXEC msdb.dbo.sp_attach_schedule
    @job_name = @JobName,
    @schedule_name = @ScheduleName
";
        using var attachCmd = new SqlCommand(attachSql, connection);
        attachCmd.Parameters.AddWithValue("@JobName", jobName);
        attachCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        await attachCmd.ExecuteNonQueryAsync();

        // Add job to server
        var addServerSql = @"
EXEC msdb.dbo.sp_add_jobserver
    @job_name = @JobName
";
        using var serverCmd = new SqlCommand(addServerSql, connection);
        serverCmd.Parameters.AddWithValue("@JobName", jobName);
        await serverCmd.ExecuteNonQueryAsync();

        _logger.Information("Created FULL backup job: {JobName}", jobName);
    }

    private async Task CreateDiffBackupJobAsync(SqlConnection connection, BackupConfiguration config, string jobName)
    {
        var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName, "DIFF");
        var uploadScriptPath = Path.Combine(AppDataFolder, "scripts", "upload_diff.cmd");

        // Step 1: Differential backup to disk
        var backupSql = $@"
DECLARE @FileName NVARCHAR(500)
SET @FileName = '{basePath}\{config.SanitizedNIT}_{config.SanitizedDatabaseName}_DIFF_' + 
    CONVERT(VARCHAR, GETDATE(), 112) + '_' + 
    REPLACE(CONVERT(VARCHAR, GETDATE(), 108), ':', '') + '.dif'

BACKUP DATABASE [{config.DatabaseName}]
TO DISK = @FileName
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, INIT
";

        // Create job
        var createJobSql = @"
EXEC msdb.dbo.sp_add_job 
    @job_name = @JobName,
    @enabled = 1,
    @description = 'Differential backup job created by BackupConfigurator'
";
        using var createCmd = new SqlCommand(createJobSql, connection);
        createCmd.Parameters.AddWithValue("@JobName", jobName);
        await createCmd.ExecuteNonQueryAsync();

        // Add Step 1: Backup
        var addStep1Sql = @"
EXEC msdb.dbo.sp_add_jobstep
    @job_name = @JobName,
    @step_name = 'Backup Database',
    @step_id = 1,
    @subsystem = 'TSQL',
    @command = @Command,
    @database_name = @DatabaseName,
    @on_success_action = 3,
    @on_fail_action = 2
";
        using var step1Cmd = new SqlCommand(addStep1Sql, connection);
        step1Cmd.Parameters.AddWithValue("@JobName", jobName);
        step1Cmd.Parameters.AddWithValue("@Command", backupSql);
        step1Cmd.Parameters.AddWithValue("@DatabaseName", config.DatabaseName);
        await step1Cmd.ExecuteNonQueryAsync();

        // Add Step 2: Upload to Azure
        var uploadCmd = $@"cmd.exe /c ""{uploadScriptPath}""";
        var addStep2Sql = @"
EXEC msdb.dbo.sp_add_jobstep
    @job_name = @JobName,
    @step_name = 'Upload to Azure',
    @step_id = 2,
    @subsystem = 'CmdExec',
    @command = @Command,
    @on_success_action = 1,
    @on_fail_action = 2
";
        using var step2Cmd = new SqlCommand(addStep2Sql, connection);
        step2Cmd.Parameters.AddWithValue("@JobName", jobName);
        step2Cmd.Parameters.AddWithValue("@Command", uploadCmd);
        await step2Cmd.ExecuteNonQueryAsync();

        // Create schedule (daily, every N hours)
        var scheduleName = $"{jobName}_Schedule";
        var addScheduleSql = @"
EXEC msdb.dbo.sp_add_schedule
    @schedule_name = @ScheduleName,
    @freq_type = 4,
    @freq_interval = 1,
    @freq_subday_type = 8,
    @freq_subday_interval = @SubdayInterval
";
        using var scheduleCmd = new SqlCommand(addScheduleSql, connection);
        scheduleCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        scheduleCmd.Parameters.AddWithValue("@SubdayInterval", config.DifferentialIntervalHours);
        await scheduleCmd.ExecuteNonQueryAsync();

        // Attach schedule to job
        var attachSql = @"
EXEC msdb.dbo.sp_attach_schedule
    @job_name = @JobName,
    @schedule_name = @ScheduleName
";
        using var attachCmd = new SqlCommand(attachSql, connection);
        attachCmd.Parameters.AddWithValue("@JobName", jobName);
        attachCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        await attachCmd.ExecuteNonQueryAsync();

        // Add job to server
        var addServerSql = @"
EXEC msdb.dbo.sp_add_jobserver
    @job_name = @JobName
";
        using var serverCmd = new SqlCommand(addServerSql, connection);
        serverCmd.Parameters.AddWithValue("@JobName", jobName);
        await serverCmd.ExecuteNonQueryAsync();

        _logger.Information("Created DIFF backup job: {JobName}", jobName);
    }

    private async Task CreateCleanupJobAsync(SqlConnection connection, BackupConfiguration config, string jobName)
    {
        var cleanupScriptPath = Path.Combine(AppDataFolder, "scripts", "cleanup_local.cmd");

        // Create job
        var createJobSql = @"
EXEC msdb.dbo.sp_add_job 
    @job_name = @JobName,
    @enabled = 1,
    @description = 'Cleanup old backups created by BackupConfigurator'
";
        using var createCmd = new SqlCommand(createJobSql, connection);
        createCmd.Parameters.AddWithValue("@JobName", jobName);
        await createCmd.ExecuteNonQueryAsync();

        // Add Step: Cleanup
        var cleanupCmd = $@"cmd.exe /c ""{cleanupScriptPath}""";
        var addStepSql = @"
EXEC msdb.dbo.sp_add_jobstep
    @job_name = @JobName,
    @step_name = 'Cleanup Old Backups',
    @step_id = 1,
    @subsystem = 'CmdExec',
    @command = @Command,
    @on_success_action = 1,
    @on_fail_action = 2
";
        using var stepCmd = new SqlCommand(addStepSql, connection);
        stepCmd.Parameters.AddWithValue("@JobName", jobName);
        stepCmd.Parameters.AddWithValue("@Command", cleanupCmd);
        await stepCmd.ExecuteNonQueryAsync();

        // Create schedule (daily at 03:30)
        var scheduleName = $"{jobName}_Schedule";
        var addScheduleSql = @"
EXEC msdb.dbo.sp_add_schedule
    @schedule_name = @ScheduleName,
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 33000
";
        using var scheduleCmd = new SqlCommand(addScheduleSql, connection);
        scheduleCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        await scheduleCmd.ExecuteNonQueryAsync();

        // Attach schedule to job
        var attachSql = @"
EXEC msdb.dbo.sp_attach_schedule
    @job_name = @JobName,
    @schedule_name = @ScheduleName
";
        using var attachCmd = new SqlCommand(attachSql, connection);
        attachCmd.Parameters.AddWithValue("@JobName", jobName);
        attachCmd.Parameters.AddWithValue("@ScheduleName", scheduleName);
        await attachCmd.ExecuteNonQueryAsync();

        // Add job to server
        var addServerSql = @"
EXEC msdb.dbo.sp_add_jobserver
    @job_name = @JobName
";
        using var serverCmd = new SqlCommand(addServerSql, connection);
        serverCmd.Parameters.AddWithValue("@JobName", jobName);
        await serverCmd.ExecuteNonQueryAsync();

        _logger.Information("Created cleanup job: {JobName}", jobName);
    }
}
