using Application.Wrappers;
using Mapster;
using MediatR;

namespace Application.Features.Schools.Queries
{
    public class GetSchoolsQuery : IRequest<IResponseWrapper>
    {
    }

    public class GetSchoolsQueryHandler : IRequestHandler<GetSchoolsQuery, IResponseWrapper>
    {
        private readonly ISchoolService _schoolService;

        public GetSchoolsQueryHandler(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        public async Task<IResponseWrapper> Handle(GetSchoolsQuery request, CancellationToken cancellationToken)
        {
            var schoolsInDb = await _schoolService.GetAllAsync();

            if (schoolsInDb?.Count > 0)
            {
                return await ResponseWrapper<List<SchoolResponse>>
                    .SuccessAsync(data: schoolsInDb.Adapt<List<SchoolResponse>>());
            }
            return await ResponseWrapper<int>.FailAsync("Aucune structure trouvée!");
        }
    }
}
