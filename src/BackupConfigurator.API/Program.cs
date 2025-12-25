using BackupConfigurator.Core.Interfaces;
using BackupConfigurator.Core.Services;
using BackupConfigurator.Data.Repositories;
using BackupConfigurator.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registrar repositorios
builder.Services.AddScoped<IBackupConfigurationRepository, BackupConfigurationRepository>();
builder.Services.AddScoped<IBackupHistoryRepository, BackupHistoryRepository>();

// Registrar servicios
builder.Services.AddScoped<IBackupConfigurationService, BackupConfigurationService>();
builder.Services.AddScoped<IBackupExecutionService, BackupExecutionService>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
