namespace FileReport.RestApi.Infrastructure.Security
{
    /// <summary>
    /// Opciones de configuración para validar tokens JWT de Keycloak
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// URL base del servidor Keycloak (ej: http://localhost:8080)
        /// </summary>
        public string Authority { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del realm en Keycloak (ej: proyecto-realm)
        /// </summary>
        public string Realm { get; set; } = string.Empty;

        /// <summary>
        /// Client ID configurado en Keycloak (ej: backend-client)
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Si se requiere HTTPS para la comunicación con Keycloak
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// Construye la URL completa del issuer de Keycloak
        /// </summary>
        public string GetIssuerUrl() => $"{Authority}/realms/{Realm}";

        /// <summary>
        /// Construye la URL del endpoint JWKS de Keycloak
        /// </summary>
        public string GetJwksUrl() => $"{Authority}/realms/{Realm}/protocol/openid-connect/certs";
    }
}
