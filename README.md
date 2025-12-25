# BackupConfigurator

Sistema completo de gestión y configuración de backups para SQL Server desarrollado en .NET 10 y C#.

## 📋 Descripción

BackupConfigurator es una solución empresarial para automatizar y gestionar backups de bases de datos SQL Server. Proporciona una arquitectura modular con API REST, aplicación de consola, y repositorios de datos usando Dapper y ADO.NET.

## 🏗️ Arquitectura del Proyecto

El proyecto sigue una arquitectura en capas con separación de responsabilidades:

```
BackupConfigurator/
├── src/
│   ├── BackupConfigurator.Core/          # Entidades, interfaces y lógica de negocio
│   │   ├── Entities/                     # Modelos de dominio
│   │   ├── Interfaces/                   # Contratos de servicios y repositorios
│   │   └── Services/                     # Implementación de lógica de negocio
│   ├── BackupConfigurator.Data/          # Acceso a datos
│   │   ├── Repositories/                 # Implementación de repositorios
│   │   └── Services/                     # Servicios de ejecución de backups
│   ├── BackupConfigurator.Console/       # Aplicación de consola
│   └── BackupConfigurator.API/           # API REST
│       └── Controllers/                  # Controladores de API
├── database/                             # Scripts SQL
│   ├── 01_CreateDatabase.sql            # Creación de base de datos y tablas
│   ├── 02_CreateStoredProcedures.sql    # Stored procedures
│   └── 03_SampleData.sql                # Datos de ejemplo
└── BackupConfigurator.sln               # Solución de Visual Studio
```

## 🚀 Características

- ✅ **Gestión de Configuraciones**: CRUD completo de configuraciones de backup
- ✅ **Ejecución de Backups**: Soporte para backups Full, Differential y Transaction Log
- ✅ **API REST**: API completa con endpoints documentados
- ✅ **Consola Interactiva**: CLI para gestión manual de backups
- ✅ **Historial de Backups**: Seguimiento de todas las ejecuciones
- ✅ **Validación**: Validación de configuraciones antes de ejecución
- ✅ **Compresión**: Soporte para backups comprimidos
- ✅ **Stored Procedures**: Procedimientos almacenados para estadísticas y limpieza

## 🛠️ Tecnologías Utilizadas

- **.NET 10** - Framework principal
- **C#** - Lenguaje de programación
- **SQL Server** - Base de datos
- **Dapper** - Micro ORM para acceso a datos
- **Microsoft.Data.SqlClient** - Provider de SQL Server
- **ASP.NET Core** - Framework para API REST
- **Dependency Injection** - Inversión de control
- **Entity Framework Core** - (Opcional, actualmente usando Dapper)

## 📦 Requisitos Previos

- .NET 10 SDK
- SQL Server 2016 o superior (Express, Standard, Enterprise)
- Visual Studio 2022 o VS Code
- Windows/Linux/macOS

## 🔧 Instalación y Configuración

### 1. Clonar el Repositorio

```bash
git clone https://github.com/Juanprojas1982/BackupConfigurator.git
cd BackupConfigurator
```

### 2. Configurar la Base de Datos

Ejecutar los scripts SQL en orden:

```bash
# En SQL Server Management Studio o sqlcmd
sqlcmd -S localhost -i database/01_CreateDatabase.sql
sqlcmd -S localhost -i database/02_CreateStoredProcedures.sql
sqlcmd -S localhost -i database/03_SampleData.sql
```

### 3. Configurar Connection Strings

Actualizar `appsettings.json` en cada proyecto:

**BackupConfigurator.Console/appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BackupConfiguratorDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**BackupConfigurator.API/appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BackupConfiguratorDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### 4. Compilar el Proyecto

```bash
dotnet restore
dotnet build
```

## 🚀 Uso

### Aplicación de Consola

```bash
cd src/BackupConfigurator.Console
dotnet run

# O con comandos directos
dotnet run -- list                    # Listar configuraciones
dotnet run -- execute <id>            # Ejecutar backup
```

### API REST

