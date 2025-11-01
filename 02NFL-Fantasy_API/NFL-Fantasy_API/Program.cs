using Microsoft.OpenApi.Models;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations;
using NFL_Fantasy_API.SharedSystems.Middleware;
using Microsoft.Extensions.Options;
using Minio;
using System.Diagnostics;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Audit;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Auth;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.StorageDatabase.Implementations;
using NFL_Fantasy_API.SharedSystems.StorageConfig;
using NFL_Fantasy_API.SharedSystems.EmailConfig;
using NFL_Fantasy_API.Helpers.Filters;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Auth;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Fantasy;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Audit;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Implementations.Storage;
using NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Implementations.Email;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Audit;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage;
using NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Interfaces.Email;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURACION DE SERVICIOS (Dependency Injection Container)
// ============================================================================

#region Logging Configuration

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Ajustar nivel de logging segun entorno
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

#endregion

#region CORS Configuration

// Configurar CORS para permitir peticiones desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Configuracion mas restrictiva para produccion (descomentar y ajustar)
    /*
    options.AddPolicy("ProductionCors", policy =>
    {
        policy.WithOrigins("https://tu-frontend.com", "https://www.tu-frontend.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    */
});

#endregion

#region Controllers Configuration

// Agregar controllers con configuraciones
builder.Services.AddControllers(options =>
    {
    // Validaci�n autom�tica de ModelState en TODOS los endpoints
    options.Filters.Add<ModelStateValidationFilter>();

    // Manejo centralizado de excepciones en TODA la aplicaci�n
    options.Filters.Add<GlobalExceptionFilter>();

    // (Opcional) Logging de autenticaci�n en endpoints protegidos
    options.Filters.Add<AuthorizationLoggingFilter>();
    }).AddJsonOptions(options =>
    {
        // Configurar serializacion JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
        options.JsonSerializerOptions.WriteIndented = true; // JSON formateado en desarrollo
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // Omitir nulls
    });

#endregion

#region Dependency Injection - Configuration Options

// Configurar opciones desde appsettings.json usando Options Pattern
// Estas configuraciones se inyectan como IOptions<T> en los servicios

// SMTP Configuration para env�o de emails
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp")
);

// MinIO Configuration para almacenamiento de im�genes
builder.Services.Configure<MinIOSettings>(
    builder.Configuration.GetSection("MinIO")
);

#endregion

#region Dependency Injection - Database & Core Infrastructure

// DatabaseHelper: Wrapper de ADO.NET para operaciones con SQL Server
// Scoped: Una instancia por request HTTP
builder.Services.AddScoped<IDatabaseHelper, DatabaseHelper>();

#endregion

#region Dependency Injection - DataAccess Layer

// Capa de acceso a datos (DAL)
// Responsabilidad: Construcci�n de queries, par�metros y mapeo de resultados
// NO contienen l�gica de negocio

// Auth & Users
builder.Services.AddScoped<AuthDataAccess>();
builder.Services.AddScoped<UserDataAccess>();

// Leagues & Teams
builder.Services.AddScoped<LeagueDataAccess>();
builder.Services.AddScoped<TeamDataAccess>();

// NFL Data
builder.Services.AddScoped<NFLTeamDataAccess>();
builder.Services.AddScoped<PlayerDataAccess>();
builder.Services.AddScoped<ScoringDataAccess>();

// System & Configuration
builder.Services.AddScoped<ReferenceDataAccess>();
builder.Services.AddScoped<SeasonDataAccess>();
builder.Services.AddScoped<SystemRolesDataAccess>();
builder.Services.AddScoped<AuditDataAccess>();

#endregion

#region Dependency Injection - Business Logic Services

// Capa de l�gica de negocio (BLL)
// Responsabilidad: Orquestaci�n, validaciones y reglas de negocio
// Delegan acceso a datos a DataAccess

// Authentication & Authorization
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISystemRolesService, SystemRolesService>();

// League Management
builder.Services.AddScoped<ILeagueService, LeagueService>();
builder.Services.AddScoped<ITeamService, TeamService>();

// NFL Data & Configuration
builder.Services.AddScoped<INFLTeamService, NFLTeamService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();

// System Services
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.AddScoped<IAuditService, AuditService>();

#endregion

#region Dependency Injection - Email System (SharedSystems)

// Sistema de env�o de emails
// Ubicaci�n: SharedSystems/Email/
// Implementaci�n actual: SMTP (compatible con SendGrid, Gmail, Office365)

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// NOTA: SmtpSettings ya configurado en secci�n "Configuration Options"

#endregion

#region Dependency Injection - Storage System (SharedSystems)

// Sistema de almacenamiento de im�genes
// Ubicaci�n: SharedSystems/Storage/
// Implementaci�n actual: MinIO (S3-compatible)

// MinIO Client (Singleton - reutilizable en toda la app)
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MinIOSettings>>().Value;

    return new MinioClient()
        .WithEndpoint(config.Endpoint)
        .WithCredentials(config.AccessKey, config.SecretKey)
        .WithSSL(config.UseSSL)
        .Build();
});

