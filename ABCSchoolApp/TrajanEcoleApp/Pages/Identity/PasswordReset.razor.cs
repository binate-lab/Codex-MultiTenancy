using TrajanEcole.Shared.Library.Models.Requests.Identity;
using App.Infrastructure.Extensions;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Identity
{
    public partial class PasswordReset
    {
        private ChangePasswordRequest ChangePasswordRequest { get; set; } = new();

        private bool _currentPasswordVisibility;
        private InputType _currentPasswordInput = InputType.Password;
        private string _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        private bool _newPasswordVisibility;
        private InputType _newPasswordInput = InputType.Password;
        private string _newPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        private MudForm _form;

        protected override async Task OnInitializedAsync()
        {
            await SetCurrentUserDetails();
        }

        private async Task SetCurrentUserDetails()
        {
            var state = await _applicationStateProvider.GetAuthenticationStateAsync();

            ChangePasswordRequest.UserId = state.User.GetUserId();
        }

        private async Task ResetPasswordAsync()
        {
            var result = await _userService.ChangeUserPasswordAsync(ChangePasswordRequest);

            if (result.IsSuccessful)
            {
                await _tokenService.LogoutAsync();
                _snackbar.Add("Votre mot de passe a été changé. Connectez vous de nouveau.", Severity.Info);
                _navigation.NavigateTo("/");
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }
        private void TogglePasswordVisibility(bool isNewPassword)
        {
            if (isNewPassword)
            {
                if (_newPasswordVisibility)
                {
                    _newPasswordVisibility = false;
                    _newPasswordInput = InputType.Password;
                    _newPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                }
                else
                {
                    _newPasswordVisibility = true;
                    _newPasswordInput = InputType.Text;
                    _newPasswordInputIcon = Icons.Material.Filled.Visibility;
                }
            }
            else
            {
                if (_currentPasswordVisibility)
                {
                    _currentPasswordVisibility = false;
                    _currentPasswordInput = InputType.Password;
                    _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                }
                else
                {
                    _currentPasswordVisibility = true;
                    _currentPasswordInput = InputType.Text;
                    _currentPasswordInputIcon = Icons.Material.Filled.Visibility;
                }
            }
        }
    }
}
