using AuthService.Api.Extensions;
using AuthService.Api.Middlewares;
using AuthService.Api.ModelBinders;
using AuthService.Persistence.Data;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;
using Serilog;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

// Carga variables de entorno desde un archivo `.env` si existe (útil para
// desarrollo local con `dotnet run`). En Docker las variables ya vienen del
// env_file de docker-compose, así que el archivo no se encuentra y
// DotNetEnv.Env.Load() simplemente no hace nada. Se ejecuta una sola vez al
// iniciar, sin impacto en el rendimiento en runtime.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));


builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new FileDataModelBinderProvider());
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});


builder.Services.AddApiDocumentation();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimitingPolicies();


builder.Services.AddSecurityPolicies(builder.Configuration);
builder.Services.AddSecurityOptions();


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "NovaPay - Auth Service API",
        Version = "v1",
        Description = "Servicio de autenticación y gestión de usuarios para la plataforma NovaPay. " +
                      "Emite los JWT que consumen los microservicios Novapay-admin y Novapay-user.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Equipo NovaPay",
            Url = new Uri("https://github.com/jackfallasoficial/NovaPay")
        }
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();


if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NovaPay Auth Service v1");
        c.DocumentTitle = "NovaPay Auth Service";
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();

app.UseSecurityHeaders(policies => policies
    .AddDefaultSecurityHeaders()
    .RemoveServerHeader()
    .AddFrameOptionsDeny()
    .AddXssProtectionBlock()
    .AddContentTypeOptionsNoSniff()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddContentSecurityPolicy(builder =>
    {
        builder.AddDefaultSrc().Self();
        builder.AddScriptSrc().Self().UnsafeInline();
        builder.AddStyleSrc().Self().UnsafeInline();
        builder.AddImgSrc().Self().Data();
        builder.AddFontSrc().Self().Data();
        builder.AddConnectSrc().Self();
        builder.AddFrameAncestors().None();
        builder.AddBaseUri().Self();
        builder.AddFormAction().Self();
    })
    .AddCustomHeader("Permissions-Policy", "geolocation=(), microphone=(), camera=()")
    .AddCustomHeader("Cache-Control", "no-store, no-cache, must-revalidate, private")
);

app.UseMiddleware<GlobalExceptionMiddleware>();


if (Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT") != "true")
{
    app.UseHttpsRedirection();
}
app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapHealthChecks("/health");


app.MapGet("/health", () =>
{
    var response = new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    };
    return Results.Ok(response);
});

app.MapHealthChecks("/api/v1/health");



var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = (IEnumerable<string>?)addressesFeature?.Addresses ?? app.Urls;

        if (addresses != null && addresses.Any())
        {
            foreach (var addr in addresses)
            {
                var health = $"{addr.TrimEnd('/')}/health";
                startupLogger.LogInformation("AuthService API is running at {Url}. Health endpoint: {HealthUrl}", addr, health);
            }
        }
        else
        {
            startupLogger.LogInformation("AuthService API started. Health endpoint: /health");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Failed to determine the listening addresses for startup log");
    }
});


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking database connection...");

        // EnsureCreated crea el esquema base (roles, users, etc.)
        await context.Database.EnsureCreatedAsync();

        // Crear tablas exclusivas del auth-service con SQL explícito para que
        // existan aunque EnsureCreated las haya omitido por detectar tablas previas.
        // Se ejecutan DESPUÉS de EnsureCreated para garantizar que users ya existe.
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS user_emails (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                email_verified BOOLEAN NOT NULL DEFAULT FALSE,
                email_verification_token TEXT,
                email_verification_token_expiry TIMESTAMP WITH TIME ZONE
            );

            CREATE TABLE IF NOT EXISTS user_password_resets (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                password_reset_token TEXT,
                password_reset_token_expiry TIMESTAMP WITH TIME ZONE
            );
        ");

        logger.LogInformation("Database ready. Running seed data...");
        await DataSeeder.SeedAsync(context);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}


app.Run();
