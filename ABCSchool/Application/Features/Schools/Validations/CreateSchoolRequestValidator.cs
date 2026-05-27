using FluentValidation;

namespace Application.Features.Schools.Validations
{
    internal class CreateSchoolRequestValidator : AbstractValidator<CreateSchoolRequest>
    {
        public CreateSchoolRequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                    .WithMessage("Le nom de la structure est obligatoire!")
                .MaximumLength(60);

            RuleFor(request => request.EstablishedDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("La date de création ne peut pas être une date du futur!");
        }
    }
}
