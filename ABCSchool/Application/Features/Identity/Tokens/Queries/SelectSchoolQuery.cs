using Application.Wrappers;
using MediatR;

namespace Application.Features.Identity.Tokens.Queries
{
    public class SelectSchoolQuery : IRequest<IResponseWrapper>
    {
        public SelectSchoolRequest SelectSchool { get; set; }
    }

    public class SelectSchoolQueryHandler : IRequestHandler<SelectSchoolQuery, IResponseWrapper>
    {
        private readonly ITokenService _tokenService;

        public SelectSchoolQueryHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task<IResponseWrapper> Handle(SelectSchoolQuery request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.SelectSchoolAsync(request.SelectSchool);

            return await ResponseWrapper<TokenResponse>.SuccessAsync(data: token);
        }
    }
}
