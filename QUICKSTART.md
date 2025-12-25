# Guía de Inicio Rápido - BackupConfigurator

## Configuración Inicial

### 1. Preparar el Entorno

1. Instalar .NET 10 SDK desde https://dotnet.microsoft.com/download
2. Instalar SQL Server 2016+ o SQL Server Express
3. Clonar el repositorio

### 2. Configurar la Base de Datos

**Opción A: Usar SQL Server Management Studio (SSMS)**
1. Abrir SSMS y conectar al servidor
2. Abrir y ejecutar `database/01_CreateDatabase.sql`
3. Abrir y ejecutar `database/02_CreateStoredProcedures.sql`
4. Abrir y ejecutar `database/03_SampleData.sql` (opcional)

**Opción B: Usar sqlcmd**
```bash
sqlcmd -S localhost -E -i database/01_CreateDatabase.sql
sqlcmd -S localhost -E -i database/02_CreateStoredProcedures.sql
sqlcmd -S localhost -E -i database/03_SampleData.sql
```

### 3. Configurar Connection Strings

Editar los archivos `appsettings.json`:

**Para Autenticación de Windows:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BackupConfiguratorDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**Para Autenticación SQL Server:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BackupConfiguratorDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  }
}
```

### 4. Compilar y Ejecutar

```bash
# Compilar todo el proyecto
dotnet build

# Ejecutar la aplicación de consola
cd src/BackupConfigurator.Console
dotnet run

# Ejecutar la API
cd src/BackupConfigurator.API
dotnet run
```

## Uso Básico

### Usando la Aplicación de Consola

1. Ejecutar la aplicación:
```bash
cd src/BackupConfigurator.Console
dotnet run
```

2. Seleccionar opciones del menú interactivo:
   - Opción 1: Ver configuraciones existentes
   - Opción 2: Crear nueva configuración
   - Opción 3: Ejecutar un backup
   - Opción 4: Ver historial
   - Opción 5: Salir

### Usando la API REST

1. Iniciar la API:
```bash
cd src/BackupConfigurator.API
dotnet run
```

2. La API estará disponible en:
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001
   - Swagger UI: https://localhost:5001/openapi/v1.json

3. Probar endpoints con Postman, curl o cualquier cliente HTTP

### Ejemplo Completo: Crear y Ejecutar Backup

**Paso 1: Crear una configuración**

```bash
curl -X POST https://localhost:5001/api/backupconfigurations \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "name": "Backup Diario ProductionDB",
    "databaseName": "ProductionDB",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "C:\\Backups",
    "isCompressed": true,
    "isEncrypted": false,
    "retentionDays": 30,
    "schedule": "Daily 2:00 AM",
    "createdBy": "Admin"
  }'
```

**Paso 2: Listar bases de datos disponibles**

```bash
curl -X GET "https://localhost:5001/api/backups/databases?serverName=localhost" -k
```

**Paso 3: Validar la configuración**

```bash
curl -X POST https://localhost:5001/api/backups/validate/1 -k
```

**Paso 4: Ejecutar el backup**

```bash
curl -X POST https://localhost:5001/api/backups/execute/1 -k
```

## Tipos de Backup

- **BackupType = 1**: Full Backup (Completo)
- **BackupType = 2**: Differential Backup (Diferencial)
- **BackupType = 3**: Transaction Log Backup (Log de transacciones)

## Rutas de Backup

Asegurarse de que:
1. La carpeta de destino existe
2. SQL Server tiene permisos de escritura en la carpeta
3. Hay suficiente espacio en disco

### Rutas comunes:

**Windows:**
- `C:\Backups`
- `D:\SQLBackups`
- `\\servidor\compartido\backups`

**Linux:**
- `/var/opt/mssql/backups`
- `/mnt/backups`

## Solución de Problemas Comunes

### Error: No se puede conectar a SQL Server

**Solución:**
- Verificar que SQL Server esté ejecutándose
- Verificar el nombre del servidor en connection string
- Verificar credenciales de autenticación
- Verificar que TCP/IP esté habilitado en SQL Server

### Error: Acceso denegado a la ruta de backup

**Solución:**
- Verificar que la carpeta exista
- Dar permisos de escritura a la cuenta de servicio de SQL Server
- En Windows: SQL Server Service account necesita permisos
- En Linux: Verificar permisos con chmod/chown

### Error: Base de datos no encontrada

**Solución:**
- Verificar que la base de datos existe
- Usar el endpoint `/api/backups/databases` para listar bases disponibles
- Verificar ortografía del nombre de la base de datos

## Próximos Pasos

1. Configurar backups programados con SQL Server Agent o cron jobs
2. Implementar notificaciones por email
3. Configurar limpieza automática de backups antiguos
4. Implementar cifrado de backups
5. Añadir monitoreo y alertas

## Recursos Adicionales

- [Documentación oficial de SQL Server Backup](https://docs.microsoft.com/sql/relational-databases/backup-restore/)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)

## Soporte

Para problemas o preguntas:
1. Revisar la documentación en README.md
2. Buscar en GitHub Issues
3. Crear un nuevo Issue con detalles del problema
