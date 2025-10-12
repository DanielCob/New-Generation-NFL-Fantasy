using Microsoft.OpenApi.Models;
using NFL_Fantasy_API.Middleware;
using NFL_Fantasy_API.Services.Implementations;
using NFL_Fantasy_API.Services.Interfaces;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURACIÓN DE SERVICIOS (Dependency Injection Container)
// ============================================================================

#region Logging Configuration

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Ajustar nivel de logging según entorno
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

    // Configuración más restrictiva para producción (descomentar y ajustar)
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar serialización JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
        options.JsonSerializerOptions.WriteIndented = true; // JSON formateado en desarrollo
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // Omitir nulls
    });

#endregion

#region Dependency Injection - Services

// Registrar todos los servicios de la aplicación
// Usar Scoped para servicios que necesitan mantener estado durante una request

// Servicios de autenticación y usuarios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Servicios de ligas
builder.Services.AddScoped<ILeagueService, LeagueService>();

// Servicios de referencia y configuración
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.AddScoped<IScoringService, ScoringService>();

// NUEVOS SERVICIOS - Features 3.1 y 10.1:
builder.Services.AddScoped<INFLTeamService, NFLTeamService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

// Servicios de vistas/reportes
builder.Services.AddScoped<IViewsService, ViewsService>();

// Servicio de auditoría y mantenimiento
builder.Services.AddScoped<IAuditService, AuditService>();

// Servicio de email (SMTP)
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

#endregion

#region Swagger Configuration

// Configurar Swagger/OpenAPI para documentación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "X-NFL Fantasy API",
        Version = "v1.0",
        Description = "API REST para el sistema X-NFL Fantasy Draft Optimizer.\n\n" +
                      "**Features implementados:**\n" +
                      "- Feature 1.1: Registro, autenticación y gestión de perfiles de usuarios\n" +
                      "- Feature 1.2: Creación y administración de ligas de fantasy\n\n" +
                      "- Feature 3.1: Creación y administración de equipos fantasy (branding, roster, distribución)\n\n" +
                      "- Feature 10.1: Gestión de Equipos NFL (CRUD completo con validaciones)\n\n" +
        "**Autenticación:**\n" +
                      "La mayoría de endpoints requieren autenticación Bearer token.\n" +
                      "1. Registrarse en POST /api/auth/register\n" +
                      "2. Iniciar sesión en POST /api/auth/login (retorna SessionID)\n" +
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
        Description = "Ingrese el SessionID (GUID) obtenido al iniciar sesión.\n\n" +
                      "Ejemplo: `3fa85f64-5717-4562-b3fc-2c963f66afa6`\n\n" +
                      "No incluir la palabra 'Bearer', solo el GUID."
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

    // Incluir comentarios XML (opcional, si generas XML documentation)
    /*
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    */
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

var app = builder.Build();

// ============================================================================
// CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE
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
        options.DisplayRequestDuration(); // Mostrar duración de requests
    });

    // Mostrar excepciones detalladas en desarrollo
    app.UseDeveloperExceptionPage();
}
else
{
    // En producción, usar manejador de excepciones genérico
    app.UseExceptionHandler("/error");

    // Forzar HTTPS en producción
    app.UseHsts();
}

#endregion

#region Middleware Pipeline

// IMPORTANTE: El orden de los middleware importa

// 1. HTTPS Redirection (siempre primero)
app.UseHttpsRedirection();

// 2. CORS (debe ir antes de UseRouting)
app.UseCors("AllowAllOrigins"); // En producción, usar "ProductionCors"

// 3. Routing
app.UseRouting();

// 4. AUTHENTICATION MIDDLEWARE PERSONALIZADO
// Este middleware valida el Bearer token y agrega UserID al contexto
// DEBE ir DESPUÉS de UseRouting y ANTES de UseAuthorization
app.UseAuthenticationMiddleware();

// 5. Authorization (si usaras [Authorize] attributes)
// app.UseAuthorization();

// 6. Map Controllers
app.MapControllers();

#endregion

#region Root Endpoint

// Endpoint raíz para verificar que la API está funcionando
app.MapGet("/", () => Results.Ok(new
{
    message = "X-NFL Fantasy API está funcionando correctamente.",
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
            create = "POST /api/league",
            editConfig = "PUT /api/league/{id}/config",
            setStatus = "PUT /api/league/{id}/status",
            summary = "GET /api/league/{id}/summary",
            directory = "GET /api/league/directory",
            members = "GET /api/league/{id}/members",
            teams = "GET /api/league/{id}/teams"
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
        feature_1_1 = "Registro, autenticación y gestión de perfiles de usuarios",
        feature_1_2 = "Creación y administración de ligas de fantasy",
        feature_3_1 = "Creación y administración de equipos fantasy (branding, roster, distribución)",
        feature_10_1 = "Gestión de Equipos NFL (CRUD completo con validaciones)",
        audit = "Sistema de auditoría completo con captura de IP y UserAgent",
        maintenance = "Limpieza automática de sesiones y tokens expirados"
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

// Endpoint para manejo de errores en producción
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