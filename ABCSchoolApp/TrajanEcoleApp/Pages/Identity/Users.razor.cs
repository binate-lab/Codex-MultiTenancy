using TrajanEcoleApp.Components;
using TrajanEcole.Shared.Library.Models.Requests.Identity;
using TrajanEcole.Shared.Library.Models.Responses.Identity;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Identity
{
    public partial class Users
    {
        private List<UserResponse> _userList = [];

        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();
            _isLoading = false;
        }

        private async Task LoadUsers()
        {
            var result = await _userService.GetUsersAsync();
            if (result.IsSuccessful)
            {
                _userList = result.Data;
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task InvokeUserRegistrationDialog()
        {
            var options = new DialogOptions 
            { 
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                BackdropClick = false,
                FullWidth = true
            };

            var dialog = await _dialogService.ShowAsync<RegisterUser>(title: null, options: options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadUsers();
            }
        }

        private async Task ActivateOrDeactivativeAsync(UserResponse user)
        {
            if (user.IsActive)
            {
                // Deactivate
                var parameters = new DialogParameters
                {
                    { nameof(Confirmation.Title), "Désactivation Utilisateur" },
                    { nameof(Confirmation.Message), $"Etes vous sûr de vouloir désactiver '{user.FirstName} {user.LastName}'?" },
                    { nameof(Confirmation.ButtonText), "Désactivation" },
                    { nameof(Confirmation.Color), Color.Error },
                    { nameof(Confirmation.InputIcon), Icons.Material.Filled.Person }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Small,
                    BackdropClick = true,
                    FullWidth = true
                };

                var dialog = await _dialogService.ShowAsync<Confirmation>(title: null, parameters, options);
                var result = await dialog.Result;
                if (!result.Canceled)
                {
                    var response = await _userService.ChangeUserStatusAsync(new ChangeUserStatusRequest
                    {
                        UserId = user.Id,
                        Activation = false
                    });

                    if (response.IsSuccessful)
                    {
                        _snackbar.Add(response.Messages[0], Severity.Success);

                        await LoadUsers();
                    }
                    else
                    {
                        foreach (var message in response.Messages)
                        {
                            _snackbar.Add(message, Severity.Error);
                        }
                    }
                }
            }
            else
            {
                // Activate
                var parameters = new DialogParameters
                {
                    { nameof(Confirmation.Title), "Activation Utilisateur" },
                    { nameof(Confirmation.Message), $"Etes vous sûr de vouloir Activer '{user.FirstName} {user.LastName}'?" },
                    { nameof(Confirmation.ButtonText), "Activation" },
                    { nameof(Confirmation.Color), Color.Primary },
                    { nameof(Confirmation.InputIcon), Icons.Material.Filled.Person }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Small,
                    BackdropClick = true,
                    FullWidth = true
                };

                var dialog = await _dialogService.ShowAsync<Confirmation>(title: null, parameters, options);
                var result = await dialog.Result;
                if (!result.Canceled)
                {
                    var response = await _userService.ChangeUserStatusAsync(new ChangeUserStatusRequest
                    {
                        UserId = user.Id,
                        Activation = true
                    });

                    if (response.IsSuccessful)
                    {
                        _snackbar.Add(response.Messages[0], Severity.Success);

                        await LoadUsers();
                    }
                    else
                    {
                        foreach (var message in response.Messages)
                        {
                            _snackbar.Add(message, Severity.Error);
                        }
                    }
                }
            }
        }

        private void Cancel()
        {
            _navigation.NavigateTo("/");
        }

        private void GoToRoles(string userId)
        {
            _navigation.NavigateTo($"/user-roles/{userId}");
        }
    }
}
