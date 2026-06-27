using TrajanEcole.Shared.Library.Models.Responses.AnneesScolaires;
using TrajanEcole.Shared.Library.Wrappers;
using App.Infrastructure.Extensions;
using App.Infrastructure.Services.AnneesScolaires;

namespace App.Infrastructure.Services.Implementations.AnneesScolaires
{
    public class AnneeScolaireService : IAnneeScolaireService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public AnneeScolaireService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;
        }

        public async Task<IResponseWrapper<AnneeScolaireResponse>> GetAnneeEnCoursAsync()
        {
            var response = await _httpClient.GetAsync(_apiSettings.AnneeScolaireEndpoints.EnCours);
            return await response.WrapToResponse<AnneeScolaireResponse>();
        }
    }
}
