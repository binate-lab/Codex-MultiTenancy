using App.Infrastructure.Services.Orange;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Orange
{
    public partial class MarchandsOrange
    {
        [Inject] private ICompteMarchandOrangeService _marchandService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        private List<MarchandRow> _lignes = new();

        // Ligne d'ajout.
        private string _nouveauCode = string.Empty;
        private string _nouveauLibelle = string.Empty;
        private bool _enCours;

        // Secret affiché une seule fois (bannière) après création / rotation.
        private string _secretAffiche;
        private string _codeSecretAffiche;

        protected override async Task OnInitializedAsync() => await ChargerAsync();

        private async Task ChargerAsync()
        {
            var comptes = await _marchandService.GetAsync();
            _lignes = comptes.OrderBy(c => c.CodeMarchand).Select(c => new MarchandRow(c)).ToList();
        }

        private async Task AjouterAsync()
        {
            if (string.IsNullOrWhiteSpace(_nouveauCode))
            {
                _snackbar.Add("Saisis d'abord le code marchand.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                var result = await _marchandService.CreateAsync(_nouveauCode.Trim(),
                    string.IsNullOrWhiteSpace(_nouveauLibelle) ? null : _nouveauLibelle.Trim());
                if (result.IsSuccessful)
                {
                    AfficherSecret(_nouveauCode.Trim(), result.Secret);
                    _snackbar.Add($"Compte « {_nouveauCode.Trim()} » créé.", Severity.Success);
                    _nouveauCode = string.Empty;
                    _nouveauLibelle = string.Empty;
                    await ChargerAsync();
                }
                else _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        private async Task EnregistrerAsync(MarchandRow row)
        {
            if (!row.EstModifiee) return;
            if (string.IsNullOrWhiteSpace(row.CodeMarchand))
            {
                _snackbar.Add("Le code marchand est obligatoire.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                var result = await _marchandService.UpdateAsync(row.Id, row.CodeMarchand.Trim(),
                    string.IsNullOrWhiteSpace(row.Libelle) ? null : row.Libelle.Trim(), row.Actif);
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Compte « {row.CodeMarchand.Trim()} » enregistré.", Severity.Success);
                    row.FigerSnapshot();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                    row.Restaurer();
                }
            }
            finally { _enCours = false; }
        }

        private async Task RotationAsync(MarchandRow row)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Renouveler le secret",
                $"Générer un NOUVEAU secret pour « {row.CodeMarchand} » ? L'ancien cessera aussitôt de " +
                "fonctionner : Orange devra être reconfiguré avec le nouveau secret.",
                yesText: "Renouveler", cancelText: "Annuler");
            if (confirm != true) return;

            _enCours = true;
            try
            {
                var result = await _marchandService.RotationSecretAsync(row.Id);
                if (result.IsSuccessful)
                {
                    AfficherSecret(row.CodeMarchand, result.Secret);
                    _snackbar.Add("Secret renouvelé.", Severity.Success);
                    await ChargerAsync();
                }
                else _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        private async Task SupprimerAsync(MarchandRow row)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Supprimer le compte marchand",
                $"Supprimer définitivement le compte « {row.CodeMarchand} » ? Orange ne pourra plus " +
                "notifier de paiement via ce code.",
                yesText: "Supprimer", cancelText: "Annuler");
            if (confirm != true) return;

            _enCours = true;
            try
            {
                var result = await _marchandService.DeleteAsync(row.Id);
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Compte « {row.CodeMarchand} » supprimé.", Severity.Info);
                    await ChargerAsync();
                }
                else _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        private void AfficherSecret(string code, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return;
            _codeSecretAffiche = code;
            _secretAffiche = secret;
        }

        private async Task CopierSecretAsync()
        {
            if (string.IsNullOrEmpty(_secretAffiche)) return;
            try
            {
                await _js.InvokeVoidAsync("navigator.clipboard.writeText", _secretAffiche);
                _snackbar.Add("Secret copié dans le presse-papier.", Severity.Success);
            }
            catch
            {
                _snackbar.Add("Copie automatique impossible : sélectionne et copie le secret à la main.", Severity.Warning);
            }
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ------------------- ViewModel mutable de la grille -------------------
        public sealed class MarchandRow
        {
            public MarchandRow(CompteMarchandItem m)
            {
                Id = m.Id;
                CodeMarchand = m.CodeMarchand;
                Libelle = m.Libelle;
                Actif = m.Actif;
                SecretApercu = m.SecretApercu;
                FigerSnapshot();
            }

            public int Id { get; }
            public string CodeMarchand { get; set; }
            public string Libelle { get; set; }
            public bool Actif { get; set; }
            public string SecretApercu { get; }

            private (string CodeMarchand, string Libelle, bool Actif) _snapshot;
            public bool EstModifiee => (CodeMarchand, Libelle, Actif) != _snapshot;
            public void FigerSnapshot() => _snapshot = (CodeMarchand, Libelle, Actif);
            public void Restaurer() => (CodeMarchand, Libelle, Actif) = _snapshot;
        }
    }
}
