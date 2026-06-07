using Application.Features.Certificats.Commands;
using FluentValidation;

namespace Application.Features.Certificats.Validations
{
    public class SoumettreDemandeCommandValidator : AbstractValidator<SoumettreDemandeCommand>
    {
        public SoumettreDemandeCommandValidator()
        {
            RuleFor(c => c.TenantId)
                .NotEmpty().WithMessage("Le tenant est obligatoire.");

            RuleFor(c => c.DemandeParAdminId)
                .NotEmpty().WithMessage("L'identifiant de l'administrateur est obligatoire.");

            RuleFor(c => c.Demande)
                .SetValidator(new SoumettreDemandeRequestValidator());
        }
    }
}
