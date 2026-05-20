using FluentValidation;

namespace ITRockChallenge.Presentation
{
    public class ValidationFilter<T> : IEndpointFilter where T : class
    {
        private readonly IValidator<T> _validator;

        public ValidationFilter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            // Buscarel DTO dentro de los argumentos de la petición HTTP
            var argument = context.Arguments.FirstOrDefault(x => x is T) as T;

            if (argument == null)
            {
                return Results.BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });
            }

            // Ejecutar la validación de forma asíncrona
            var validationResult = await _validator.ValidateAsync(argument);

            if (!validationResult.IsValid)
            {
                // Agrupar los errores por propiedad
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                return Results.ValidationProblem(errors);
            }

            return await next(context);
        }
    }
}
