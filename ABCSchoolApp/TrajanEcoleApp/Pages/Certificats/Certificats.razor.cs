using App.Infrastructure.Services.Certificats;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using TrajanEcoleApp.Components;

namespace TrajanEcoleApp.Pages.Certificats
{
    public partial class Certificats
    {
        // ── Onglet 1 ──────────────────────────────────────────────────
        private MudForm _form;
        private bool _formIsValid;
        private bool _isSubmitting;
        private string _jsonSoumission;
        private SoumettreDemandeRequest _request = new();

        // ── Onglet 2 ──────────────────────────────────────────────────
        private List<DemandeResponse> _mesDemandes = [];
        private bool _isLoadingDemandes;

        // ── Onglet 3 ──────────────────────────────────────────────────
        private List<DemandeResponse> _demandesPendantes = [];
        private bool _isLoadingPendantes;

        // ── Onglet 4 ──────────────────────────────────────────────────
        private List<CertificatResponse> _mesAppareils = [];
        private bool _isLoadingAppareils;

        // ── Navigation ────────────────────────────────────────────────
        private void ReturnClicked() => _navigation.NavigateTo("/");

        private async Task OnTabChangedAsync(int index)
        {
            _activeTab = index;
            if (index == 1) await ChargerMesDemandesAsync();
            else if (index == 2) await ChargerDemandesPendantesAsync();
            else if (index == 3) await ChargerMesAppareilsAsync();
        }

        // ── Onglet 1 : soumettre ──────────────────────────────────────
        private async Task SoumettreDemandeAsync()
        {
            await _form.Validate();
            if (!_formIsValid) return;

            _isSubmitting = true;
            _jsonSoumission = null;

            var response = await _certificatService.SoumettreDemandeAsync(_request);

            _jsonSoumission = JsonSerializer.Serialize(new
            {
                data = response.Data,
                messages = response.Messages,
                isSuccessful = response.IsSuccessful
            }, new JsonSerializerOptions { WriteIndented = true });

            if (response.IsSuccessful)
            {
                _request = new SoumettreDemandeRequest();
                await _form.ResetAsync();
                _snackbar.Add("Demande soumise avec succès.", Severity.Success);
            }
            else
            {
                foreach (var msg in response.Messages)
                    _snackbar.Add(msg, Severity.Error);
            }

            _isSubmitting = false;
        }

        // ── Onglet 2 & 3 : chargement demandes ───────────────────────
        private async Task ChargerMesDemandesAsync()
        {
            _isLoadingDemandes = true;
            var response = await _certificatService.GetMesDemandesAsync();
            if (response.IsSuccessful) _mesDemandes = response.Data ?? [];
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
            _isLoadingDemandes = false;
        }

        private async Task ChargerDemandesPendantesAsync()
        {
            _isLoadingPendantes = true;
            var response = await _certificatService.GetDemandesPendantesAsync();
            if (response.IsSuccessful) _demandesPendantes = response.Data ?? [];
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
            _isLoadingPendantes = false;
        }

        // ── Onglet 4 : appareils ──────────────────────────────────────
        private async Task ChargerMesAppareilsAsync()
        {
            _isLoadingAppareils = true;
            var response = await _certificatService.GetMesAppareilsAsync();
            if (response.IsSuccessful) _mesAppareils = response.Data ?? [];
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
            _isLoadingAppareils = false;
        }

        // ── Actions ───────────────────────────────────────────────────
        private async Task ApprouverDemandeAsync(DemandeResponse demande)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Approuver la demande",
                $"Approuver l'appareil « {demande.NomAppareil} » ? Un certificat sera généré.",
                "Approuver", cancelText: "Annuler");

            if (confirm != true) return;

            var response = await _certificatService.ApprouverDemandeAsync(demande.Id);

            if (response.IsSuccessful)
            {
                _snackbar.Add($"Certificat généré pour {demande.NomAppareil}.", Severity.Success);
                await RefreshTab();
            }
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
        }

        private async Task RejeterDemandeAsync(DemandeResponse demande)
        {
            var parameters = new DialogParameters<PromptDialog>
            {
                { d => d.Title, "Rejeter la demande" },
                { d => d.Label, "Raison du rejet" },
                { d => d.ButtonText, "Rejeter" },
                { d => d.Color, Color.Error },
                { d => d.InputIcon, Icons.Material.Filled.Cancel }
            };

            var dialog = await _dialogService.ShowAsync<PromptDialog>("Rejeter", parameters);
            var result = await dialog.Result;
            if (result.Canceled) return;

            var response = await _certificatService.RejeterDemandeAsync(demande.Id, result.Data?.ToString());

            if (response.IsSuccessful)
            {
                _snackbar.Add("Demande rejetée.", Severity.Warning);
                await RefreshTab();
            }
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
        }

        private async Task SupprimerDemandeAsync(DemandeResponse demande)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Supprimer la demande",
                $"Supprimer définitivement la demande « {demande.NomAppareil} » ?",
                "Supprimer", cancelText: "Annuler");

            if (confirm != true) return;

            var response = await _certificatService.SupprimerDemandeAsync(demande.Id);

            if (response.IsSuccessful)
            {
                _snackbar.Add("Demande supprimée.", Severity.Info);
                await RefreshTab();
            }
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
        }

        private async Task ReactiverCertificatAsync(Guid certificatId)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Réactiver l'appareil",
                "Réactiver ce certificat ? L'appareil retrouvera l'accès à l'application.",
                "Réactiver", cancelText: "Annuler");

            if (confirm != true) return;

            var response = await _certificatService.ReactiverCertificatAsync(certificatId);

            if (response.IsSuccessful)
            {
                _snackbar.Add("Certificat réactivé.", Severity.Success);
                await RefreshTab();
            }
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
        }

        private async Task RevoquerCertificatAsync(Guid certificatId)
        {
            var parameters = new DialogParameters<PromptDialog>
            {
                { d => d.Title, "Révoquer le certificat" },
                { d => d.Label, "Raison de la révocation" },
                { d => d.ButtonText, "Révoquer" },
                { d => d.Color, Color.Error },
                { d => d.InputIcon, Icons.Material.Filled.Block }
            };

            var dialog = await _dialogService.ShowAsync<PromptDialog>("Révoquer", parameters);
            var result = await dialog.Result;
            if (result.Canceled) return;

            var response = await _certificatService.RevoquerCertificatAsync(certificatId, result.Data?.ToString());

            if (response.IsSuccessful)
            {
                _snackbar.Add("Certificat révoqué.", Severity.Warning);
                await RefreshTab();
            }
            else foreach (var msg in response.Messages) _snackbar.Add(msg, Severity.Error);
        }

        // ── Refresh selon l'onglet actif ──────────────────────────────
        private int _activeTab;

        private async Task RefreshTab()
        {
            if (_activeTab == 1) await ChargerMesDemandesAsync();
            else if (_activeTab == 2) await ChargerDemandesPendantesAsync();
            else if (_activeTab == 3) await ChargerMesAppareilsAsync();
        }

        // ── Couleurs ──────────────────────────────────────────────────
        private static Color StatutDemandeColor(int statut) => statut switch
        {
            1 => Color.Warning,
            2 => Color.Info,
            3 => Color.Error,
            4 => Color.Success,
            _ => Color.Default
        };

        private static Color StatutCertificatColor(int statut) => statut switch
        {
            1 => Color.Success,
            2 => Color.Error,
            3 => Color.Warning,
            _ => Color.Default
        };
    }
}
