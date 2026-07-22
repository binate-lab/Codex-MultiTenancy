using App.Infrastructure.Services.Fne;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Fne
{
    // Page /fne : paramètres DGI de l'école (onglet 1) + suivi des certifications (onglet 2).
    public partial class FactureNormalisee
    {
        [Inject] private IFneService _fneService { get; set; } = default!;

        // Onglet Paramètres.
        private ParametreFneDto _parametres = new();
        private bool _parametresCharges;

        // Onglet Suivi.
        private List<FactureFneItem> _factures = new();
        private string _filtreStatut;

        private bool _enCours;

        protected override async Task OnInitializedAsync()
        {
            _parametres = await _fneService.GetParametresAsync() ?? new ParametreFneDto();
            _parametresCharges = true;
            await ChargerFacturesAsync();
        }

        private async Task EnregistrerParametresAsync()
        {
            if (string.IsNullOrWhiteSpace(_parametres.Ncc))
            {
                _snackbar.Add("Le NCC de l'école est obligatoire.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                var result = await _fneService.SaveParametresAsync(_parametres);
                if (result.IsSuccessful)
                    _snackbar.Add("Paramètres FNE enregistrés.", Severity.Success);
                else
                    _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        private async Task ChargerFacturesAsync()
        {
            _enCours = true;
            try
            {
                _factures = (await _fneService.GetFacturesAsync(_filtreStatut)).ToList();
            }
            finally { _enCours = false; }
        }

        private async Task RelancerAsync(FactureFneItem facture)
        {
            _enCours = true;
            try
            {
                var result = await _fneService.RelancerAsync(facture.Id);
                if (result.IsSuccessful)
                {
                    _snackbar.Add("Facture remise en file de certification.", Severity.Success);
                    await ChargerFacturesAsync();
                }
                else _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        private static Color CouleurStatut(string statut) => statut switch
        {
            "Certifiee" => Color.Success,
            "Echec" => Color.Error,
            _ => Color.Warning
        };

        private static string LibelleStatut(string statut) => statut switch
        {
            "Certifiee" => "Certifiée",
            "Echec" => "Échec",
            "EnAttente" => "En attente",
            _ => statut
        };

        private static string Tronquer(string s)
            => s.Length <= 60 ? s : s[..60] + "…";

        private void Fermer() => _navigation.NavigateTo("/ecole");
    }
}
