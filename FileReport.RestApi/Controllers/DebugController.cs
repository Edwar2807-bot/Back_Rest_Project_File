using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FileReport.RestApi.Controllers
{
    /// <summary>
    /// Controlador temporal para debugging de tokens JWT
    /// ⚠️ ELIMINAR EN PRODUCCIÓN
    /// </summary>
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint público para decodificar y ver el contenido de un token JWT
        /// </summary>
        [HttpPost("decode-token")]
        [AllowAnonymous]
        public IActionResult DecodeToken([FromBody] TokenRequest request)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                
                if (!handler.CanReadToken(request.Token))
                {
                    return BadRequest(new { error = "Token inválido o mal formado" });
                }

                var token = handler.ReadJwtToken(request.Token);

                var claims = token.Claims.Select(c => new
                {
                    type = c.Type,
                    value = c.Value
                }).ToList();

                return Ok(new
                {
                    issuer = token.Issuer,
                    audience = token.Audiences.ToList(),
                    validFrom = token.ValidFrom,
                    validTo = token.ValidTo,
                    isExpired = token.ValidTo < DateTime.UtcNow,
                    claims = claims,
                    rawHeader = token.Header,
                    message = "✅ Token decodificado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decodificando token");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint protegido para verificar autenticación
        /// </summary>
        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var claims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            }).ToList();

            return Ok(new
            {
                message = "🎉 Autenticación exitosa!",
                userId = User.Identity?.Name,
                isAuthenticated = User.Identity?.IsAuthenticated,
                authenticationType = User.Identity?.AuthenticationType,
                claims = claims
            });
        }

        /// <summary>
        /// Endpoint para verificar conectividad con Keycloak
        /// </summary>
        [HttpGet("keycloak-health")]
        [AllowAnonymous]
        public async Task<IActionResult> KeycloakHealth([FromServices] IConfiguration config)
        {
            try
            {
                var authority = config["Jwt:Authority"];
                var realm = config["Jwt:Realm"];
                var audience = config["Jwt:Audience"];

                var realmUrl = $"{authority}/realms/{realm}";
                var certsUrl = $"{authority}/realms/{realm}/protocol/openid-connect/certs";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var realmResponse = await httpClient.GetAsync(realmUrl);
                var certsResponse = await httpClient.GetAsync(certsUrl);

                return Ok(new
                {
                    keycloakUrl = authority,
                    realm = realm,
                    clientId = audience,
                    realmAccessible = realmResponse.IsSuccessStatusCode,
                    certsAccessible = certsResponse.IsSuccessStatusCode,
                    realmUrl = realmUrl,
                    certsUrl = certsUrl,
                    message = realmResponse.IsSuccessStatusCode && certsResponse.IsSuccessStatusCode
                        ? "✅ Keycloak está accesible"
                        : "❌ Problemas de conectividad con Keycloak"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando Keycloak");
                return StatusCode(500, new
                {
                    error = "No se puede conectar a Keycloak",
                    details = ex.Message
                });
            }
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
