using App.Infrastructure.Services.Economat;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Economat
{
    public partial class NaturesVersement
    {
        [Inject] private INatureVersementService _natureService { get; set; } = default!;

        private List<NatureRow> _lignes = new();

        // Champ de la ligne d'ajout.
        private string _nouveauLibelle = string.Empty;
        private bool _enCours;

        protected override async Task OnInitializedAsync() => await ChargerAsync();

        private async Task ChargerAsync()
        {
            var natures = await _natureService.GetNaturesAsync();
            _lignes = natures.OrderBy(n => n.Ordre).ThenBy(n => n.Libelle)
                             .Select(n => new NatureRow(n)).ToList();
        }

        private bool Verifier(NatureOpResult result, string messageSucces)
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
                _snackbar.Add("Saisis d'abord le libellé de la nature.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                // Ordre = null -> le backend le place en fin de liste ; OK par defaut, pas inscription.
                var result = await _natureService.CreateAsync(_nouveauLibelle.Trim(), null, ok: true, estInscription: false);
                if (Verifier(result, $"Nature « {_nouveauLibelle.Trim()} » ajoutée."))
                {
                    _nouveauLibelle = string.Empty;
                    await ChargerAsync();
                }
            }
            finally { _enCours = false; }
        }

        // Enregistre une ligne modifiee. Si elle devient LA nature d'inscription, on recharge
        // pour refleter le decochage des autres (une seule inscription par ecole, garde backend).
        private async Task EnregistrerAsync(NatureRow row)
        {
            if (!row.EstModifiee) return;

            _enCours = true;
            try
            {
                var result = await _natureService.UpdateAsync(row.VersItem());
                if (Verifier(result, $"Nature « {row.Libelle} » enregistrée."))
                {
                    if (row.EstInscription) await ChargerAsync();
                    else row.FigerSnapshot();
                }
                else
                {
                    row.Restaurer();
                }
            }
            finally { _enCours = false; }
        }

        private async Task SupprimerAsync(NatureRow row)
        {
            _enCours = true;
            try
            {
                var result = await _natureService.DeleteAsync(row.Id);
                if (Verifier(result, $"Nature « {row.Libelle} » supprimée."))
                {
                    await ChargerAsync();
                }
            }
            finally { _enCours = false; }
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ------------------- ViewModel mutable de la grille -------------------
        public sealed class NatureRow
        {
            public NatureRow(NatureVersementItem n)
            {
                Id = n.Id; Ordre = n.Ordre; Libelle = n.Libelle; OK = n.OK; EstInscription = n.EstInscription;
                FigerSnapshot();
            }

            public int Id { get; }
            public int Ordre { get; set; }
            public string Libelle { get; set; }
            public bool OK { get; set; }
            public bool EstInscription { get; set; }

            private (int Ordre, string Libelle, bool OK, bool EstInscription) _snapshot;
            public bool EstModifiee => (Ordre, Libelle, OK, EstInscription) != _snapshot;
            public void FigerSnapshot() => _snapshot = (Ordre, Libelle, OK, EstInscription);
            public void Restaurer()
                => (Ordre, Libelle, OK, EstInscription) = _snapshot;

            public NatureVersementItem VersItem() => new()
            {
                Id = Id, Ordre = Ordre, Libelle = Libelle, OK = OK, EstInscription = EstInscription,
            };
        }
    }
}
