# Ejemplos de Uso de la API

Este archivo contiene ejemplos de uso de la API de BackupConfigurator usando diferentes herramientas.

## Configuración Inicial

**URL Base:** `https://localhost:5001` (desarrollo)

## 1. Ejemplos con cURL

### Listar Todas las Configuraciones

```bash
curl -X GET "https://localhost:5001/api/backupconfigurations" \
  -H "accept: application/json" \
  -k
```

### Listar Configuraciones Activas

```bash
curl -X GET "https://localhost:5001/api/backupconfigurations/active" \
  -H "accept: application/json" \
  -k
```

### Obtener Configuración por ID

```bash
curl -X GET "https://localhost:5001/api/backupconfigurations/1" \
  -H "accept: application/json" \
  -k
```

### Crear Nueva Configuración

```bash
curl -X POST "https://localhost:5001/api/backupconfigurations" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "name": "Backup Diario ProductionDB",
    "databaseName": "ProductionDB",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "C:\\Backups\\Full",
    "isCompressed": true,
    "isEncrypted": false,
    "retentionDays": 30,
    "schedule": "Daily 2:00 AM",
    "createdBy": "Admin"
  }'
```

### Actualizar Configuración

```bash
curl -X PUT "https://localhost:5001/api/backupconfigurations/1" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "id": 1,
    "name": "Backup Diario ProductionDB Actualizado",
    "databaseName": "ProductionDB",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "D:\\Backups\\Full",
    "isCompressed": true,
    "isEncrypted": false,
    "retentionDays": 60,
    "schedule": "Daily 3:00 AM",
    "isActive": true,
    "createdBy": "Admin",
    "lastModifiedBy": "Admin"
  }'
```

### Eliminar Configuración

```bash
curl -X DELETE "https://localhost:5001/api/backupconfigurations/1" \
  -H "accept: application/json" \
  -k
```

### Activar Configuración

```bash
curl -X POST "https://localhost:5001/api/backupconfigurations/1/activate" \
  -H "accept: application/json" \
  -k
```

### Desactivar Configuración

```bash
curl -X POST "https://localhost:5001/api/backupconfigurations/1/deactivate" \
  -H "accept: application/json" \
  -k
```

### Ejecutar Backup

```bash
curl -X POST "https://localhost:5001/api/backups/execute/1" \
  -H "accept: application/json" \
  -k
```

### Validar Configuración

```bash
curl -X POST "https://localhost:5001/api/backups/validate/1" \
  -H "accept: application/json" \
  -k
```

### Listar Bases de Datos Disponibles

```bash
curl -X GET "https://localhost:5001/api/backups/databases?serverName=localhost" \
  -H "accept: application/json" \
  -k
```

## 2. Ejemplos con PowerShell

### Listar Configuraciones

```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/backupconfigurations" `
    -Method Get `
    -SkipCertificateCheck
$response | ConvertTo-Json
```

### Crear Nueva Configuración

```powershell
$body = @{
    name = "Backup Nocturno TestDB"
    databaseName = "TestDB"
    serverName = "localhost"
    backupType = 1
    backupPath = "C:\Backups"
    isCompressed = $true
    isEncrypted = $false
    retentionDays = 30
    schedule = "Daily 1:00 AM"
    createdBy = "PowerShell User"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/backupconfigurations" `
    -Method Post `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck

Write-Host "Configuración creada con ID: $response"
```

### Ejecutar Backup

```powershell
$configId = 1
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/backups/execute/$configId" `
    -Method Post `
    -SkipCertificateCheck

$response | ConvertTo-Json -Depth 5
```

## 3. Ejemplos con Python (requests)

### Configuración Inicial

```python
import requests
import json
from urllib3.exceptions import InsecureRequestWarning

# Desactivar advertencias SSL para desarrollo
requests.packages.urllib3.disable_warnings(category=InsecureRequestWarning)

BASE_URL = "https://localhost:5001/api"
```

### Listar Configuraciones

