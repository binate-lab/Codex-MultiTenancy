using TrajanEcoleApp.Components;
using MudBlazor;

namespace TrajanEcoleApp.Layout
{
    public partial class MainLayout
    {
        private bool _drawerOpen = true;
        protected override void OnInitialized()
        {
            _interceptor.RegisterEvent();
            StateHasChanged();
        }

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }

        private async Task LogoutDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(Logout.Title),  "Quitter"},
                { nameof(Logout.ConfirmationMessage),  "Etes vous sûr de vouloir quitter cette application?"},
                { nameof(Logout.ButtonText),  "Se déconnecter"},
                { nameof(Logout.Color),  Color.Success}
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                BackdropClick = true,
            };

            await _dialogService.ShowAsync<Logout>(title: null, parameters: parameters, options: options);
        }
    }
}
