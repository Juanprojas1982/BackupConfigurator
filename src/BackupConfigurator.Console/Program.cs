using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using BackupConfigurator.Core.Services;
using BackupConfigurator.Data.Repositories;
using BackupConfigurator.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackupConfigurator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            var app = host.Services.GetRequiredService<BackupConfiguratorApp>();
            await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
            return;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Registrar repositorios
                services.AddSingleton<IBackupConfigurationRepository, BackupConfigurationRepository>();
                services.AddSingleton<IBackupHistoryRepository, BackupHistoryRepository>();

                // Registrar servicios
                services.AddSingleton<IBackupConfigurationService, BackupConfigurationService>();
                services.AddSingleton<IBackupExecutionService, BackupExecutionService>();

                // Registrar aplicación principal
                services.AddSingleton<BackupConfiguratorApp>();
            });
}

class BackupConfiguratorApp
{
    private readonly IBackupConfigurationService _configService;
    private readonly IBackupExecutionService _executionService;
    private readonly ILogger<BackupConfiguratorApp> _logger;

    public BackupConfiguratorApp(
        IBackupConfigurationService configService,
        IBackupExecutionService executionService,
        ILogger<BackupConfiguratorApp> logger)
    {
        _configService = configService;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine("  BackupConfigurator - Consola de Gestión");
        System.Console.WriteLine("===========================================");
        System.Console.WriteLine();

        if (args.Length == 0)
        {
            await ShowMenuAsync();
        }
        else
        {
            await ProcessCommandAsync(args);
        }
    }

    private async Task ShowMenuAsync()
    {
        while (true)
        {
            System.Console.WriteLine("\nOpciones:");
            System.Console.WriteLine("1. Listar configuraciones");
            System.Console.WriteLine("2. Crear nueva configuración");
            System.Console.WriteLine("3. Ejecutar backup");
            System.Console.WriteLine("4. Ver historial");
            System.Console.WriteLine("5. Salir");
            System.Console.Write("\nSeleccione una opción: ");

            var option = System.Console.ReadLine();

            switch (option)
            {
                case "1":
                    await ListConfigurationsAsync();
                    break;
                case "2":
                    await CreateConfigurationAsync();
                    break;
                case "3":
                    await ExecuteBackupAsync();
                    break;
                case "4":
                    await ShowHistoryAsync();
                    break;
                case "5":
                    return;
                default:
                    System.Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    private async Task ListConfigurationsAsync()
    {
        System.Console.WriteLine("\n--- Configuraciones de Backup ---");
        var configs = await _configService.GetAllConfigurationsAsync();
        
        foreach (var config in configs)
        {
            System.Console.WriteLine($"\nID: {config.Id}");
            System.Console.WriteLine($"Nombre: {config.Name}");
            System.Console.WriteLine($"Base de datos: {config.DatabaseName}");
            System.Console.WriteLine($"Servidor: {config.ServerName}");
            System.Console.WriteLine($"Tipo: {config.BackupType}");
            System.Console.WriteLine($"Ruta: {config.BackupPath}");
            System.Console.WriteLine($"Activo: {(config.IsActive ? "Sí" : "No")}");
        }
    }

    private async Task CreateConfigurationAsync()
    {
        System.Console.WriteLine("\n--- Crear Nueva Configuración ---");
        
        System.Console.Write("Nombre: ");
        var name = System.Console.ReadLine() ?? "";
        
        System.Console.Write("Servidor: ");
        var server = System.Console.ReadLine() ?? "";
        
        System.Console.Write("Base de datos: ");
        var database = System.Console.ReadLine() ?? "";
        
        System.Console.Write("Ruta de backup: ");
        var path = System.Console.ReadLine() ?? "";
        
        System.Console.Write("Tipo (1=Full, 2=Differential, 3=Log): ");
        var typeStr = System.Console.ReadLine() ?? "1";
        var type = int.TryParse(typeStr, out var t) ? (BackupType)t : BackupType.Full;
        
        var config = new BackupConfiguration
        {
            Name = name,
            ServerName = server,
            DatabaseName = database,
            BackupPath = path,
            BackupType = type,
            IsCompressed = true,
            CreatedBy = Environment.UserName
        };

        try
        {
            var id = await _configService.CreateConfigurationAsync(config);
            System.Console.WriteLine($"\n✓ Configuración creada exitosamente con ID: {id}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"\n✗ Error: {ex.Message}");
        }
    }

    private async Task ExecuteBackupAsync()
    {
        System.Console.WriteLine("\n--- Ejecutar Backup ---");
        System.Console.Write("ID de configuración: ");
        var idStr = System.Console.ReadLine() ?? "";
        
        if (!int.TryParse(idStr, out var id))
        {
            System.Console.WriteLine("ID no válido.");
            return;
        }

        var config = await _configService.GetConfigurationAsync(id);
        if (config == null)
        {
            System.Console.WriteLine("Configuración no encontrada.");
            return;
        }

        System.Console.WriteLine($"\nEjecutando backup de {config.DatabaseName}...");
        
        try
        {
            var history = await _executionService.ExecuteBackupAsync(config);
            
            if (history.Status == BackupStatus.Completed)
            {
                System.Console.WriteLine($"✓ Backup completado exitosamente");
                System.Console.WriteLine($"  Ruta: {history.BackupFilePath}");
                System.Console.WriteLine($"  Tamaño: {history.BackupSizeBytes / 1024 / 1024} MB");
            }
            else
            {
                System.Console.WriteLine($"✗ Backup fallido: {history.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }

    private async Task ShowHistoryAsync()
    {
        System.Console.WriteLine("\n--- Historial de Backups ---");
        System.Console.WriteLine("(Funcionalidad pendiente de implementar)");
        await Task.CompletedTask;
    }

    private async Task ProcessCommandAsync(string[] args)
    {
        var command = args[0].ToLower();
        
        switch (command)
        {
            case "list":
                await ListConfigurationsAsync();
                break;
            case "execute":
                if (args.Length > 1 && int.TryParse(args[1], out var id))
                {
                    var config = await _configService.GetConfigurationAsync(id);
                    if (config != null)
                    {
                        await _executionService.ExecuteBackupAsync(config);
                    }
                }
                break;
            default:
                System.Console.WriteLine("Comando no reconocido. Use: list, execute <id>");
                break;
        }
    }
}
