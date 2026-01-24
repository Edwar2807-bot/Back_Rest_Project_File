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
JWT Bearer con **Keycloak** como Identity Provider. Incluir `Authorization: Bearer <token>` en cada petición. Swagger tiene soporte integrado para probar con token.

> Ver sección [Autenticación con Keycloak](#autenticación-con-keycloak) para detalles de configuración.

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

### Build y ejecución simple
```bash
docker build -t filereport-api -f FileReport.RestApi/Dockerfile .
docker run -p 8080:8080 filereport-api
```

### Docker Compose (múltiples instancias)

El proyecto incluye un archivo `docker-compose.yml` que permite ejecutar **dos instancias** del servicio simultáneamente en diferentes puertos:

| Servicio | Puerto Host | Puerto Contenedor | Container Name |
|----------|-------------|-------------------|----------------|
| `filereport-api-1` | **8085** | 8080 | filereport-api-8085 |
| `filereport-api-2` | **8086** | 8080 | filereport-api-8086 |

#### Comandos Docker Compose

```bash
# Construir y ejecutar ambas instancias
docker-compose up --build

# Ejecutar en segundo plano (detached)
docker-compose up --build -d

# Ver logs de los contenedores
docker-compose logs -f

# Detener los servicios
docker-compose down

# Detener y eliminar volúmenes
docker-compose down -v
```

#### Acceso a las APIs
- **Instancia 1**: `http://localhost:8085/swagger`
- **Instancia 2**: `http://localhost:8086/swagger`

#### Variables de entorno configuradas
Las siguientes variables de entorno sobrescriben la configuración de `appsettings.json`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - Soap__Endpoint=http://host.docker.internal:8080/ws
  - Minio__Endpoint=http://host.docker.internal:9000
  - Minio__AccessKey=minioadmin
  - Minio__SecretKey=minioadmin
  - Minio__Bucket=file-pipeline
  - Minio__PresignExpirySeconds=120
  - Jwt__Authority=http://host.docker.internal:8080
  - Jwt__Realm=proyecto-realm
  - Jwt__Audience=backend-client
  - Jwt__RequireHttpsMetadata=false
```

> **Nota**: `host.docker.internal` permite que los contenedores accedan a servicios que se ejecutan en el host (como Keycloak, MinIO o el backend SOAP).

---

## Autenticación con Keycloak

La API utiliza **Keycloak** como servidor de identidad (Identity Provider) para la validación de tokens JWT.

### Configuración de Keycloak

La configuración se realiza en `appsettings.json` en la sección `Jwt`:

```json
{
  "Jwt": {
    "Authority": "http://localhost:8080",
    "Realm": "proyecto-realm",
    "Audience": "backend-client",
    "RequireHttpsMetadata": false
  }
}
```

| Parámetro | Descripción |
|-----------|-------------|
| `Authority` | URL base del servidor Keycloak |
| `Realm` | Nombre del realm configurado en Keycloak |
| `Audience` | Client ID configurado en Keycloak (debe coincidir con el claim `aud` o `azp` del token) |
| `RequireHttpsMetadata` | `false` para desarrollo local, `true` en producción |

### URLs construidas automáticamente

A partir de la configuración, el sistema construye automáticamente:

- **Issuer URL**: `{Authority}/realms/{Realm}`
  - Ejemplo: `http://localhost:8080/realms/proyecto-realm`
- **JWKS URL**: `{Authority}/realms/{Realm}/protocol/openid-connect/certs`
  - Keycloak expone las claves públicas para validar la firma del token

### Validación del Token

El middleware de autenticación valida los siguientes aspectos del token JWT:

| Validación | Descripción |
|------------|-------------|
| `ValidateIssuer` | Verifica que el token fue emitido por Keycloak (`iss` claim) |
| `ValidateAudience` | Verifica que el token está destinado a este cliente (`aud` o `azp` claim) |
| `ValidateLifetime` | Verifica que el token no ha expirado |
| `ValidateIssuerSigningKey` | Verifica la firma del token usando las claves JWKS de Keycloak |
| `ClockSkew` | Tolerancia de 1 minuto para diferencias de reloj |

### Configuración requerida en Keycloak

1. **Crear un Realm** (ej: `proyecto-realm`)
2. **Crear un Client** (ej: `backend-client`)
   - Access Type: `confidential` o `public` según tu caso
   - Valid Redirect URIs: URLs de tu aplicación
3. **Configurar el Audience** en el client:
   - En Client Scopes → Mappers, agregar un mapper de tipo `Audience`
   - O verificar que el claim `azp` contenga el Client ID

### Obtener token de Keycloak

```bash
# Obtener token usando Resource Owner Password Grant (solo para pruebas)
curl -X POST "http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=backend-client" \
  -d "client_secret=<client-secret>" \
  -d "username=<usuario>" \
  -d "password=<contraseña>"
```

### Usar el token en las peticiones

```bash
# Ejemplo de petición autenticada
curl -X GET "http://localhost:8085/api/v1/files?page=1" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Debugging de autenticación

El sistema registra eventos de autenticación en los logs:

- **Autenticación fallida**: Se registra el error con detalles de la excepción
- **Token validado**: Se registra el usuario autenticado

Para ver los logs en Docker Compose:
```bash
docker-compose logs -f | grep -i "auth"
```
