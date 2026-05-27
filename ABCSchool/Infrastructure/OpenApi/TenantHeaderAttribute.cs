using Infrastructure.Tenancy;

namespace Infrastructure.OpenApi
{
    public class TenantHeaderAttribute() 
        : SwaggerHeaderAttribute(
            headerName: TenancyConstants.TenantIdName, 
            description: "Enter your tenant name to access this API.", 
            defaultValue: string.Empty, 
            isRequired: true)
    {
    }
}
