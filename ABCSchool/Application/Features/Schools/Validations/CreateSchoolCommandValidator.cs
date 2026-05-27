using Application.Features.Schools.Commands;
using FluentValidation;

namespace Application.Features.Schools.Validations
{
    public class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
    {
        public CreateSchoolCommandValidator()
        {
            RuleFor(command => command.CreateSchool)
                .SetValidator(new CreateSchoolRequestValidator());
        }
    }
}