```python
response = requests.get(
    f"{BASE_URL}/backupconfigurations",
    verify=False
)

if response.status_code == 200:
    configurations = response.json()
    for config in configurations:
        print(f"ID: {config['id']}, Name: {config['name']}")
else:
    print(f"Error: {response.status_code}")
```

### Crear Nueva Configuración

```python
new_config = {
    "name": "Backup Python Auto",
    "databaseName": "MyDatabase",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "C:\\Backups",
    "isCompressed": True,
    "isEncrypted": False,
    "retentionDays": 30,
    "schedule": "Daily 2:00 AM",
    "createdBy": "Python Script"
}

response = requests.post(
    f"{BASE_URL}/backupconfigurations",
    json=new_config,
    verify=False
)

if response.status_code == 201:
    config_id = response.json()
    print(f"Configuración creada con ID: {config_id}")
else:
    print(f"Error: {response.status_code} - {response.text}")
```

### Ejecutar Backup

```python
config_id = 1
response = requests.post(
    f"{BASE_URL}/backups/execute/{config_id}",
    verify=False
)

if response.status_code == 200:
    history = response.json()
    print(f"Backup Status: {history['status']}")
    print(f"File Path: {history['backupFilePath']}")
    print(f"Size: {history['backupSizeBytes']} bytes")
else:
    print(f"Error: {response.status_code}")
```

## 4. Ejemplos con JavaScript (fetch)

### Listar Configuraciones

```javascript
async function listConfigurations() {
    try {
        const response = await fetch('https://localhost:5001/api/backupconfigurations', {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (response.ok) {
            const configurations = await response.json();
            console.log(configurations);
            return configurations;
        } else {
            console.error('Error:', response.status);
        }
    } catch (error) {
        console.error('Error:', error);
    }
}
```

### Crear Nueva Configuración

```javascript
async function createConfiguration() {
    const newConfig = {
        name: "Backup JS Auto",
        databaseName: "WebAppDB",
        serverName: "localhost",
        backupType: 1,
        backupPath: "C:\\Backups",
        isCompressed: true,
        isEncrypted: false,
        retentionDays: 30,
        schedule: "Daily 3:00 AM",
        createdBy: "JavaScript Client"
    };
    
    try {
        const response = await fetch('https://localhost:5001/api/backupconfigurations', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(newConfig)
        });
        
        if (response.status === 201) {
            const configId = await response.json();
            console.log('Configuration created with ID:', configId);
            return configId;
        } else {
            console.error('Error:', response.status);
        }
    } catch (error) {
        console.error('Error:', error);
    }
}
```

### Ejecutar Backup

