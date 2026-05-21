using FluentValidation;

namespace ITRockChallenge.Presentation;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(x => x is T) as T;

        if (argument == null)
        {
            return ProblemResults.BadRequest(
                context.HttpContext,
                "El cuerpo de la solicitud no puede estar vacío.");
        }

        var validationResult = await _validator.ValidateAsync(argument);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(
                errors,
                title: ProblemResults.BadRequestTitle,
                detail: "Uno o más campos de la solicitud no son válidos.",
                instance: context.HttpContext.Request.Path);
        }

        return await next(context);
    }
}
