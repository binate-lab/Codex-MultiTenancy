using System.Globalization;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Scolarites
{
    public partial class Scolarites
    {
        [Inject] private IScolariteEleveService _scolariteEleveService { get; set; } = default!;
        [Inject] private IStructureService _structureService { get; set; } = default!;
        [Inject] private IVersementService _versementService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        // Année scolaire en cours (bandeau) — même source que SchoolNavMenu.
        private string _annee = "—";

        // Nom d'affichage de l'école active (en-tête du reçu PDF) — même source
        // que SchoolNavMenu (GetMineAsync filtré sur le claim school).
        private string _nomEcole = string.Empty;

        // Référentiel structures de l'école (module Structures de pedagogie-api) :
        // les sélecteurs Niveau/Classe de la grille et du filtre sont alimentés par
        // CE référentiel, plus par une liste figée dans le code.
        private List<string> _niveaux = new();
        private IReadOnlyList<ClasseItem> _classesRef = new List<ClasseItem>();

        // Classes ouvertes pour le niveau donné (cellule Classe d'une ligne).
        private IEnumerable<ClasseItem> ClassesPour(string niveau) =>
            _classesRef.Where(c => c.NiveauCode == niveau);

        // Format monétaire façon Access : « 35 000 F » (espace comme séparateur de milliers).
        private static readonly CultureInfo _fr = CultureInfo.GetCultureInfo("fr-FR");

        // ---- État des filtres (barre de recherche) ----
        private string _fNom = string.Empty;
        private string _fPrenoms = string.Empty;
        private string _fMatricule = string.Empty;
        private string _fNiveau = string.Empty;   // "" = tous
        private string _fClasse = string.Empty;
        private string _fStatut = "Tous";
        private string _fInscrit = "Tous";

        // Élèves de l'école, chargés depuis Scolarite.Api (ScolariteDb) dans OnInitializedAsync.
        private List<EleveScolariteRow> _all = new();

        protected override async Task OnInitializedAsync()
        {
            // École active = claim « school » (= CodeEts) du JWT école-scoped.
            var user = await _applicationStateProvider.GetAuthenticationStateProviderUserAsync();
            var codeEts = user.FindFirst("school")?.Value ?? string.Empty;

            // Année scolaire en cours (bandeau).
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            // Référentiel structures : niveaux (dans l'ordre configuré) + classes de l'année.
            var cycles = await _structureService.GetCyclesAsync();
            _niveaux = cycles.SelectMany(c => c.Niveaux).Select(n => n.Code).ToList();
            _classesRef = await _structureService.GetClassesAsync(_annee == "—" ? null : _annee);

            // Nom de l'école active (en-tête du reçu PDF).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var ecoles = await _schoolService.GetMineAsync();
                if (ecoles.IsSuccessful && ecoles.Data is not null)
                {
                    _nomEcole = ecoles.Data.FirstOrDefault(s => s.CodeEts == codeEts)?.Name ?? string.Empty;
                }
            }

            // Chargement des élèves de l'école depuis Scolarite.Api (table Eleve / ScolariteDb).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var eleves = await _scolariteEleveService.GetElevesAsync(codeEts);
                _all = eleves.Select(e => new EleveScolariteRow(
                    e.Id,
                    e.Matricule,
                    e.Telephone,
                    e.Nom,
                    e.Prenom,
                    e.Inscrit,
                    e.Statut,
                    e.Niveau,
                    e.Classe,
                    e.Solde,            // colonne « Net à payer » ≈ solde restant
                    e.FraisScolarite    // colonne « Inscription » ≈ frais (à affiner plus tard)
                )).ToList();
            }
        }

        // Filtre client sur la source. Tous les critères se cumulent (ET).
        private IEnumerable<EleveScolariteRow> Filtered =>
            _all.Where(e =>
                (string.IsNullOrWhiteSpace(_fNom)
                    || e.Nom.Contains(_fNom, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fPrenoms)
                    || e.Prenoms.Contains(_fPrenoms, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fMatricule)
                    || Compact(e.Matricule).Contains(Compact(_fMatricule), StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(_fNiveau) || e.Niveau == _fNiveau)
                && (string.IsNullOrWhiteSpace(_fClasse)
                    || e.Classe.Contains(_fClasse, StringComparison.OrdinalIgnoreCase))
                && (_fStatut == "Tous" || e.Statut == _fStatut)
                && (_fInscrit == "Tous" || (_fInscrit == "Oui") == e.Inscrit));

        private void Effacer()
        {
            _fNom = _fPrenoms = _fMatricule = _fNiveau = _fClasse = string.Empty;
            _fStatut = _fInscrit = "Tous";
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // Édition en ligne du statut (Aff/Naff) d'un élève.
        // TODO (à venir) : publier un événement « changement de statut » relié à
        // l'échéancier (recalcul des modalités de versement selon Aff/Naff).
        private void OnStatutChanged(EleveScolariteRow row, string nouveau)
        {
            row.Statut = nouveau;
        }

        // Édition en ligne du niveau d'un élève. La classe est invalidée si elle
        // n'appartient plus au nouveau niveau (cascade du référentiel structures).
        // TODO (à venir) : publier un événement « changement de niveau » relié à
        // l'échéancier (les frais / l'échéancier dépendent du niveau).
        private void OnNiveauChanged(EleveScolariteRow row, string nouveau)
        {
            row.Niveau = nouveau;
            if (!ClassesPour(nouveau).Any(c => c.Libelle == row.Classe))
            {
                row.Classe = string.Empty;
            }
        }

        // Édition en ligne de la classe d'un élève.
        // TODO (à venir) : publier un événement « changement de classe » relié à
        // l'échéancier / aux listes de classe.
        private void OnClasseChanged(EleveScolariteRow row, string nouveau)
        {
            row.Classe = nouveau;
        }

        // ================== Versements de l'élève sélectionné ==================

        // Élève sélectionné (clic sur une ligne de la grille rouge) + son état versements.
        private EleveScolariteRow _sel;
        private ScolariteResume _resume;
        private List<VersementDetailItem> _versements = new();
        private List<EcheanceEleveItem> _echeancier = new();

        // Champs de saisie du sous-form bleu ciel.
        private decimal _vMontant;
        private DateTime? _vDate = DateTime.Today;
        private string _vNature = "Inscription";
        private string _vMode = "Espèce";
        private string _vRef = string.Empty;
        private bool _vEnCours;

        private async Task OnRowClick(TableRowClickEventArgs<EleveScolariteRow> args)
        {
            _sel = args.Item;
            NouveauVersement();
            await ChargerVersementsAsync();
        }

        // Surligne la ligne sélectionnée dans la grille rouge.
        private string ClasseLigne(EleveScolariteRow row, int _)
            => row == _sel ? "svt-ligne-sel" : string.Empty;

        private async Task ChargerVersementsAsync()
        {
            var data = await _versementService.GetVersementsAsync(_sel.Id);
            AppliquerReponse(data);
        }

        private void AppliquerReponse(VersementsEleveReponse data)
        {
            _resume = data?.Resume;
            _versements = data?.Versements ?? new List<VersementDetailItem>();
            _echeancier = data?.Echeancier ?? new List<EcheanceEleveItem>();

            // Rafraîchit la colonne « Net à payer » de la ligne (reste à payer à jour).
            if (_resume is not null && _sel is not null)
            {
                _sel.NetAPayer = _resume.Reste;
                _sel.Inscription = _resume.FraisScolarite;
            }
        }

        // « Nouveau » : réinitialise la saisie (date du jour, Espèce, nature Inscription).
        private void NouveauVersement()
        {
            _vMontant = 0;
            _vDate = DateTime.Today;
            _vNature = "Inscription";
            _vMode = "Espèce";
            _vRef = string.Empty;
        }

        private async Task ValiderVersementAsync()
        {
            if (_sel is null) return;
            if (_vMontant <= 0)
            {
                _snackbar.Add("Saisis d'abord le montant du versement.", Severity.Warning);
                return;
            }

            _vEnCours = true;
            try
            {
                var result = await _versementService.CreateAsync(
                    _sel.Id, _vMontant, _vDate, _vNature, _vMode, _vRef);

                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Versement de {Fmt(_vMontant)} enregistré pour {_sel.Nom} {_sel.Prenoms}.", Severity.Success);
                    AppliquerReponse(result.Data);
                    NouveauVersement();
                }
                else
                {
                    _snackbar.Add(result.Error, Severity.Error);
                }
            }
            finally
            {
                _vEnCours = false;
            }
        }

        // ================== Reçu de paiement (PDF) ==================

        private bool _recuEnCours;

        // Télécharge le reçu PDF de l'élève sélectionné (situation du compte :
        // versements + synthèse + échéancier) — calque du reçu Access.
        private async Task TelechargerRecuAsync()
        {
            if (_sel is null) return;

            _recuEnCours = true;
            try
            {
                var pdf = await _versementService.GetRecuPdfAsync(_sel.Id, _nomEcole);
                if (pdf is null || pdf.Length == 0)
                {
                    _snackbar.Add("Reçu indisponible pour cet élève.", Severity.Error);
                    return;
                }

                var nomFichier = $"recu-{Compact(_sel.Matricule)}-{DateTime.Today:yyyyMMdd}.pdf";
                await _js.InvokeVoidAsync("trajanTelechargerFichier",
                    nomFichier, Convert.ToBase64String(pdf), "application/pdf");
            }
            finally
            {
                _recuEnCours = false;
            }
        }

        // Libellé d'affichage des natures (les valeurs sont les noms de l'enum backend).
        private static string AfficherNature(string nature) => nature switch
        {
            "Scolarite" => "Scolarité",
            "Arriere" => "Arriéré",
            _ => nature,
        };

        // Libellé et couleur du statut d'une échéance (valeurs = noms de l'enum backend).
        private static string AfficherStatut(string statut) => statut switch
        {
            "Paye" => "Payé",
            "Partiel" => "Partiel",
            "NonPaye" => "Non payé",
            _ => statut,
        };

        private static string ClasseStatut(string statut) => statut switch
        {
            "Paye" => "svt-ech-paye",
            "Partiel" => "svt-ech-partiel",
            _ => "svt-ech-nonpaye",
        };

        // Comparaison de matricule insensible aux espaces (« 24 179 400 X » ~ « 24179400X »).
        private static string Compact(string s) => s.Replace(" ", string.Empty);

        private static string Fmt(decimal montant) => montant.ToString("#,0", _fr) + " F";

        // Ligne de la grille rouge (calque des colonnes du formulaire Access « Scolarités »).
        // Statut et Niveau sont modifiables directement dans la grille → propriétés set.
        public sealed class EleveScolariteRow
        {
            public Guid Id { get; }
            public string Matricule { get; }
            public string TelCorrespondant { get; }
            public string Nom { get; }
            public string Prenoms { get; }
            public bool Inscrit { get; set; }
            public string Statut { get; set; }
            public string Niveau { get; set; }
            public string Classe { get; set; }
            public decimal NetAPayer { get; set; }   // rafraîchi après chaque versement (reste à payer)
            public decimal Inscription { get; set; } // frais de l'année (échéancier généré)

            public EleveScolariteRow(
                Guid id, string matricule, string telCorrespondant, string nom, string prenoms,
                bool inscrit, string statut, string niveau, string classe,
                decimal netAPayer, decimal inscription)
            {
                Id = id;
                Matricule = matricule;
                TelCorrespondant = telCorrespondant;
                Nom = nom;
                Prenoms = prenoms;
                Inscrit = inscrit;
                Statut = statut;
                Niveau = niveau;
                Classe = classe;
                NetAPayer = netAPayer;
                Inscription = inscription;
            }
        }
    }
}
