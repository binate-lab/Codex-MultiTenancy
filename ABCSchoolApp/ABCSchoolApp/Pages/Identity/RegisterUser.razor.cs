using ABCShared.Library.Models.Requests.Identity;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ABCSchoolApp.Pages.Identity
{
    public partial class RegisterUser
    {
        private CreateUserRequest CreateUserRequest { get; set; } = new();
        [CascadingParameter] private IMudDialogInstance _dialogInstance { get; set; }

        private InputType _inputType = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
        private bool _isPasswordVisisble;

        private InputType _inputConfirmType = InputType.Password;
        private string _passwordConfirmInputIcon = Icons.Material.Filled.VisibilityOff;
        private bool _isPasswordConfirmVisisble;

        private MudForm _form = default;

        private async Task SubmitUserRegistrationAsync()
        {
            var result = await _userService.RegisterUserAsync(CreateUserRequest);

            if (result.IsSuccessful)
            {
                _snackbar.Add(result.Messages[0], Severity.Success);
                _dialogInstance.Close(DialogResult.Ok(true));
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private void CancelDialog()
        {
            _dialogInstance.Close();
        }

        void TogglePasswordVisibility()
        {
            if (_isPasswordVisisble)
            {
                _isPasswordVisisble = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _inputType = InputType.Password;
            }
            else
            {
                _isPasswordVisisble = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _inputType = InputType.Text;
            }
        }

        void TogglePasswordConfirmVisibility()
        {
            if (_isPasswordConfirmVisisble)
            {
                _isPasswordConfirmVisisble = false;
                _passwordConfirmInputIcon = Icons.Material.Filled.VisibilityOff;
                _inputConfirmType = InputType.Password;
            }
            else
            {
                _isPasswordConfirmVisisble = true;
                _passwordConfirmInputIcon = Icons.Material.Filled.Visibility;
                _inputConfirmType = InputType.Text;
            }
        }
    }
}
