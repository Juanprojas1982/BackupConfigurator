using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BackupConfigurator.API.Controllers;

/// <summary>
/// API Controller para gestionar configuraciones de backup
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackupConfigurationsController : ControllerBase
{
    private readonly IBackupConfigurationService _configService;
    private readonly ILogger<BackupConfigurationsController> _logger;

    public BackupConfigurationsController(
        IBackupConfigurationService configService,
        ILogger<BackupConfigurationsController> logger)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene todas las configuraciones de backup
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BackupConfiguration>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BackupConfiguration>>> GetAll()
    {
        var configurations = await _configService.GetAllConfigurationsAsync();
        return Ok(configurations);
    }

    /// <summary>
    /// Obtiene configuraciones activas
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<BackupConfiguration>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BackupConfiguration>>> GetActive()
    {
        var configurations = await _configService.GetActiveConfigurationsAsync();
        return Ok(configurations);
    }

    /// <summary>
    /// Obtiene una configuración por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BackupConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BackupConfiguration>> GetById(int id)
    {
        var configuration = await _configService.GetConfigurationAsync(id);
        
        if (configuration == null)
            return NotFound($"Configuración con ID {id} no encontrada.");

        return Ok(configuration);
    }

    /// <summary>
    /// Crea una nueva configuración de backup
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> Create([FromBody] BackupConfiguration configuration)
    {
        try
        {
            var id = await _configService.CreateConfigurationAsync(configuration);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Actualiza una configuración existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] BackupConfiguration configuration)
    {
        if (id != configuration.Id)
            return BadRequest("El ID no coincide.");

        try
        {
            var result = await _configService.UpdateConfigurationAsync(configuration);
            
            if (!result)
                return NotFound($"Configuración con ID {id} no encontrada.");

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Elimina una configuración
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _configService.DeleteConfigurationAsync(id);
        
        if (!result)
            return NotFound($"Configuración con ID {id} no encontrada.");

        return NoContent();
    }

    /// <summary>
    /// Activa una configuración
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _configService.ActivateConfigurationAsync(id);
        
        if (!result)
            return NotFound($"Configuración con ID {id} no encontrada.");

        return NoContent();
    }

    /// <summary>
    /// Desactiva una configuración
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _configService.DeactivateConfigurationAsync(id);
        
        if (!result)
            return NotFound($"Configuración con ID {id} no encontrada.");

        return NoContent();
    }
}
