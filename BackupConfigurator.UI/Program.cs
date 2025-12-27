using Serilog;

namespace BackupConfigurator.UI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configure initial Serilog to Temp folder (until configuration is loaded)
        var tempLogPath = Path.Combine(Path.GetTempPath(), "BackupConfigurator", $"log-{DateTime.Now:yyyyMMdd}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(tempLogPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(tempLogPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("BackupConfigurator application started");
            
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            MessageBox.Show($"Fatal error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }    
}