using Asp.Versioning;

namespace ITRockChallenge.Presentation;

public static class ProblemResults
{
    public const string NotFoundTitle = "Recurso no encontrado";
    public const string BadRequestTitle = "Solicitud inválida";
    public const string TaskNotFoundDetail = "Tarea no encontrada o no tiene permisos.";

    public static IResult NotFound(HttpContext httpContext, string detail) =>
        Results.Problem(
            title: NotFoundTitle,
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            instance: httpContext.Request.Path);

    public static IResult BadRequest(HttpContext httpContext, string detail) =>
        Results.Problem(
            title: BadRequestTitle,
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            instance: httpContext.Request.Path);

    public static string BuildTaskLocation(HttpContext httpContext, Guid taskId)
    {
        var apiVersion = httpContext.GetRequestedApiVersion();
        var versionSegment = apiVersion != null ? $"v{apiVersion.MajorVersion}" : "v1";
        return $"/api/{versionSegment}/tasks/{taskId}";
    }
}
