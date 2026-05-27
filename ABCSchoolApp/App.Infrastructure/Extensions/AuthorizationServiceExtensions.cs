using ABCShared.Library.Constants;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace App.Infrastructure.Extensions
{
    public static class AuthorizationServiceExtensions
    {
        public static async Task<bool> HasPermissionAsync(
            this IAuthorizationService service, 
            ClaimsPrincipal user, 
            string feature, 
            string action)
        {
            return (await service.AuthorizeAsync(user, null, SchoolPermission.NameFor(action, feature))).Succeeded;
        }
    }
}
