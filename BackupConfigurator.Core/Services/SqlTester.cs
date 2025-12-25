using BackupConfigurator.Core.Models;
using Microsoft.Data.SqlClient;
using Serilog;

namespace BackupConfigurator.Core.Services;

public class SqlTester
{
    private readonly ILogger _logger;

    public SqlTester(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> TestConnectionAsync(BackupConfiguration config)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Testing SQL connection to {Server}", config.SqlServer);

            var connectionString = BuildConnectionString(config);

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            result.AddDetail($"✓ Connected to SQL Server: {config.SqlServer}");

            // Check if database exists and is ONLINE
            var dbCheckQuery = @"
                SELECT state_desc 
                FROM sys.databases 
                WHERE name = @DatabaseName";

            using var dbCmd = new SqlCommand(dbCheckQuery, connection);
            dbCmd.Parameters.AddWithValue("@DatabaseName", config.DatabaseName);
            dbCmd.CommandTimeout = 10;

            var dbState = dbCmd.ExecuteScalar() as string;
            if (dbState == null)
            {
                return ValidationResult.Fail($"Database '{config.DatabaseName}' does not exist");
            }

            if (dbState != "ONLINE")
            {
                return ValidationResult.Fail($"Database '{config.DatabaseName}' is not ONLINE (state: {dbState})");
            }

            result.AddDetail($"✓ Database '{config.DatabaseName}' exists and is ONLINE");

            // Check if msdb exists (SQL Agent dependency)
            var msdbCheckQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = 'msdb'";
            using var msdbCmd = new SqlCommand(msdbCheckQuery, connection);
            msdbCmd.CommandTimeout = 10;
            var msdbExists = (int)msdbCmd.ExecuteScalar()! > 0;

            if (!msdbExists)
            {
                return ValidationResult.Fail("msdb database not found. SQL Server Agent may not be available.");
            }

            result.AddDetail("✓ msdb database accessible (SQL Agent available)");

            // Check permissions
            var permCheckQuery = @"
                SELECT 
                    CASE 
                        WHEN IS_SRVROLEMEMBER('sysadmin') = 1 THEN 'sysadmin'
                        ELSE 'user'
                    END AS role";

            using var permCmd = new SqlCommand(permCheckQuery, connection);
            permCmd.CommandTimeout = 10;
            var userRole = permCmd.ExecuteScalar() as string;

            if (userRole == "sysadmin")
            {
                result.AddDetail("✓ User has sysadmin role (full permissions)");
            }
            else
            {
                result.AddDetail("⚠ User is not sysadmin. Ensure user has permissions to create SQL Agent jobs.");
            }

            // Try to access sysjobs in msdb
            try
            {
                var jobCheckQuery = "SELECT COUNT(*) FROM msdb.dbo.sysjobs";
                using var jobCmd = new SqlCommand(jobCheckQuery, connection);
                jobCmd.CommandTimeout = 10;
                jobCmd.ExecuteScalar();
                result.AddDetail("✓ Can access msdb.dbo.sysjobs");
            }
            catch (Exception ex)
            {
                result.AddDetail($"⚠ Cannot access msdb.dbo.sysjobs: {ex.Message}");
            }

            result.Success = true;
            result.Message = "SQL Server connection test successful";

            _logger.Information("SQL connection test successful");
            return result;
        }
        catch (SqlException ex)
        {
            _logger.Error(ex, "SQL connection test failed");
            return ValidationResult.Fail($"SQL connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error during SQL connection test");
            return ValidationResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    public static string BuildConnectionString(BackupConfiguration config)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = config.SqlServer,
            UserID = config.SqlUser,
            Password = config.SqlPassword,
            InitialCatalog = config.DatabaseName,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        };

        return builder.ConnectionString;
    }
}
