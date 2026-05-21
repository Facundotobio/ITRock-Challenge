using Asp.Versioning;
using FluentValidation;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Application.Validators;
using ITRockChallenge.Infrastructure;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Http;
using ITRockChallenge.Infrastructure.Repositories;
using ITRockChallenge.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Logging: Configuración y canales de salida
builder.Logging.ClearProviders();

// Salida para Consola formateada como JSON estructurado
builder.Logging.AddJsonConsole(options =>
{
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = true // JSON ordenado en desarrollo
    };
});

// Salida para la ventana de Depuración
builder.Logging.AddDebug();

// Nivel mínimo de captura
builder.Logging.SetMinimumLevel(LogLevel.Information);

// REGISTRO DE INFRAESTRUCTURA - Soporta appsettings local y variables de Render
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
                    ?? builder.Configuration["ConnectionStrings__DefaultConnection"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// BaseUrl (appsettings) + Polly
builder.Services.AddHttpClient<IJsonPlaceholderClient, JsonPlaceholderClient>(client =>
{
    var baseUrl = builder.Configuration["JsonPlaceholderSettings:BaseUrl"]
                  ?? "https://jsonplaceholder.typicode.com";

    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
})
.AddStandardResilienceHandler();

builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<ITaskImportService, TaskImportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITaskService, TaskService>();

// VALIDACIONES
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();

// CONFIGURACIÓN DE SEGURIDAD (JWT)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// CONFIGURACIÓN GLOBAL DE VERSIONADO
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// GENERADOR DE SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Definir esquema de seguridad JWT Bearer
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresá el token JWT de la siguiente manera: Bearer {tu_token}"
    });

    // Hace que Swagger aplique esta seguridad de forma global a los endpoints
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
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Swagger se ejecuta SIEMPRE (tanto en Desarrollo como en Producción)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var descriptions = app.DescribeApiVersions();
    foreach (var description in descriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"ITRockChallenge API {description.GroupName.ToUpperInvariant()}"
        );
    }
});

// Control condicional exclusivo para redirección HTTPS
if (!app.Environment.IsDevelopment())
{
    // Solo forzamos HTTPS en producción, no en el Docker local
    app.UseHttpsRedirection();
}

// Middlewares de seguridad indispensables
app.UseAuthentication();
app.UseAuthorization();

// REGISTRO DE ENDPOINTS
app.MapAuthEndpoints();
app.MapTaskEndpoints();

//MIGRACIONES AUTOMÁTICAS EN PRODUCCIÓN
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Esto ejecuta las migraciones pendientes en el Postgres de Render al iniciar
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones en la base de datos.");
    }
}

app.Run();

public partial class Program { }