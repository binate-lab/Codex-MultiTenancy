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

            // #4 : seuls Matricule National + Nom + Prenoms sont obligatoires.
            // Niveau, Classe, Annee scolaire (et tout le reste) sont facultatifs a la
            // creation et pourront etre completes/mis a jour plus tard.
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
