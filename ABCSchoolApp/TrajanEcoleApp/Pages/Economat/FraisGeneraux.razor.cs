using System.Globalization;
using App.Infrastructure.Services.Economat;
using TrajanEcoleApp.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Economat
{
    public partial class FraisGeneraux
    {
        [Inject] private IFraisGeneralService _fgService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        private static readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");

        private List<FgRow> _lignes = new();

        // Champs de la ligne d'ajout.
        private string _nouveauLibelle = string.Empty;
        private decimal _nouveauMontant;
        private bool _enCours;

        protected override async Task OnInitializedAsync() => await ChargerAsync();

        private async Task ChargerAsync()
        {
            var postes = await _fgService.GetPostesAsync();
            _lignes = postes.OrderBy(p => p.Ordre).ThenBy(p => p.Libelle)
                            .Select(p => new FgRow(p)).ToList();
        }

        private bool Verifier(FraisGeneralOpResult result, string messageSucces)
        {
            if (result.IsSuccessful)
            {
                _snackbar.Add(messageSucces, Severity.Success);
                return true;
            }
            _snackbar.Add(result.Error, Severity.Error);
            return false;
        }

        private async Task AjouterAsync()
        {
            if (string.IsNullOrWhiteSpace(_nouveauLibelle))
            {
                _snackbar.Add("Saisis d'abord le libellé du poste.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                // Ordre = null -> le backend le place en fin de liste ; visible par defaut.
                var result = await _fgService.CreateAsync(_nouveauLibelle.Trim(), _nouveauMontant, null, ok: true);
                if (Verifier(result, $"Poste « {_nouveauLibelle.Trim()} » ajouté."))
                {
                    _nouveauLibelle = string.Empty;
                    _nouveauMontant = 0;
                    await ChargerAsync();
                }
            }
            finally { _enCours = false; }
        }

        private async Task EnregistrerAsync(FgRow row)
        {
            if (!row.EstModifiee) return;

            _enCours = true;
            try
            {
                var result = await _fgService.UpdateAsync(row.VersItem());
                if (Verifier(result, $"Poste « {row.Libelle} » enregistré."))
                    row.FigerSnapshot();
                else
                    row.Restaurer();
            }
            finally { _enCours = false; }
        }

        private async Task SupprimerAsync(FgRow row)
        {
            _enCours = true;
            try
            {
                var result = await _fgService.DeleteAsync(row.Id);
                if (Verifier(result, $"Poste « {row.Libelle} » supprimé."))
                    await ChargerAsync();
            }
            finally { _enCours = false; }
        }

        // Ajoute les lignes FG manquantes aux eleves qui ont deja un echeancier.
        private async Task AppliquerAuxExistantsAsync()
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmerAction.Titre), "Appliquer aux élèves existants" },
                { nameof(ConfirmerAction.Message),
                    "Ajouter les postes de Frais Généraux manquants à tous les élèves déjà inscrits ?" },
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, BackdropClick = false };
            var dialog = await _dialogService.ShowAsync<ConfirmerAction>(null, parameters, options);
            var confirmation = await dialog.Result;
            if (confirmation is null || confirmation.Canceled) return;

            _enCours = true;
            try
            {
                var res = await _fgService.AppliquerAuxExistantsAsync();
                if (res is null)
                {
                    _snackbar.Add("Opération indisponible.", Severity.Error);
                    return;
                }

                if (res.Lignes == 0)
                    _snackbar.Add("Aucune ligne à ajouter (postes déjà présents ou aucun montant configuré).", Severity.Info);
                else
                    _snackbar.Add($"{res.Lignes} ligne(s) ajoutée(s) sur {res.Eleves} élève(s).", Severity.Success);
            }
            finally { _enCours = false; }
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ------------------- ViewModel mutable de la grille -------------------
        public sealed class FgRow
        {
            public FgRow(FraisGeneralItem p)
            {
                Id = p.Id; Ordre = p.Ordre; Libelle = p.Libelle; Montant = p.Montant; OK = p.OK;
                FigerSnapshot();
            }

            public int Id { get; }
            public int Ordre { get; set; }
            public string Libelle { get; set; }
            public decimal Montant { get; set; }
            public bool OK { get; set; }

            private (int Ordre, string Libelle, decimal Montant, bool OK) _snapshot;
            public bool EstModifiee => (Ordre, Libelle, Montant, OK) != _snapshot;
            public void FigerSnapshot() => _snapshot = (Ordre, Libelle, Montant, OK);
            public void Restaurer() => (Ordre, Libelle, Montant, OK) = _snapshot;

            public FraisGeneralItem VersItem() => new()
            {
                Id = Id, Ordre = Ordre, Libelle = Libelle, Montant = Montant, OK = OK,
            };
        }
    }
}
