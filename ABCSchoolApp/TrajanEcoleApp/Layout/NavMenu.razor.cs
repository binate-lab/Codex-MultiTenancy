using TrajanEcole.Shared.Library.Constants;
using App.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace TrajanEcoleApp.Layout
{
    public partial class NavMenu
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject]
        protected IAuthorizationService AuthService { get; set; } = default!;

        private bool _canViewTenants;
        private bool _canViewUsers;
        private bool _canViewRoles;
        private bool _canViewSchools;

        protected override async Task OnParametersSetAsync()
        {
            var user = (await AuthState).User;

            _canViewTenants = await AuthService.HasPermissionAsync(user, SchoolFeature.Tenants, SchoolAction.Read);
            _canViewUsers = await AuthService.HasPermissionAsync(user, SchoolFeature.Users, SchoolAction.Read);
            _canViewRoles = await AuthService.HasPermissionAsync(user, SchoolFeature.Roles, SchoolAction.Read);
            _canViewSchools = await AuthService.HasPermissionAsync(user, SchoolFeature.Schools, SchoolAction.Read);
        }
    }
}
