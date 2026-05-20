using Asp.Versioning;
using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;

namespace ITRockChallenge.Presentation
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var group = app.MapGroup("/api/v{version:apiVersion}/auth")
                .WithApiVersionSet(apiVersionSet)
                .WithOpenApi();

            group.MapPost("/login", (LoginRequest request, ITokenService tokenService) =>
            {
                // Validación estática requerida por el enunciado
                if (request.Username == "admin" && request.Password == "password123")
                {
                    // Mapear el admin al userId "1" (Requerimiento para la importación)
                    string token = tokenService.GenerateToken(request.Username, "1");
                    return Results.Ok(new AuthResponse(token, request.Username));
                }

                return Results.Unauthorized();
            })
            .WithName("Login")
            .WithOpenApi();
        }
    }
}