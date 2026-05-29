using Infrastructure.Tenancy;

namespace Infrastructure.OpenApi
{
    public class TenantHeaderAttribute() 
        : SwaggerHeaderAttribute(
            headerName: TenancyConstants.TenantIdName, 
            description: "Enter the tenant identifier to access this API, for example: heleis.", 
            defaultValue: string.Empty, 
            isRequired: true)
    {
    }
}
