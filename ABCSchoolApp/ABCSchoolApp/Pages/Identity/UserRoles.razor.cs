using ABCShared.Library.Constants;
using ABCShared.Library.Models.Requests.Identity;
using ABCShared.Library.Models.Responses.Identity;
using App.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ABCSchoolApp.Pages.Identity
{
    public partial class UserRoles
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject]
        protected IAuthorizationService AuthService { get; set; } = default!;

        [Parameter]
        public string UserId { get; set; } = string.Empty;

        private List<UserRoleResponse> _userRoleList = [];
        private UserResponse _userResponse = new();

        private bool _canUpdateUserRoles;
        private bool _isLoading = true;
        private string _title = string.Empty;
        private string _description = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var user = (await AuthState).User;
            _canUpdateUserRoles = await AuthService.HasPermissionAsync(user, SchoolFeature.UserRoles, SchoolAction.Update);

            await GetUserByIdAsync();
            await GetUserRolesAsync();
            _isLoading = false;
        }

        private async Task GetUserByIdAsync() 
        { 
            var result = await _userService.GetByIdAsync(UserId);
            if (result.IsSuccessful)
            {
                _userResponse = result.Data;
                _title = $"{_userResponse.FirstName} {_userResponse.LastName}";
                _description = $"Gère les roles de {_userResponse.FirstName} {_userResponse.LastName}.";
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task GetUserRolesAsync()
        {
            var result = await _userService.GetUserRolesAsync(UserId);
            if (result.IsSuccessful)
            {
                _userRoleList = result.Data;
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task UpdateUserRolesAsync()
        {
            var request = new UserRolesRequest
            {
                UserRoles = _userRoleList
                .Select(ur => new UserRoleRequest
                {
                    RoleId = ur.RoleId,
                    Name = ur.Name,
                    Description = ur.Description,
                    IsAssigned = ur.IsAssigned
                }).ToList()
            };

            var result = await _userService.UpdateUserRolesAsync(UserId, request);
            if (result.IsSuccessful)
            {
                _snackbar.Add(result.Messages[0], Severity.Success);
                _navigation.NavigateTo("/users");
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private void Cancel()
        {
            _navigation.NavigateTo("/users");
        }
    }
}
