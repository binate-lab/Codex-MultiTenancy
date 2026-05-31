using ABCShared.Library.Models.Requests.Tenancy;
using ABCShared.Library.Models.Responses.Tenancy;
using ABCShared.Library.Wrappers;

namespace App.Infrastructure.Services.Tenancy
{
    public interface ITenantService
    {
        Task<IResponseWrapper<List<TenantResponse>>> GetAllAsync();
        Task<IResponseWrapper<TenantResponse>> GetByIdAsync(string tenantId);
        Task<IResponseWrapper<string>> CreateAsync(CreateTenantRequest request);
        Task<IResponseWrapper<string>> UpgradeSubscriptionAsync(UpdateTenantSubscriptionRequest request);
        Task<IResponseWrapper<string>> ActivateAsync(string tenantId);
        Task<IResponseWrapper<string>> DeActivateAsync(string tenantId);
        Task<IResponseWrapper<string>> DeleteAsync(string tenantId);
    }
}
