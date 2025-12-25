using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BackupConfigurator.API.Controllers;

/// <summary>
/// API Controller para ejecutar backups
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackupsController : ControllerBase
{
    private readonly IBackupExecutionService _executionService;
    private readonly IBackupConfigurationService _configService;
    private readonly ILogger<BackupsController> _logger;

    public BackupsController(
        IBackupExecutionService executionService,
        IBackupConfigurationService configService,
        ILogger<BackupsController> logger)
    {
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta un backup basado en una configuración
    /// </summary>
    [HttpPost("execute/{configurationId}")]
    [ProducesResponseType(typeof(BackupHistory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BackupHistory>> ExecuteBackup(int configurationId)
    {
        var configuration = await _configService.GetConfigurationAsync(configurationId);
        
        if (configuration == null)
            return NotFound($"Configuración con ID {configurationId} no encontrada.");

        try
        {
            var history = await _executionService.ExecuteBackupAsync(configuration);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando backup para configuración {ConfigurationId}", configurationId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Error ejecutando backup", message = ex.Message });
        }
    }

    /// <summary>
    /// Valida una configuración de backup
    /// </summary>
    [HttpPost("validate/{configurationId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ValidateConfiguration(int configurationId)
    {
        var configuration = await _configService.GetConfigurationAsync(configurationId);
        
        if (configuration == null)
            return NotFound($"Configuración con ID {configurationId} no encontrada.");

        var isValid = await _executionService.ValidateConfigurationAsync(configuration);
        return Ok(new { isValid, message = isValid ? "Configuración válida" : "Configuración inválida" });
    }

    /// <summary>
    /// Obtiene la lista de bases de datos disponibles
    /// </summary>
    [HttpGet("databases")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetDatabases([FromQuery] string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return BadRequest("El nombre del servidor es requerido.");

        try
        {
            var databases = await _executionService.GetDatabasesAsync(serverName);
            return Ok(databases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo bases de datos de {ServerName}", serverName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Error obteniendo bases de datos", message = ex.Message });
        }
    }
}