```javascript
async function executeBackup(configId) {
    try {
        const response = await fetch(`https://localhost:5001/api/backups/execute/${configId}`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (response.ok) {
            const history = await response.json();
            console.log('Backup Status:', history.status);
            console.log('File Path:', history.backupFilePath);
            console.log('Size:', history.backupSizeBytes, 'bytes');
            return history;
        } else {
            console.error('Error:', response.status);
        }
    } catch (error) {
        console.error('Error:', error);
    }
}
```

## 5. Ejemplos con Postman

### Colección de Postman

```json
{
    "info": {
        "name": "BackupConfigurator API",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [
        {
            "name": "Get All Configurations",
            "request": {
                "method": "GET",
                "header": [],
                "url": {
                    "raw": "https://localhost:5001/api/backupconfigurations",
                    "protocol": "https",
                    "host": ["localhost"],
                    "port": "5001",
                    "path": ["api", "backupconfigurations"]
                }
            }
        },
        {
            "name": "Create Configuration",
            "request": {
                "method": "POST",
                "header": [
                    {
                        "key": "Content-Type",
                        "value": "application/json"
                    }
                ],
                "body": {
                    "mode": "raw",
                    "raw": "{\n  \"name\": \"Daily Backup\",\n  \"databaseName\": \"ProductionDB\",\n  \"serverName\": \"localhost\",\n  \"backupType\": 1,\n  \"backupPath\": \"C:\\\\Backups\",\n  \"isCompressed\": true,\n  \"isEncrypted\": false,\n  \"retentionDays\": 30,\n  \"schedule\": \"Daily 2:00 AM\",\n  \"createdBy\": \"Admin\"\n}"
                },
                "url": {
                    "raw": "https://localhost:5001/api/backupconfigurations",
                    "protocol": "https",
                    "host": ["localhost"],
                    "port": "5001",
                    "path": ["api", "backupconfigurations"]
                }
            }
        },
        {
            "name": "Execute Backup",
            "request": {
                "method": "POST",
                "header": [],
                "url": {
                    "raw": "https://localhost:5001/api/backups/execute/1",
                    "protocol": "https",
                    "host": ["localhost"],
                    "port": "5001",
                    "path": ["api", "backups", "execute", "1"]
                }
            }
        }
    ]
}
```

## 6. Tipos de Backup

### Backup Tipo 1 - Full (Completo)

```json
{
  "backupType": 1,
  "name": "Full Backup",
  "databaseName": "ProductionDB",
  "serverName": "localhost",
  "backupPath": "C:\\Backups\\Full",
  "isCompressed": true,
  "createdBy": "Admin"
}
```

### Backup Tipo 2 - Differential (Diferencial)

```json
{
  "backupType": 2,
  "name": "Differential Backup",
  "databaseName": "ProductionDB",
  "serverName": "localhost",
  "backupPath": "C:\\Backups\\Diff",
  "isCompressed": true,
  "createdBy": "Admin"
}
```

### Backup Tipo 3 - Transaction Log

```json
{
  "backupType": 3,
  "name": "Log Backup",
  "databaseName": "ProductionDB",
  "serverName": "localhost",
  "backupPath": "C:\\Backups\\Log",
  "isCompressed": true,
  "createdBy": "Admin"
}
```

## 7. Respuestas de Ejemplo

### Respuesta Exitosa - Crear Configuración

```json
1
```

### Respuesta Exitosa - Listar Configuraciones

```json
[
  {
    "id": 1,
    "name": "Daily Full Backup - ProductionDB",
    "databaseName": "ProductionDB",
    "serverName": "localhost",
    "backupType": 1,
    "backupPath": "C:\\Backups\\Full",
    "isCompressed": true,
    "isEncrypted": false,
    "retentionDays": 30,
    "schedule": "Daily 2:00 AM",
    "isActive": true,
    "createdDate": "2025-12-25T23:00:00Z",
    "lastModifiedDate": null,
    "createdBy": "System",
    "lastModifiedBy": null
  }
]
```

### Respuesta Exitosa - Ejecutar Backup

```json
{
  "id": 1,
  "backupConfigurationId": 1,
  "startTime": "2025-12-25T23:30:00Z",
  "endTime": "2025-12-25T23:32:15Z",
  "status": 2,
  "errorMessage": null,
  "backupSizeBytes": 52428800,
  "backupFilePath": "C:\\Backups\\Full\\ProductionDB_FULL_20251225_233000.bak",
  "executedBy": "Admin"
}
```

### Respuesta de Error

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "El nombre de la configuración es requerido."
}
```

## Notas Importantes

1. **SSL Certificate:** En desarrollo, usar `-k` (curl) o `SkipCertificateCheck` (PowerShell) para ignorar certificados SSL autofirmados.

2. **Rutas de Windows:** En JSON, escapar las barras invertidas: `"C:\\Backups"` en lugar de `"C:\Backups"`.

3. **Autenticación:** Esta versión no incluye autenticación. Para producción, agregar JWT o similar.

4. **Content-Type:** Siempre usar `application/json` para las solicitudes POST y PUT.

5. **Códigos de Estado HTTP:**
   - `200 OK` - Operación exitosa
   - `201 Created` - Recurso creado
   - `204 No Content` - Actualización/eliminación exitosa
   - `400 Bad Request` - Datos inválidos
   - `404 Not Found` - Recurso no encontrado
   - `500 Internal Server Error` - Error del servidor
