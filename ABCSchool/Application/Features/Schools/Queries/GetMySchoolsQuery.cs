using Application.Features.Identity.Users;
using Application.Features.Schools.Memberships;
using Application.Wrappers;
using Mapster;
using MediatR;
using TrajanEcole.Shared.Library.Constants;

namespace Application.Features.Schools.Queries
{
    // Écoles visibles par l'utilisateur connecté (alimente les cartes de la page d'accueil).
    public class GetMySchoolsQuery : IRequest<IResponseWrapper>
    {
    }

    public class GetMySchoolsQueryHandler : IRequestHandler<GetMySchoolsQuery, IResponseWrapper>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ISchoolService _schoolService;
        private readonly ISchoolMembershipService _membershipService;

        public GetMySchoolsQueryHandler(
            ICurrentUserService currentUserService,
            ISchoolService schoolService,
            ISchoolMembershipService membershipService)
        {
            _currentUserService = currentUserService;
            _schoolService = schoolService;
            _membershipService = membershipService;
        }

        public async Task<IResponseWrapper> Handle(GetMySchoolsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return await ResponseWrapper<int>.FailAsync("Utilisateur non authentifié.");
            }

            // L'Admin tenant-wide voit toutes les écoles du tenant ; les autres utilisateurs
            // ne voient que les écoles auxquelles ils sont affectés (SchoolMembership).
            // Cohérent avec la règle d'accès de select-school.
            var schools = _currentUserService.IsInRole(RoleConstants.Admin)
                ? await _schoolService.GetAllAsync()
                : await _membershipService.GetUserSchoolsAsync(userId);

            return await ResponseWrapper<List<SchoolResponse>>
                .SuccessAsync(data: schools.Adapt<List<SchoolResponse>>());
        }
    }
}
