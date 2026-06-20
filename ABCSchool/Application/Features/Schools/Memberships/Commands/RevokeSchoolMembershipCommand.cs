using Application.Wrappers;
using MediatR;

namespace Application.Features.Schools.Memberships.Commands
{
    public class RevokeSchoolMembershipCommand : IRequest<IResponseWrapper>
    {
        public AssignSchoolMembershipRequest Revoke { get; set; }
    }

    public class RevokeSchoolMembershipCommandHandler : IRequestHandler<RevokeSchoolMembershipCommand, IResponseWrapper>
    {
        private readonly ISchoolMembershipService _membershipService;

        public RevokeSchoolMembershipCommandHandler(ISchoolMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public async Task<IResponseWrapper> Handle(RevokeSchoolMembershipCommand request, CancellationToken cancellationToken)
        {
            await _membershipService.RevokeAsync(
                request.Revoke.UserId,
                request.Revoke.SchoolId,
                request.Revoke.RoleId);

            return await ResponseWrapper.SuccessAsync("Affectation retirée avec succès");
        }
    }
}
