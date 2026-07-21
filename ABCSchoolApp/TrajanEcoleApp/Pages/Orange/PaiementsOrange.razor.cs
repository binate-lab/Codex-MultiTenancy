using System.Globalization;
using App.Infrastructure.Services.Orange;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TrajanEcoleApp.Components;

namespace TrajanEcoleApp.Pages.Orange
{
    public partial class PaiementsOrange
    {
        [Inject] private IPaiementOrangeService _paiementService { get; set; } = default!;

        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        private List<PaiementOrangeItem> _lignes = new();
        private string _statut = "EnAttente";      // filtre par défaut : les paiements à traiter
        private bool _enCours;

        protected override async Task OnInitializedAsync() => await ChargerAsync();

        private async Task ChargerAsync()
        {
            _enCours = true;
            try
            {
                var data = await _paiementService.GetAsync(string.IsNullOrEmpty(_statut) ? null : _statut);
                _lignes = data.ToList();
            }
            finally { _enCours = false; }
        }

        // Valider AUTO (A) : impute le versement lié sur l'échéancier + notifie le parent.
        private async Task ValiderAsync(PaiementOrangeItem p)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Valider automatiquement (A)",
                $"Valider le paiement de {Fmt(p.Montant)}F (réf {p.Reference}) ? Il sera imputé " +
                "automatiquement sur l'échéancier de l'élève.",
                yesText: "Valider Auto", cancelText: "Annuler");
            if (confirm != true) return;

            await ExecuterAsync(() => _paiementService.ValiderAsync(p.Id), "Paiement validé et imputé (A).");
        }

        // Valider MANU (M) : le montant couvre plusieurs enfants (fratrie payée avec un seul
        // matricule). Le brouillon est retiré ; le caissier répartit lui-même dans /scolarites
        // en réutilisant la référence Orange (autorisée entre versements de même CodeParent).
        private async Task ValiderManuAsync(PaiementOrangeItem p)
        {
            var confirm = await _dialogService.ShowMessageBox(
                "Valider manuellement (M)",
                $"Marquer le paiement de {Fmt(p.Montant)}F (réf {p.Reference}) « à répartir » ? " +
                "Rien ne sera imputé automatiquement : vous saisirez vous-même les versements des " +
                $"enfants concernés dans Versements, avec la même référence {p.Reference}.",
                yesText: "Valider Manu", cancelText: "Annuler");
            if (confirm != true) return;

            await ExecuterAsync(() => _paiementService.ValiderManuAsync(p.Id),
                $"Paiement marqué M : à répartir dans Versements (réf {p.Reference}).");
        }

        // Rattache un orphelin : l'agent saisit le matricule correct de l'élève.
        private async Task RattacherAsync(PaiementOrangeItem p)
        {
            var parameters = new DialogParameters<PromptDialog>
            {
                { d => d.Title, "Rattacher à un élève" },
                { d => d.Label, "Matricule correct de l'élève" },
                { d => d.ButtonText, "Rattacher" },
                { d => d.Color, Color.Primary },
                { d => d.InputIcon, Icons.Material.Filled.PersonSearch },
            };

            var dialog = await _dialogService.ShowAsync<PromptDialog>("Rattacher", parameters);
            var result = await dialog.Result;
            if (result.Canceled) return;

            var matricule = result.Data?.ToString();
            if (string.IsNullOrWhiteSpace(matricule)) return;

            await ExecuterAsync(() => _paiementService.RattacherAsync(p.Id, matricule.Trim()),
                "Paiement rattaché ; il est maintenant en attente de validation.");
        }

        // Rejette un paiement (une raison est demandée pour la traçabilité).
        private async Task RejeterAsync(PaiementOrangeItem p)
        {
            var parameters = new DialogParameters<PromptDialog>
            {
                { d => d.Title, "Rejeter le paiement" },
                { d => d.Label, "Motif du rejet" },
                { d => d.ButtonText, "Rejeter" },
                { d => d.Color, Color.Error },
                { d => d.InputIcon, Icons.Material.Filled.Cancel },
            };

            var dialog = await _dialogService.ShowAsync<PromptDialog>("Rejeter", parameters);
            var result = await dialog.Result;
            if (result.Canceled) return;

            await ExecuterAsync(() => _paiementService.RejeterAsync(p.Id, result.Data?.ToString()),
                "Paiement rejeté.");
        }

        private async Task ExecuterAsync(Func<Task<PaiementOpResult>> action, string messageSucces)
        {
            _enCours = true;
            try
            {
                var result = await action();
                if (result.IsSuccessful)
                {
                    _snackbar.Add(messageSucces, Severity.Success);
                    await ChargerAsync();
                }
                else _snackbar.Add(result.Error, Severity.Error);
            }
            finally { _enCours = false; }
        }

        // Le nom déclaré par Orange diffère-t-il de l'élève réellement rattaché ?
        // (signal d'alerte : typo, homonyme, ou rapprochement à revérifier).
        private static bool NomDeclareDiffere(PaiementOrangeItem p)
        {
            if (string.IsNullOrWhiteSpace(p.EleveNomComplet)) return false;
            var declare = $"{p.Nom} {p.Prenoms}".Trim();
            return !string.Equals(declare, p.EleveNomComplet.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        private static string Fmt(decimal montant) => montant.ToString("#,0", Fr);

        private static string LibelleStatut(string statut) => statut switch
        {
            "EnAttente" => "En attente",
            "Orphelin" => "Orphelin",
            "Valide" => "Validé",
            "Rejete" => "Rejeté",
            _ => statut,
        };

        // Libellé du lien payeur (Pere/Mere/Tuteur/Inconnu → affichage accentué).
        private static string LibelleLien(string lien) => lien switch
        {
            "Pere" => "Père",
            "Mere" => "Mère",
            "Tuteur" => "Tuteur",
            _ => "Inc.",
        };

        private static Color CouleurLien(string lien) => lien switch
        {
            "Pere" or "Mere" or "Tuteur" => Color.Success,
            _ => Color.Default,
        };

        private static Color CouleurStatut(string statut) => statut switch
        {
            "EnAttente" => Color.Info,
            "Orphelin" => Color.Warning,
            "Valide" => Color.Success,
            "Rejete" => Color.Error,
            _ => Color.Default,
        };
    }
}
