using Application.Wrappers;
using Domain.Entities;
using MediatR;

namespace Application.Features.Schools.Memberships.Queries
{
    public class GetUserSchoolsQuery : IRequest<IResponseWrapper>
    {
        public string UserId { get; set; }
    }

    public class GetUserSchoolsQueryHandler : IRequestHandler<GetUserSchoolsQuery, IResponseWrapper>
    {
        private readonly ISchoolMembershipService _membershipService;

        public GetUserSchoolsQueryHandler(ISchoolMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public async Task<IResponseWrapper> Handle(GetUserSchoolsQuery request, CancellationToken cancellationToken)
        {
            var schools = await _membershipService.GetUserSchoolsAsync(request.UserId);

            return await ResponseWrapper<List<School>>.SuccessAsync(data: schools);
        }
    }
}
