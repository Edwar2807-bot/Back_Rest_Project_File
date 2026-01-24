using FileReport.RestApi.Application.Interfaces;
using FileReport.RestApi.Application.Services;
using FileReport.RestApi.Infrastructure.Minio;
using FileReport.RestApi.Infrastructure.Security;
using FileReport.RestApi.Infrastructure.Soap;
using FileReport.RestApi.Infrastructure.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Configuration
// =======================
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

// =======================
// Controllers & Swagger
// =======================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "File Report REST API",
        Version = "v1"
    });

    // JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =======================
// Authentication (JWT - Keycloak)
// =======================
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Configuración para validar tokens de Keycloak
        options.Authority = jwtOptions.GetIssuerUrl();
        options.Audience = jwtOptions.Audience;
        options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
        
        // Configuración adicional para desarrollo
        options.MetadataAddress = $"{jwtOptions.Authority}/realms/{jwtOptions.Realm}/.well-known/openid-configuration";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // TODO: Cambiar a true en producción una vez que funcione
            ValidateAudience = false, // TODO: Cambiar a true y configurar audience en Keycloak
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.GetIssuerUrl(),
            ValidAudience = jwtOptions.Audience,
            // Keycloak puede usar 'azp' (authorized party) como audience
            ValidAudiences = new[] { jwtOptions.Audience, "account" },
            ClockSkew = TimeSpan.FromMinutes(1),
            // Nombres alternativos de claims que Keycloak puede usar
            NameClaimType = "preferred_username",
            RoleClaimType = "realm_access.roles"
        };

        // Eventos opcionales para debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// =======================
// Dependency Injection
// =======================
builder.Services.AddScoped<IFileReportSoapClient, FileReportSoapClient>();
builder.Services.AddScoped<IMinioPresignService, MinioPresignService>();
builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

// =======================
// Pipeline
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware global de manejo de errores
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
