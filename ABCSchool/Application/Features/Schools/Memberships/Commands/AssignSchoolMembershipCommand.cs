using Application.Wrappers;
using MediatR;

namespace Application.Features.Schools.Memberships.Commands
{
    public class AssignSchoolMembershipCommand : IRequest<IResponseWrapper>
    {
        public AssignSchoolMembershipRequest Assign { get; set; }
    }

    public class AssignSchoolMembershipCommandHandler : IRequestHandler<AssignSchoolMembershipCommand, IResponseWrapper>
    {
        private readonly ISchoolMembershipService _membershipService;

        public AssignSchoolMembershipCommandHandler(ISchoolMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public async Task<IResponseWrapper> Handle(AssignSchoolMembershipCommand request, CancellationToken cancellationToken)
        {
            var membershipId = await _membershipService.AssignAsync(
                request.Assign.UserId,
                request.Assign.SchoolId,
                request.Assign.RoleId);

            return await ResponseWrapper<int>.SuccessAsync(data: membershipId, "Affectation enregistrée avec succès");
        }
    }
}
