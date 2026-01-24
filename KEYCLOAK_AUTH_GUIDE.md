# 🔐 Guía de Autenticación con Keycloak

## Información de Configuración

- **Keycloak URL**: http://localhost:8080
- **Realm**: `proyecto-realm`
- **Client ID**: `backend-client`
- **Client Secret**: `7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR`
- **Usuario de Prueba**: `admin`
- **Contraseña**: `Admin123.`

---

## 🚀 Paso 1: Obtener Token de Acceso

### Opción A: Usando cURL (Terminal)

```bash
curl --location 'http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'client_id=backend-client' \
--data-urlencode 'client_secret=7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR' \
--data-urlencode 'grant_type=password' \
--data-urlencode 'username=admin' \
--data-urlencode 'password=Admin123.'
```

### Opción B: Usando PowerShell

```powershell
$body = @{
    client_id     = "backend-client"
    client_secret = "7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR"
    grant_type    = "password"
    username      = "admin"
    password      = "Admin123."
}

$response = Invoke-RestMethod -Uri "http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $body

Write-Host "Access Token:"
Write-Host $response.access_token
```

### Opción C: Usando Postman

1. **Método**: POST
2. **URL**: `http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token`
3. **Headers**:
   - `Content-Type`: `application/x-www-form-urlencoded`
4. **Body** (x-www-form-urlencoded):
   - `client_id`: `backend-client`
   - `client_secret`: `7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR`
   - `grant_type`: `password`
   - `username`: `admin`
   - `password`: `Admin123.`

---

## 📥 Respuesta Esperada

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJ...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJ...",
  "token_type": "Bearer",
  "not-before-policy": 0,
  "session_state": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "scope": "profile email"
}
```

**Copia el valor de `access_token`** - este es el token JWT que necesitas.

---

## 🧪 Paso 2: Usar el Token en Swagger

1. **Inicia tu API**: Asegúrate de que tu aplicación .NET esté corriendo
   ```bash
   dotnet run --project FileReport.RestApi
   ```

2. **Abre Swagger**: Navega a `http://localhost:{tu_puerto}/swagger`

3. **Autoriza**:
   - Haz clic en el botón **"Authorize"** 🔒 (arriba a la derecha)
   - En el campo **Value**, pega el `access_token` (sin la palabra "Bearer")
   - Haz clic en **"Authorize"**
   - Haz clic en **"Close"**

4. **Prueba los Endpoints**: Ahora todos los endpoints protegidos con `[Authorize]` funcionarán

---

## 📝 Ejemplo de Uso Manual con cURL

### 1. Obtener Token

```bash
TOKEN=$(curl -s --location 'http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'client_id=backend-client' \
--data-urlencode 'client_secret=7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR' \
--data-urlencode 'grant_type=password' \
--data-urlencode 'username=admin' \
--data-urlencode 'password=Admin123.' | jq -r '.access_token')

echo "Token: $TOKEN"
```

### 2. Usar Token en Request

```bash
# Listar archivos paginados
curl --location 'http://localhost:5000/api/v1/fileInfo?page=1' \
--header "Authorization: Bearer $TOKEN"

# Buscar archivo por nombre
curl --location 'http://localhost:5000/api/v1/fileInfo/search?fileName=ejemplo' \
--header "Authorization: Bearer $TOKEN"

# Obtener archivo por ID
curl --location 'http://localhost:5000/api/v1/fileInfo/abc123' \
--header "Authorization: Bearer $TOKEN"

# Generar URL de descarga (original)
curl --location --request POST 'http://localhost:5000/api/v1/fileInfo/abc123/download/original' \
--header "Authorization: Bearer $TOKEN"

# Generar URL de descarga (encriptado)
curl --location --request POST 'http://localhost:5000/api/v1/fileInfo/abc123/download/encrypted' \
--header "Authorization: Bearer $TOKEN"

# Obtener archivos de un usuario
curl --location 'http://localhost:5000/api/v1/users/user123/files' \
--header "Authorization: Bearer $TOKEN"
```

---

## 🔄 Renovar Token (Refresh Token)

Cuando el `access_token` expire (después de 5 minutos por defecto), puedes usar el `refresh_token`:

