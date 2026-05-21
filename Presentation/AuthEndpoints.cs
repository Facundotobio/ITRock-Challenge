using Asp.Versioning;
using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Application.Interfaces;

namespace ITRockChallenge.Presentation;

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

        group.MapPost("/login", (LoginRequest request, IAuthService authService, ITokenService tokenService) =>
        {
            var authResult = authService.Authenticate(request);

            if (authResult is null)
            {
                return Results.Unauthorized();
            }

            var token = tokenService.GenerateToken(authResult.Username, authResult.UserId);
            return Results.Ok(new AuthResponse(token, authResult.Username));
        })
        .WithName("Login")
        .WithOpenApi()
        .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }
}
