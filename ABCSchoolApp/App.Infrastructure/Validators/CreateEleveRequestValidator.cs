using FluentValidation;
using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace App.Infrastructure.Validators
{
    public class CreateEleveRequestValidator : AbstractValidator<EleveRequestDto>
    {
        public CreateEleveRequestValidator()
        {
            RuleFor(e => e.NumeroMatricule)
                .NotEmpty().WithMessage("Le matricule est requis!");

            RuleFor(e => e.Nom)
                .NotEmpty().WithMessage("Le nom est requis!");

            RuleFor(e => e.Prenom)
                .NotEmpty().WithMessage("Le prenom est requis!");

            RuleFor(e => e.Niveau)
                .NotEmpty().WithMessage("Le niveau est requis!");

            RuleFor(e => e.Classe)
                .NotEmpty().WithMessage("La classe est requise!");

            RuleFor(e => e.AnneeScolaire)
                .NotEmpty().WithMessage("L'annee scolaire est requise!");
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var validationResult = await ValidateAsync(ValidationContext<EleveRequestDto>
                .CreateWithOptions((EleveRequestDto)model, vst => vst.IncludeProperties(propertyName)));

            if (validationResult.IsValid)
            {
                return [];
            }
            return validationResult.Errors.Select(e => e.ErrorMessage);
        };
    }
}
