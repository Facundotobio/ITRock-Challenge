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
            // Buscamos el DTO dentro de los argumentos de la petición HTTP
            var argument = context.Arguments.FirstOrDefault(x => x is T) as T;

            if (argument == null)
            {
                return Results.BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });
            }

            // Ejecutamos la validación de forma asíncrona
            var validationResult = await _validator.ValidateAsync(argument);

            if (!validationResult.IsValid)
            {
                // Agrupamos los errores por propiedad para devolver un formato limpio y semántico
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                // Genera un HTTP 400 con la estructura oficial de ValidationProblem
                return Results.ValidationProblem(errors);
            }

            // Si no hay errores, el pipeline continúa con el flujo normal hacia el servicio
            return await next(context);
        }
    }
}
