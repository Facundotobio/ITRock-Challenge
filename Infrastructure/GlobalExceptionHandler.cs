using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ITRockChallenge.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ocurrió un error no controlado en la aplicación: {Message}", exception.Message);

        var statusCode = HttpStatusCode.InternalServerError;
        var errorMessage = "Ocurrió un error interno inesperado en el servidor. Intente más tarde.";

        if (exception is HttpRequestException or TaskCanceledException)
        {
            statusCode = HttpStatusCode.BadGateway;
            errorMessage = "El servicio externo no responde tras múltiples reintentos. Intente más tarde.";
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = "Error del Servidor",
            Detail = errorMessage,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
