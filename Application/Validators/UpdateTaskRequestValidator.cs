using FluentValidation;
using ITRockChallenge.Application.Dtos;

namespace ITRockChallenge.Application.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título no puede estar vacío si se incluye en la actualización.")
            .MaximumLength(100).WithMessage("El título no puede superar los 100 caracteres.")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción no puede estar vacía si se incluye en la actualización.")
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.")
            .When(x => x.Description != null);
    }
}