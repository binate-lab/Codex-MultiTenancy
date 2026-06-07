using FluentValidation;

namespace Application.Features.Certificats.Validations
{
    public class SoumettreDemandeRequestValidator : AbstractValidator<SoumettreDemandeRequest>
    {
        public SoumettreDemandeRequestValidator()
        {
            RuleFor(r => r.NomAppareil)
                .NotEmpty().WithMessage("Le nom de l'appareil est obligatoire.")
                .MaximumLength(200).WithMessage("Le nom de l'appareil ne peut pas dépasser 200 caractères.");

            RuleFor(r => r.Description)
                .NotEmpty().WithMessage("La description est obligatoire.")
                .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères.");
        }
    }
}