```bash
cd src/BackupConfigurator.API
dotnet run

# La API estará disponible en:
# https://localhost:5001
# http://localhost:5000
```

### Endpoints de API

**Configuraciones de Backup**:
- `GET /api/backupconfigurations` - Listar todas las configuraciones
- `GET /api/backupconfigurations/active` - Listar configuraciones activas
- `GET /api/backupconfigurations/{id}` - Obtener por ID
- `POST /api/backupconfigurations` - Crear nueva configuración
- `PUT /api/backupconfigurations/{id}` - Actualizar configuración
- `DELETE /api/backupconfigurations/{id}` - Eliminar configuración
- `POST /api/backupconfigurations/{id}/activate` - Activar configuración
- `POST /api/backupconfigurations/{id}/deactivate` - Desactivar configuración

**Ejecución de Backups**:
- `POST /api/backups/execute/{configurationId}` - Ejecutar backup
- `POST /api/backups/validate/{configurationId}` - Validar configuración
- `GET /api/backups/databases?serverName=localhost` - Listar bases de datos

### Ejemplo de Uso con cURL

```bash
# Listar configuraciones
curl -X GET https://localhost:5001/api/backupconfigurations

# Crear nueva configuración
curl -X POST https://localhost:5001/api/backupconfigurations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Backup Diario",
    "databaseName": "MyDatabase",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "C:\\Backups",
    "isCompressed": true,
    "createdBy": "Admin"
  }'

# Ejecutar backup
curl -X POST https://localhost:5001/api/backups/execute/1
```

## 📊 Estructura de Base de Datos

### Tabla: BackupConfigurations

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | INT | Identificador único |
| Name | NVARCHAR(200) | Nombre de la configuración |
| DatabaseName | NVARCHAR(128) | Nombre de la base de datos |
| ServerName | NVARCHAR(128) | Nombre del servidor |
| BackupType | INT | 1=Full, 2=Differential, 3=TransactionLog |
| BackupPath | NVARCHAR(500) | Ruta de destino del backup |
| IsCompressed | BIT | Indica si el backup está comprimido |
| IsEncrypted | BIT | Indica si el backup está encriptado |
| RetentionDays | INT | Días de retención |
| Schedule | NVARCHAR(100) | Programación del backup |
| IsActive | BIT | Estado activo/inactivo |

### Tabla: BackupHistory

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | INT | Identificador único |
| BackupConfigurationId | INT | Referencia a la configuración |
| StartTime | DATETIME2 | Hora de inicio |
| EndTime | DATETIME2 | Hora de finalización |
| Status | INT | 1=Running, 2=Completed, 3=Failed, 4=Cancelled |
| ErrorMessage | NVARCHAR(MAX) | Mensaje de error (si aplica) |
| BackupSizeBytes | BIGINT | Tamaño del backup en bytes |
| BackupFilePath | NVARCHAR(500) | Ruta del archivo generado |
| ExecutedBy | NVARCHAR(100) | Usuario que ejecutó el backup |

## 🔒 Stored Procedures

- `usp_GetBackupStatistics` - Obtiene estadísticas de backups
- `usp_CleanupOldBackupHistory` - Limpia historial antiguo
- `usp_GetFailedBackups` - Obtiene backups fallidos
- `usp_ExecuteBackup` - Ejecuta un backup desde SQL

## 🧪 Testing

```bash
dotnet test
```

## 📝 Notas de Desarrollo

- **Patrón Repository**: Implementado para abstraer el acceso a datos
- **Dependency Injection**: Configurado en todos los proyectos
- **Logging**: Implementado usando ILogger de Microsoft.Extensions.Logging
- **Async/Await**: Todas las operaciones son asíncronas
- **Manejo de Errores**: Implementado en todos los niveles

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto es de código abierto y está disponible bajo la licencia MIT.

## 👤 Autor

**Juan Pablo Rojas**

## 📞 Soporte

Para preguntas o soporte, por favor abre un issue en GitHub.

---

⭐ Si este proyecto te fue útil, por favor dale una estrella!
