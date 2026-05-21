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

// REGISTRO DE INFRAESTRUCTURA - Conexión a PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// REGISTRO DE INYECCIÓN DE DEPENDENCIAS Y RESILIENCIA (POLLY)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IJsonPlaceholderClient, JsonPlaceholderClient>(client =>
{
    // Lee la URL desde el appsettings
    var baseUrl = builder.Configuration["JsonPlaceholderSettings:BaseUrl"]
                  ?? "https://jsonplaceholder.typicode.com";

    client.BaseAddress = new Uri(baseUrl);
})
.AddStandardResilienceHandler();

builder.Services.AddScoped<IJsonPlaceholderClient, JsonPlaceholderClient>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
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

app.Run();

public partial class Program { }