```bash
curl --location 'http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'client_id=backend-client' \
--data-urlencode 'client_secret=7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR' \
--data-urlencode 'grant_type=refresh_token' \
--data-urlencode 'refresh_token=TU_REFRESH_TOKEN_AQUI'
```

---

## 🐛 Troubleshooting

### Error: "Invalid user credentials"
- Verifica que el usuario `admin` existe en Keycloak
- Ve a Keycloak → Users → admin → Credentials
- Asegúrate de que la contraseña es `Admin123.` y **no es temporal**

### Error: "Client not found" o "Invalid client credentials"
- Verifica el `client_id` y `client_secret` en Keycloak
- Ve a Clients → backend-client → Credentials
- Copia el secret exacto

### Error: "401 Unauthorized" en tu API
- Verifica que el token no haya expirado
- Asegúrate de usar el formato: `Authorization: Bearer {token}`
- Verifica que tu API está corriendo y Keycloak está accesible

### Error: "Direct Access Grants not enabled"
- Ve a Keycloak → Clients → backend-client → Settings
- Activa **"Direct Access Grants Enabled"**
- Guarda los cambios

---

## 📚 Endpoints Protegidos en tu API

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/v1/fileInfo/{fileId}` | Obtener archivo por ID |
| GET | `/api/v1/fileInfo/search?fileName={name}` | Buscar archivos por nombre |
| GET | `/api/v1/fileInfo?page={page}` | Listar archivos paginados |
| POST | `/api/v1/fileInfo/{fileId}/download/original` | Generar URL descarga original |
| POST | `/api/v1/fileInfo/{fileId}/download/encrypted` | Generar URL descarga encriptada |
| GET | `/api/v1/users/{userId}/files` | Obtener archivos de un usuario |

**Todos requieren token JWT válido en el header `Authorization: Bearer {token}`**

---

## 🔧 Verificar Configuración de Keycloak

### 1. Verificar Realm
```bash
curl http://localhost:8080/realms/proyecto-realm
```

**Respuesta esperada:**
```json
{
  "realm": "proyecto-realm",
  "public_key": "MIIBIjAN...",
  "token-service": "http://localhost:8080/realms/proyecto-realm/protocol/openid-connect",
  "account-service": "http://localhost:8080/realms/proyecto-realm/account"
}
```

✅ **Verificado** - Keycloak está corriendo correctamente en `localhost:8080`

### 2. Verificar JWKS (JSON Web Key Set)
```bash
curl http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/certs
```

Esto muestra las claves públicas que tu API usa para validar tokens.

### 3. Verificar OpenID Configuration
```bash
curl http://localhost:8080/realms/proyecto-realm/.well-known/openid-configuration
```

Esto muestra todos los endpoints disponibles de Keycloak.

---

## ✅ Checklist de Configuración

- [x] Keycloak corriendo en http://localhost:8080
- [x] Realm `proyecto-realm` creado
- [ ] Client `backend-client` configurado con:
  - [ ] Client authentication: ON
  - [ ] Standard Flow: ON
  - [ ] Direct Access Grants: ON ⚠️ **IMPORTANTE**
  - [x] Secret: `7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR`
- [ ] Usuario `admin` creado con contraseña `Admin123.` (no temporal) ⚠️ **VERIFICAR**
- [ ] API .NET corriendo y conectada a Keycloak
- [ ] Token obtenido exitosamente
- [ ] Swagger configurado y funcionando con el token

---

## ⚠️ Configuración Actual (Desarrollo)

**Estado:** Las validaciones de `Issuer` y `Audience` están **desactivadas temporalmente** para testing.

**Para Producción:** Una vez que todo funcione, activa las validaciones en [Program.cs](FileReport.RestApi/Program.cs):
```csharp
ValidateIssuer = true,    // Cambiar de false a true
ValidateAudience = true,  // Cambiar de false a true
```

**Y configura el Audience Mapper en Keycloak:**
1. Clients → `backend-client` → Client scopes → `backend-client-dedicated`
2. Add mapper → By configuration → Audience
3. Name: `backend-audience`
4. Included Client Audience: `backend-client`
5. Add to access token: **ON**
