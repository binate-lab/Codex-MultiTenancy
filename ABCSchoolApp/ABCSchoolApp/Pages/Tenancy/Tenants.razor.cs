using ABCSchoolApp.Components;
using ABCShared.Library.Models.Requests.Tenancy;
using ABCShared.Library.Models.Responses.Tenancy;
using MudBlazor;

namespace ABCSchoolApp.Pages.Tenancy
{
    public partial class Tenants
    {
        private List<TenantResponse> TenantList { get; set; } = [];
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadTenantsAsync();
            _isLoading = false;
        }

        private async Task LoadTenantsAsync()
        {
            var result = await _tenantService.GetAllAsync();
            if (result.IsSuccessful)
            {
                TenantList = result.Data;
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private void ReturnClicked()
        {
            _navigation.NavigateTo("/");
        }

        private async Task OnBoardNewTenantAsync()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await _dialogService.ShowAsync<CreateTenant>("Créer un nouvel Ets", options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadTenantsAsync();
            }
        }

        private async Task UpgradeSubscriptionAsync(TenantResponse tenant)
        {
            var parameters = new DialogParameters
            {
                { 
                    nameof(UpgradeSubscription.SubscriptionRequest),
                    new UpdateTenantSubscriptionRequest
                    {
                        TenantIdentifier = tenant.Identifier,
                        NewExpiryDate = tenant.ValidUpTo,
                    }
                }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await _dialogService
                .ShowAsync<UpgradeSubscription>("Upgrader Souscription Ets", parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadTenantsAsync();
            }
        }

        private async Task DeleteTenantAsync(TenantResponse tenant)
        {
            var parameters = new DialogParameters
            {
                { nameof(Confirmation.Title), "Suppression Ets" },
                { nameof(Confirmation.Message), $"Etes vous sûr de vouloir supprimer définitivement l'Ets: {tenant.Name}? Cette action est irréversible." },
                { nameof(Confirmation.ButtonText), "Supprimer" },
                { nameof(Confirmation.Color), Color.Error },
                { nameof(Confirmation.InputIcon), Icons.Material.Filled.DeleteForever }
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
                var response = await _tenantService.DeleteAsync(tenant.Identifier);

                if (response.IsSuccessful)
                {
                    _snackbar.Add(response.Messages[0], Severity.Success);
                    await LoadTenantsAsync();
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

        private async Task ActivateOrDeactivativeAsync(TenantResponse tenant)
        {
            if (tenant.IsActive)
            {
                // Deactivate
                var parameters = new DialogParameters
                {
                    { nameof(Confirmation.Title), "Désactivation Ets" },
                    { nameof(Confirmation.Message), $"Etes vous sûr de vouloir désactiver l'Ets: {tenant.Name}?" },
                    { nameof(Confirmation.ButtonText), "Désactivation" },
                    {nameof(Confirmation.Color), Color.Error },
                    {nameof(Confirmation.InputIcon), Icons.Material.Filled.CloudOff }
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
                    var response = await _tenantService.DeActivateAsync(tenant.Identifier);

                    if (response.IsSuccessful)
                    {
                        _snackbar.Add(response.Messages[0], Severity.Success);

                        await LoadTenantsAsync();
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
                    { nameof(Confirmation.Title), "Activation Ets" },
                    { nameof(Confirmation.Message), $"Etes vous sûr de vouloir activer l'Ets: {tenant.Name}?" },
                    { nameof(Confirmation.ButtonText), "Activation" },
                    {nameof(Confirmation.Color), Color.Primary },
                    {nameof(Confirmation.InputIcon), Icons.Material.Filled.Cloud }
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
                    var response = await _tenantService.ActivateAsync(tenant.Identifier);

                    if (response.IsSuccessful)
                    {
                        _snackbar.Add(response.Messages[0], Severity.Success);

                        await LoadTenantsAsync();
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
    }
}
