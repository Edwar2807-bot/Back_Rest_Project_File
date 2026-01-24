# 🔧 Script de Verificación y Prueba Completa

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Verificación de Servicios" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar Keycloak
Write-Host "1️⃣  Verificando Keycloak (puerto 8080)..." -ForegroundColor Yellow
try {
    $keycloak = Invoke-RestMethod -Uri "http://localhost:8080/realms/proyecto-realm" -TimeoutSec 3
    Write-Host "   ✅ Keycloak: OK" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Keycloak: NO DISPONIBLE" -ForegroundColor Red
}

# 2. Verificar SOAP
Write-Host ""
Write-Host "2️⃣  Verificando SOAP (puerto 8085)..." -ForegroundColor Yellow
try {
    $soap = Invoke-WebRequest -Uri "http://localhost:8085/ws" -Method GET -TimeoutSec 3 -ErrorAction SilentlyContinue
    if ($soap.StatusCode -eq 405 -or $soap.StatusCode -eq 200) {
        Write-Host "   ✅ SOAP: OK" -ForegroundColor Green
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 405) {
        Write-Host "   ✅ SOAP: OK (405 es normal para SOAP)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ SOAP: NO DISPONIBLE" -ForegroundColor Red
    }
}

# 3. Verificar MinIO
Write-Host ""
Write-Host "3️⃣  Verificando MinIO (puerto 9000)..." -ForegroundColor Yellow
try {
    $minio = Invoke-WebRequest -Uri "http://localhost:9000/minio/health/live" -TimeoutSec 3
    Write-Host "   ✅ MinIO: OK" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️  MinIO: Verificar manualmente" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Obtener Token y Probar API" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 4. Obtener Token
Write-Host "4️⃣  Obteniendo token de Keycloak..." -ForegroundColor Yellow
try {
    $body = @{
        client_id     = "backend-client"
        client_secret = "7923BDoaSdWOP3kkdh8VJMBgwZFjlZUR"
        grant_type    = "password"
        username      = "admin"
        password      = "Admin123."
    }
    
    $response = Invoke-RestMethod `
        -Uri "http://localhost:8080/realms/proyecto-realm/protocol/openid-connect/token" `
        -Method Post `
        -ContentType "application/x-www-form-urlencoded" `
        -Body $body
    
    $token = $response.access_token
    Write-Host "   ✅ Token obtenido!" -ForegroundColor Green
    Write-Host "   📋 Token (primeros 50 caracteres): $($token.Substring(0,50))..." -ForegroundColor Gray
    Write-Host ""
    
    # 5. Probar API
    Write-Host "5️⃣  Probando API REST..." -ForegroundColor Yellow
    Write-Host "   Ingresa el puerto de tu API (ej: 5000, 8086, 8087): " -NoNewline -ForegroundColor Cyan
    $port = Read-Host
    
    Write-Host ""
    Write-Host "   Probando endpoint: http://localhost:$port/api/v1/fileInfo?page=1" -ForegroundColor Gray
    
    try {
        $headers = @{
            Authorization = "Bearer $token"
        }
        
        $result = Invoke-RestMethod `
            -Uri "http://localhost:$port/api/v1/fileInfo?page=1" `
            -Method Get `
            -Headers $headers
        
        Write-Host ""
        Write-Host "   ✅ API REST: OK!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📄 Respuesta:" -ForegroundColor Cyan
        Write-Host ($result | ConvertTo-Json -Depth 3)
        
    } catch {
        Write-Host ""
        Write-Host "   ❌ Error en la API:" -ForegroundColor Red
        Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-Host ""
            Write-Host "   Código de estado: $statusCode" -ForegroundColor Yellow
            
            if ($statusCode -eq 503) {
                Write-Host ""
                Write-Host "   💡 Error 503: El Circuit Breaker está abierto" -ForegroundColor Yellow
                Write-Host "      Posibles causas:" -ForegroundColor Yellow
                Write-Host "      - El servicio SOAP no responde correctamente" -ForegroundColor White
                Write-Host "      - Reinicia tu API .NET después de cambiar el puerto" -ForegroundColor White
                Write-Host "      - Verifica que SOAP esté en puerto 8085" -ForegroundColor White
            }
            elseif ($statusCode -eq 401) {
                Write-Host ""
                Write-Host "   💡 Error 401: Token inválido o expirado" -ForegroundColor Yellow
            }
        }
    }
    
} catch {
    Write-Host "   ❌ Error al obtener token:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Resumen de Configuración" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Puertos configurados:" -ForegroundColor Yellow
Write-Host "   - Keycloak: 8080" -ForegroundColor White
Write-Host "   - SOAP Service: 8085 ✅ (actualizado)" -ForegroundColor Green
Write-Host "   - API REST: 8086 / 8087 (o tu puerto local)" -ForegroundColor White
Write-Host "   - MinIO: 9000" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Recuerda: Después de cambiar appsettings.json," -ForegroundColor Yellow
Write-Host "   debes REINICIAR tu aplicación .NET" -ForegroundColor Yellow
Write-Host ""