// MinIO DataAccess y Service
builder.Services.AddScoped<MinIODataAccess>();
builder.Services.AddScoped<IStorageService, StorageService>();

// MinIO Initializer (ejecuta al arrancar la app)
// Crea bucket y configura pol�tica p�blica si no existe
builder.Services.AddHostedService<MinIOInitializer>();

// NOTA: MinIOSettings ya configurado en secci�n "Configuration Options"

#endregion

#region Dependency Injection - Filters

// Action Filters para validaci�n y logging
// Scoped: Se crean por request

// Validaci�n autom�tica de ModelState
builder.Services.AddScoped<ModelStateValidationFilter>();

// Manejo global de excepciones
builder.Services.AddScoped<GlobalExceptionFilter>();

// Logging de autorizaci�n
builder.Services.AddScoped<AuthorizationLoggingFilter>();

#endregion

#region Swagger Configuration

// Configurar Swagger/OpenAPI para documentacion
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "X-NFL Fantasy API",
        Version = "v1.0",
        Description = "API REST para el sistema X-NFL Fantasy Draft Optimizer.\n\n" +
                      "**Features implementados:**\n" +
                      "- Feature 1.1: Registro, autenticacion y gestion de perfiles de usuarios\n" +
                      "- Feature 1.2: Creacion y administracion de ligas de fantasy\n\n" +
                      "- Feature 3.1: Creacion y administracion de equipos fantasy (branding, roster, distribucion)\n\n" +
                      "- Feature 10.1: Gestion de Equipos NFL (CRUD completo con validaciones)\n\n" +
                      "- Storage: Manejo de im�genes con MinIO\n\n" +
                      "**Autenticacion:**\n" +
                      "La mayoria de endpoints requieren autenticacion Bearer token.\n" +
                      "1. Registrarse en POST /api/auth/register\n" +
                      "2. Iniciar sesion en POST /api/auth/login (retorna SessionID)\n" +
                      "3. Usar SessionID como Bearer token en header Authorization",
        Contact = new OpenApiContact
        {
            Name = "X-NFL Team",
            Email = "support@xnfl.com"
        }
    });

    // Configurar esquema de seguridad Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "GUID",
        In = ParameterLocation.Header,
        Description = "Ingrese el SessionID (GUID) obtenido al iniciar sesion.\n\n" +
                      "Ejemplo: `3fa85f64-5717-4562-b3fc-2c963f66afa6`\n\n"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

#endregion

#region HTTP Configuration

// Configurar HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443; // Puerto HTTPS por defecto
});

#endregion

// ============================================================================
// BUILD APPLICATION
// ============================================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("ADMIN"));
    options.AddPolicy("BrandOrAdmin", p => p.RequireRole("ADMIN", "BRAND_MANAGER"));
});

var app = builder.Build();

// ============================================================================
// CONFIGURACION DEL PIPELINE DE MIDDLEWARE
// ============================================================================

#region Development vs Production Configuration

if (app.Environment.IsDevelopment())
{
    // Habilitar Swagger solo en desarrollo
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "X-NFL Fantasy API v1");
        options.RoutePrefix = "swagger"; // Acceder en /swagger
        options.DocumentTitle = "X-NFL Fantasy API Documentation";
        options.DisplayRequestDuration(); // Mostrar duracion de requests
    });

    // Mostrar excepciones detalladas en desarrollo
    app.UseDeveloperExceptionPage();
}
else
{
    // En produccion, usar manejador de excepciones generico
    app.UseExceptionHandler("/error");

    // Forzar HTTPS en produccion
    app.UseHsts();
}

#endregion

#region Middleware Pipeline

// IMPORTANTE: El orden de los middleware importa

// 1. HTTPS Redirection (siempre primero)
app.UseHttpsRedirection();

// 2. CORS (debe ir antes de UseRouting)
app.UseCors("AllowAllOrigins"); // En produccion, usar "ProductionCors"

// 3. Routing
app.UseRouting();

// 4. AUTHENTICATION MIDDLEWARE PERSONALIZADO
// Este middleware valida el Bearer token y agrega UserID al contexto
// DEBE ir DESPUES de UseRouting y ANTES de UseAuthorization
app.UseAuthenticationMiddleware();

// 5. Authorization (si usaras [Authorize] attributes)
app.UseAuthorization();

// 6. Map Controllers
app.MapControllers();

#endregion

#region Root Endpoint

