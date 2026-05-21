using System.Security.Claims;

namespace ITRockChallenge.Presentation.Filters;

public class AuthenticatedUserFilter : IEndpointFilter
{
    public const string UserIdItemKey = "UserId";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        context.HttpContext.Items[UserIdItemKey] = userId;
        return await next(context);
    }
}
