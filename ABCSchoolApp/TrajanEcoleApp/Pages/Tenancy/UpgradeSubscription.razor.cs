using TrajanEcole.Shared.Library.Models.Requests.Tenancy;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Tenancy
{
    public partial class UpgradeSubscription
    {
        [CascadingParameter] private IMudDialogInstance _dialogInstance { get; set; }
        [Parameter]
        public UpdateTenantSubscriptionRequest SubscriptionRequest { get; set; }

        private MudForm _form;
        private DateTime? NewExpiryDatePicker
        {
            get => SubscriptionRequest.NewExpiryDate == default
                ? null
                : SubscriptionRequest.NewExpiryDate;
            set
            {
                if (value.HasValue)
                {
                    SubscriptionRequest.NewExpiryDate = value.Value;
                }
            }
        }

        private async Task UpgradeSubcriptionAsync()
        {
            var result = await _tenantService.UpgradeSubscriptionAsync(SubscriptionRequest);

            if (result.IsSuccessful)
            {
                _snackbar.Add("Mise à jour souscription de l'organisation effectuée avec succès", Severity.Success);
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
    }
}
