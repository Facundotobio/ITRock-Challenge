using Asp.Versioning;
using FluentValidation;
using ITRockChallenge.Application.Interfaces;
using ITRockChallenge.Application.Services;
using ITRockChallenge.Application.Validators;
using ITRockChallenge.Infrastructure.Data;
using ITRockChallenge.Infrastructure.Http;
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 1. Swagger se ejecuta SIEMPRE (tanto en Desarrollo como en Producción)
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

// 2. Control condicional exclusivo para la redirección HTTPS
if (!app.Environment.IsDevelopment())
{
    // Solo forzamos HTTPS en producción (Render), NO en el Docker local
    app.UseHttpsRedirection();
}

// Middlewares de seguridad indispensables
app.UseAuthentication();
app.UseAuthorization();

// 7. REGISTRO DE ENDPOINTS MÍNIMALES
app.MapAuthEndpoints();
app.MapTaskEndpoints();

app.Run();