using TrajanEcole.Shared.Library.Models.Requests.Schools;
using FluentValidation;

namespace App.Infrastructure.Validators
{
    public class UpdateSchoolRequestValidator : AbstractValidator<UpdateSchoolRequest>
    {
        public UpdateSchoolRequestValidator()
        {
            RuleFor(request => request.CodeEts)
                .Must(code => !string.IsNullOrEmpty(code))
                .WithMessage("Le code établissement est requis!");

            RuleFor(request => request.NomCourtEts)
                .Must(nom => !string.IsNullOrEmpty(nom))
                .WithMessage("Le nom court est requis!");

            RuleFor(request => request.Name)
                .Must(name => !string.IsNullOrEmpty(name))
                .WithMessage("Le nom de l'école est requis!");
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (request, propertyName) =>
        {
            var validationResult = await ValidateAsync(ValidationContext<UpdateSchoolRequest>
                .CreateWithOptions((UpdateSchoolRequest)request, vst => vst.IncludeProperties(propertyName)));

            if (validationResult.IsValid)
            {
                return [];
            }
            return validationResult.Errors.Select(e => e.ErrorMessage);
        };
    }
}
