using TrajanEcoleApp.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace TrajanEcoleApp.Layout
{
    // Mise en page « espace école » : même barre Trajan, mais le tiroir gauche
    // affiche le menu de gestion de l'école (SchoolNavMenu) au lieu du menu locataire.
    public partial class SchoolLayout : IDisposable
    {
        private bool _drawerOpen = true;

        // Vrai uniquement sur la page principale de l'espace école (/ecole) : sert à
        // n'afficher la barre d'outils horizontale que là (cf. SchoolLayout.razor).
        private bool IsSchoolHome =>
            string.Equals(
                _navigation.ToBaseRelativePath(_navigation.Uri).Split('?', '#')[0].TrimEnd('/'),
                "ecole",
                StringComparison.OrdinalIgnoreCase);

        protected override void OnInitialized()
        {
            _interceptor.RegisterEvent();
            // Re-rendre le layout à chaque navigation pour réévaluer IsSchoolHome.
            _navigation.LocationChanged += OnLocationChanged;
            StateHasChanged();
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e) => StateHasChanged();

        public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }

        private async Task LogoutDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(Logout.Title),  "Quitter"},
                { nameof(Logout.ConfirmationMessage),  "Voulez-vous quitter Trajan ?"},
                { nameof(Logout.ButtonText),  "OUI"},
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
