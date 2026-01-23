# FileReport REST API

API REST en .NET 8 para exponer reportes de archivos consumiendo un backend SOAP. Incluye autenticación JWT, manejo global de errores y generación de URLs presignadas en MinIO.

## Tecnologías
- ASP.NET Core 8 (Web API)
- WCF client (System.ServiceModel) para SOAP
- Polly (reintentos y circuit breaker)
- MinIO SDK para presignado de descargas
- JWT (Microsoft.AspNetCore.Authentication.JwtBearer)
- Swagger / OpenAPI

## Estructura principal
- `Controllers/FilesController.cs` Endpoints públicos de archivos
- `Application/Services/FileService.cs` Lógica de negocio
- `Infrastructure/Soap/` Cliente SOAP y proxy generado (Reference.cs)
- `Infrastructure/Minio/MinioPresignService.cs` Presignado de URLs
- `Infrastructure/Errors/ErrorHandlingMiddleware.cs` Manejo global de errores HTTP
- `Infrastructure/Security/JwtOptions.cs` Configuración JWT

## Configuración
Archivo `appsettings.json` (ejemplo mínimo):
```json
{
  "Jwt": {
    "Issuer": "file-report",
    "Audience": "file-report-clients",
    "SigningKey": "clave-secreta-minima"
  },
  "Soap": {
    "Endpoint": "http://localhost:8080/ws"
  },
  "Minio": {
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minio",
    "SecretKey": "minio123",
    "Bucket": "reports",
    "PresignExpirySeconds": 3600
  }
}
```

## Ejecución
```bash
dotnet restore FileReport.RestApi/FileReport.RestApi.csproj
dotnet run --project FileReport.RestApi/FileReport.RestApi.csproj
```
Swagger: `https://localhost:44371/swagger/index.html` (puerto según `launchSettings.json`).

## Autenticación
JWT Bearer. Incluir `Authorization: Bearer <token>` en cada petición. Swagger tiene soporte para probar con token.

## Endpoints
Base: `/api/v1`

- `GET /files/{fileId}`
  - Devuelve metadatos del archivo por UUID (SOAP: GetFileByUuid).

- `GET /files/search?fileName={name}`
  - Busca archivos por nombre (SOAP: SearchFilesByName).

- `GET /files?page=1`
  - Lista paginada (SOAP: ListAllFilesPaginated).

- `GET /users/{userId}/files` *(si existe UsersController en tu rama)*
  - Lista archivos por usuario (SOAP: ListFilesByUser).

- `POST /files/{fileId}/download/original`
  - Genera URL de descarga presignada desde MinIO (no encripta).

- `POST /files/{fileId}/download/encrypted`
  - Genera URL de descarga presignada (versión encriptada si aplica).

## Ejemplos (curl)

### Obtener archivo por UUID
```bash
curl -X GET "https://localhost:44371/api/v1/files/696dbf10-eff8-11f0-898a-f60de9c2931f" \
  -H "Authorization: Bearer <token>"
```

### Buscar por nombre
```bash
curl -X GET "https://localhost:44371/api/v1/files/search?fileName=documento" \
  -H "Authorization: Bearer <token>"
```

### Lista paginada
```bash
curl -X GET "https://localhost:44371/api/v1/files?page=1" \
  -H "Authorization: Bearer <token>"
```

### Presignado de descarga
```bash
curl -X POST "https://localhost:44371/api/v1/files/696dbf10-eff8-11f0-898a-f60de9c2931f/download/original" \
  -H "Authorization: Bearer <token>"
```

## Manejo de errores
- Middleware global devuelve JSON `{ error, status, message }`.
- Circuit breaker (Polly) para llamadas SOAP: cuando el backend está caído, responde 503.
- `KeyNotFoundException` -> 404; `ArgumentException` -> 400.

## Notas sobre SOAP
- Proxy generado con `Microsoft.Tools.ServiceModel.Svcutil` (Reference.cs).
- Cliente personalizado en `Infrastructure/Soap/FileReportSoapClient.cs` con políticas de resiliencia y fallbacks HTTP.

## Docker
```bash
docker build -t filereport-api -f FileReport.RestApi/Dockerfile .
docker run -p 8080:8080 filereport-api
```
