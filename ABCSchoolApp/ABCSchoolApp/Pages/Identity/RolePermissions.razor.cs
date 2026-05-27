using ABCShared.Library.Constants;
using ABCShared.Library.Models.Requests.Identity;
using ABCShared.Library.Models.Responses.Identity;
using App.Infrastructure.Constants;
using App.Infrastructure.Extensions;
using App.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace ABCSchoolApp.Pages.Identity
{
    public partial class RolePermissions
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject]
        protected IAuthorizationService AuthService { get; set; } = default!;

        [Parameter]
        public string RoleId { get; set; } = string.Empty;

        private bool _isLoading = true;
        private string _title = string.Empty;
        private string _description = string.Empty;

        private RoleResponse _roleClaimResponse = new();
        private Dictionary<string, List<RolePermissionViewModel>> RoleClaimsGroup { get; set; } = [];

        private bool _canUpdateRolePermission;

        protected override async Task OnInitializedAsync()
        {
            // Permissions
            var user = (await AuthState).User;

            _canUpdateRolePermission = await AuthService.HasPermissionAsync(user, SchoolFeature.RoleClaims, SchoolAction.Update);
            // Load data
            await GetRolePermissionsAsync(user);
            _isLoading = false;
        }

        private async Task GetRolePermissionsAsync(ClaimsPrincipal user)
        {
            var result = await _roleService.GetRoleWithPermissionsAsync(RoleId);
            if (result.IsSuccessful)
            {
                _roleClaimResponse = result.Data;

                _title = "Gestion des Permissions";
                _description = string.Format("Gérer {0} Permissions", _roleClaimResponse.Name);

                var permissions = user.GetTenant() == MultitenancyConstants.RootId 
                    ? SchoolPermissions.All
                    : SchoolPermissions.Admin;

                RoleClaimsGroup = permissions
                    .GroupBy(p => p.Feature)
                    .ToDictionary(g => g.Key, g => g.Select(p =>
                    {
                        var permission = new RolePermissionViewModel(
                            Action: p.Action,
                            Feature: p.Feature,
                            Description: p.Description,
                            Group: p.Group,
                            IsBasic: p.IsBasic,
                            IsRoot: p.IsRoot);

                        permission.IsSelected = _roleClaimResponse.Permissions.Contains(permission.Name);
                        return permission;
                    }).ToList());
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
                _navigation.NavigateTo("/roles");
            }
        }

        private async Task UpdateRolePermissionsAsync()
        {
            var allPermissions = RoleClaimsGroup.Values.SelectMany(permissions => permissions);

            var request = new UpdateRolePermissionsRequest()
            {
                RoleId = RoleId,
                NewPermissions = allPermissions
                    .Where(rpvm => rpvm.IsSelected)
                        .Select(permission => permission.Name)
                    .ToList()
            };

            var result = await _roleService.UpdatePermissionsAsync(request);
            if (result.IsSuccessful)
            {
                _snackbar.Add(result.Messages[0], Severity.Success);
                _navigation.NavigateTo("/roles");
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private Color GetGroupBadgeColor(int selected, int all)
        {
            if (selected == 0)
                return Color.Error;
            if (selected == all)
                return Color.Success;
            return Color.Warning;
        }

        private void Cancel()
        {
            _navigation.NavigateTo("/roles");
        }
    }
}
