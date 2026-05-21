using ITRockChallenge.Presentation.Filters;

namespace ITRockChallenge.Presentation.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserId(this HttpContext httpContext) =>
        httpContext.Items[AuthenticatedUserFilter.UserIdItemKey] as string
        ?? throw new InvalidOperationException("El filtro de usuario autenticado no estableció el UserId.");
}
