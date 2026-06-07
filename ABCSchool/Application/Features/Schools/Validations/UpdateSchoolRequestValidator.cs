using Domain.Entities;
using FluentValidation;

namespace Application.Features.Schools.Validations
{
    internal class UpdateSchoolRequestValidator : AbstractValidator<UpdateSchoolRequest>
    {
        public UpdateSchoolRequestValidator(ISchoolService schoolService)
        {
            RuleFor(request => request.Id)
                .NotEmpty()
                .MustAsync(async (id, ct) => await schoolService.GetByIdAsync(id) is School schoolInDb && schoolInDb.Id == id)
                .WithMessage("Cette structure '{PropertyValue}' n'existe pas!");

            RuleFor(request => request.CodeEts)
                .NotEmpty()
                    .WithMessage("Le code établissement est obligatoire!")
                .MaximumLength(20)
                    .WithMessage("Le code établissement ne peut pas dépasser 20 caractères.");

            RuleFor(request => request.NomCourtEts)
                .NotEmpty()
                    .WithMessage("Le nom court de l'établissement est obligatoire!")
                .MaximumLength(11)
                    .WithMessage("Le nom court ne peut pas dépasser 11 caractères.");

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
