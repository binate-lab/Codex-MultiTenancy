using System.Globalization;
using App.Infrastructure.Services.Economat;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Economat
{
    public partial class Echeancier
    {
        [Inject] private IEcheancierService _echeancierService { get; set; }
        [Inject] private IStructureService _structureService { get; set; }

        // Année scolaire en cours : le barème se gère pour cette année.
        private string _annee = "—";

        // Niveaux du référentiel structures (ordre pédagogique : cycle puis ordre du niveau),
        // pour le sélecteur d'ajout et le tri de la grille.
        private List<NiveauItem> _niveauxRef = new();

        private List<EcheancierRow> _lignes = new();

        // Champs de la ligne d'ajout.
        private string _nouveauNiveauCode;
        private string _nouveauStatut = "Naff";

        // Filtres de la grille (null/vide = tous).
        private string _filtreNiveau;
        private string _filtreStatut;

        private IEnumerable<EcheancierRow> LignesFiltrees => _lignes
            .Where(l => string.IsNullOrEmpty(_filtreNiveau) || l.NiveauCode == _filtreNiveau)
            .Where(l => string.IsNullOrEmpty(_filtreStatut) || l.Statut == _filtreStatut);

        // Format monétaire façon Access : « 110 000 F ».
        private static readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");
        private static string FormaterMontant(decimal montant) => $"{montant.ToString("N0", _fr)} F";

        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            var cycles = await _structureService.GetCyclesAsync();
            _niveauxRef = cycles.SelectMany(c => c.Niveaux).ToList();
            if (_niveauxRef.Count == 0)
            {
                _snackbar.Add(
                    "Aucune structure pédagogique configurée pour cette école — menu Structure → Pédagogie → Cycles, niveaux & classes.",
                    Severity.Warning);
            }

            await ChargerAsync();
        }

        private async Task ChargerAsync()
        {
            var lignes = await _echeancierService.GetModalitesAsync(_annee == "—" ? null : _annee);

            // Tri : ordre pédagogique du référentiel (6e avant Tle), puis Naff avant Aff.
            var ordreNiveaux = _niveauxRef.Select((n, i) => (n.Code, i)).ToDictionary(x => x.Code, x => x.i);
            _lignes = lignes
                .OrderBy(l => ordreNiveaux.TryGetValue(l.NiveauCode, out var i) ? i : int.MaxValue)
                .ThenByDescending(l => l.Statut) // Naff avant Aff
                .Select(l => new EcheancierRow(l))
                .ToList();
        }

        private bool Verifier(EcheancierOpResult result, string messageSucces)
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
            if (string.IsNullOrWhiteSpace(_nouveauNiveauCode))
            {
                _snackbar.Add("Choisissez d'abord le niveau.", Severity.Warning);
                return;
            }

            var result = await _echeancierService.CreateModaliteAsync(_annee, _nouveauNiveauCode, _nouveauStatut);
            if (Verifier(result, $"Barème « {_nouveauNiveauCode} / {_nouveauStatut} » ajouté (montants à 0)."))
            {
                await ChargerAsync();
            }
        }

        // Enregistrement direct façon Access : la cellule part en base à la sortie du champ,
        // seulement si un montant a réellement changé ; restauration si refus.
        private async Task EnregistrerSiModifieeAsync(EcheancierRow row)
        {
            if (!row.EstModifiee)
            {
                return;
            }

            var result = await _echeancierService.UpdateMontantsAsync(row.VersItem());
            if (Verifier(result, $"Barème « {row.NiveauCode} / {row.Statut} » enregistré."))
            {
                row.FigerSnapshot();
            }
            else
            {
                row.Restaurer();
            }
        }

        private async Task SupprimerAsync(EcheancierRow row)
        {
            var result = await _echeancierService.DeleteModaliteAsync(row.Id);
            if (Verifier(result, $"Barème « {row.NiveauCode} / {row.Statut} » supprimé."))
            {
                await ChargerAsync();
            }
        }

        // ------------------- ViewModel mutable de la grille -------------------

        public sealed class EcheancierRow
        {
            public EcheancierRow(ModaliteVersementItem m)
            {
                Id = m.Id; AnneeScolaire = m.AnneeScolaire; NiveauCode = m.NiveauCode; Statut = m.Statut;
                Inscription = m.Inscription; Septembre = m.Septembre; Octobre = m.Octobre;
                Novembre = m.Novembre; Decembre = m.Decembre; Janvier = m.Janvier;
                Fevrier = m.Fevrier; Mars = m.Mars; Avril = m.Avril; Mai = m.Mai;
                FigerSnapshot();
            }

            public Guid Id { get; }
            public string AnneeScolaire { get; }
            public string NiveauCode { get; }   // non éditable
            public string Statut { get; }       // non éditable
            public decimal Inscription { get; set; }
            public decimal Septembre { get; set; }
            public decimal Octobre { get; set; }
            public decimal Novembre { get; set; }
            public decimal Decembre { get; set; }
            public decimal Janvier { get; set; }
            public decimal Fevrier { get; set; }
            public decimal Mars { get; set; }
            public decimal Avril { get; set; }
            public decimal Mai { get; set; }

            // Total calculé, jamais éditable : somme d'Inscription à Mai.
            public decimal Total => Inscription + Septembre + Octobre + Novembre + Decembre
                                  + Janvier + Fevrier + Mars + Avril + Mai;

            // Snapshot pour ne sauver au blur que si un montant a changé (et restaurer si refus).
            private decimal[] _snapshot = [];
            private decimal[] Montants => [Inscription, Septembre, Octobre, Novembre, Decembre,
                                           Janvier, Fevrier, Mars, Avril, Mai];

            public bool EstModifiee => !Montants.SequenceEqual(_snapshot);
            public void FigerSnapshot() => _snapshot = Montants;

            public void Restaurer()
            {
                Inscription = _snapshot[0]; Septembre = _snapshot[1]; Octobre = _snapshot[2];
                Novembre = _snapshot[3]; Decembre = _snapshot[4]; Janvier = _snapshot[5];
                Fevrier = _snapshot[6]; Mars = _snapshot[7]; Avril = _snapshot[8]; Mai = _snapshot[9];
            }

            public ModaliteVersementItem VersItem() => new()
            {
                Id = Id, AnneeScolaire = AnneeScolaire, NiveauCode = NiveauCode, Statut = Statut,
                Inscription = Inscription, Septembre = Septembre, Octobre = Octobre,
                Novembre = Novembre, Decembre = Decembre, Janvier = Janvier,
                Fevrier = Fevrier, Mars = Mars, Avril = Avril, Mai = Mai,
            };
        }
    }
}
