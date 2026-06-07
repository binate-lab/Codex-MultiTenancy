using TrajanEcoleApp.Components;
using TrajanEcoleApp.Pages.Tenancy;
using TrajanEcole.Shared.Library.Constants;
using TrajanEcole.Shared.Library.Models.Responses.Schools;
using App.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Schools
{
    public partial class SchoolList
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthState { get; set; } = default!;

        [Inject]
        protected IAuthorizationService AuthService { get; set; } = default!;

        private bool _isLoading = true;

        private bool _canCreateSchools;
        private bool _canUpdateSchools;
        private bool _canDeleteSchools;

        private List<SchoolResponse> _schoolList = [];

        protected override async Task OnInitializedAsync()
        {
            var user = (await AuthState).User;

            _canCreateSchools = await AuthService.HasPermissionAsync(user, SchoolFeature.Schools, SchoolAction.Create);
            _canUpdateSchools = await AuthService.HasPermissionAsync(user, SchoolFeature.Schools, SchoolAction.Update);
            _canDeleteSchools = await AuthService.HasPermissionAsync(user, SchoolFeature.Schools, SchoolAction.Delete);

            // Load Schools
            await LoadSchoolsAsync();
            _isLoading = false;
        }

        private async Task LoadSchoolsAsync()
        {
            var result = await _schoolService.GetAllAsync();
            if (result.IsSuccessful)
            {
                _schoolList = result.Data;
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task OnBoardNewSchoolAsync()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await _dialogService.ShowAsync<CreateSchool>(title: null, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadSchoolsAsync();
            }
        }

        private async Task UpdateSchoolAsync(SchoolResponse school)
        {
            var parameters = new DialogParameters
            {
                { nameof(UpdateSchool.School), school }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await _dialogService.ShowAsync<UpdateSchool>("Modifier l'école", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadSchoolsAsync();
            }
        }

        private async Task DeleteSchoolAsync(SchoolResponse school)
        {
            var parameters = new DialogParameters
            {
                { nameof(Confirmation.Title), "Suppression école" },
                { nameof(Confirmation.Message), $"Êtes-vous sûr de vouloir supprimer l'école : {school.Name} ?" },
                { nameof(Confirmation.ButtonText), "Supprimer" },
                { nameof(Confirmation.Color), Color.Error },
                { nameof(Confirmation.InputIcon), Icons.Material.Filled.DeleteForever }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                BackdropClick = true,
                FullWidth = true
            };

            var dialog = await _dialogService.ShowAsync<Confirmation>(title: null, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await _schoolService.DeleteAsync(school.Id.ToString());
                if (response.IsSuccessful)
                {
                    _snackbar.Add("École supprimée avec succès.", Severity.Success);
                    await LoadSchoolsAsync();
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

        private void Cancel()
        {
            _navigation.NavigateTo("/");
        }
    }
}