// Endpoint raiz para verificar que la API esta funcionando
app.MapGet("/", () => Results.Ok(new
{
    message = "X-NFL Fantasy API esta funcionando correctamente.",
    version = "v1.0",
    timestamp = DateTime.UtcNow,
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        auth = new
        {
            register = "POST /api/auth/register",
            login = "POST /api/auth/login",
            logout = "POST /api/auth/logout",
            logoutAll = "POST /api/auth/logout-all",
            requestReset = "POST /api/auth/request-reset",
            resetWithToken = "POST /api/auth/reset-with-token"
        },
        user = new
        {
            profile = "GET /api/user/profile",
            updateProfile = "PUT /api/user/profile",
            header = "GET /api/user/header",
            sessions = "GET /api/user/sessions"
        },
        league = new
        {
            // Endpoints existentes...
            create = "POST /api/league",
            editConfig = "PUT /api/league/{id}/config",
            setStatus = "PUT /api/league/{id}/status",
            summary = "GET /api/league/{id}/summary",
            directory = "GET /api/league/directory",
            members = "GET /api/league/{id}/members",
            teams = "GET /api/league/{id}/teams",
            userRoles = "GET /api/league/{leagueId}/users/{userId}/roles",

            // NUEVOS ENDPOINTS - B�squeda y Uni�n
            search = "GET /api/league/search",
            validatePassword = "POST /api/league/validate-password",
            join = "POST /api/league/join",

            // NUEVOS ENDPOINTS - Gesti�n de Miembros
            removeTeam = "DELETE /api/league/{leagueId}/teams",
            leave = "POST /api/league/{leagueId}/leave",
            assignCoCommissioner = "POST /api/league/{leagueId}/co-commissioner",
            removeCoCommissioner = "DELETE /api/league/{leagueId}/co-commissioner",
            transferCommissioner = "POST /api/league/{leagueId}/transfer-commissioner",
            passwordInfo = "GET /api/league/{leagueId}/password-info"
        },
        nflteam = new
        {
            create = "POST /api/nflteam",
            list = "GET /api/nflteam",
            details = "GET /api/nflteam/{id}",
            update = "PUT /api/nflteam/{id}",
            deactivate = "POST /api/nflteam/{id}/deactivate",
            reactivate = "POST /api/nflteam/{id}/reactivate",
            active = "GET /api/nflteam/active"
        },
        team = new
        {
            updateBranding = "PUT /api/team/{id}/branding",
            getMyTeam = "GET /api/team/{id}/my-team",
            distribution = "GET /api/team/{id}/roster/distribution",
            addPlayer = "POST /api/team/{id}/roster/add",
            removePlayer = "POST /api/team/roster/{rosterId}/remove"
        },
        player = new
        {
            list = "GET /api/player",
            available = "GET /api/player/available",
            byNFLTeam = "GET /api/player/by-nfl-team/{nflTeamId}"
        },
        season = new
        {
            current = "GET /api/seasons/current",
            create = "POST /api/seasons",
            update = "PUT /api/seasons/{id}",
            deactivate = "POST /api/seasons/{id}/deactivate",
            delete = "DELETE /api/seasons/{id}",
            get = "GET /api/seasons/{id}",
            weeks = "GET /api/seasons/{id}/weeks"
        },
        reference = new
        {
            currentSeason = "GET /api/reference/current-season",
            positionFormats = "GET /api/reference/position-formats",
            positionFormatSlots = "GET /api/reference/position-formats/{id}/slots"
        },
        scoring = new
        {
            schemas = "GET /api/scoring/schemas",
            schemaRules = "GET /api/scoring/schemas/{id}/rules"
        },
        audit = new
        {
            logs = "GET /api/audit/logs",
            myHistory = "GET /api/audit/my-history",
            userHistory = "GET /api/audit/users/{userId}/history",
            stats = "GET /api/audit/stats",
            cleanup = "POST /api/audit/cleanup"
        },
        views = new
        {
            leagueSummary = "GET /api/views/leagues/{id}/summary",
            allLeagues = "GET /api/views/leagues/directory",
            activeUsers = "GET /api/views/users/active",
            systemStats = "GET /api/views/system/stats"
        }
    },
    features = new
    {
        feature_1_1 = "Registro, autenticacion y gestion de perfiles de usuarios",
        feature_1_2 = "Creacion y administracion de ligas de fantasy",
        feature_3_1 = "Creacion y administracion de equipos fantasy (branding, roster, distribucion)",
        feature_10_1 = "Gestion de Equipos NFL (CRUD completo con validaciones)",
        audit = "Sistema de auditoria completo con captura de IP y UserAgent",
        maintenance = "Limpieza automatica de sesiones y tokens expirados"
    }
}))
.WithName("Root")
.WithTags("Health");

#endregion

#region Health Check Endpoint

// Endpoint simple de health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
}))
.WithName("HealthCheck")
.WithTags("Health");

#endregion

#region Error Handling Endpoint

// Endpoint para manejo de errores en produccion
app.MapGet("/error", () => Results.Problem(
    title: "Un error ha ocurrido",
    detail: "Por favor, contacte al administrador del sistema.",
    statusCode: 500
))
.ExcludeFromDescription(); // No mostrar en Swagger

#endregion

// ============================================================================
// RUN APPLICATION
// ============================================================================

// Log de inicio
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("========================================");
logger.LogInformation("X-NFL Fantasy API iniciando...");
logger.LogInformation("Entorno: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Swagger disponible en: /swagger");
logger.LogInformation("========================================");

app.Run();