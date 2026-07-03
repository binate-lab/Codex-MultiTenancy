using System.Globalization;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;

namespace TrajanEcoleApp.Pages.Scolarites
{
    public partial class Scolarites
    {
        [Inject] private IScolariteEleveService _scolariteEleveService { get; set; } = default!;
        [Inject] private IStructureService _structureService { get; set; } = default!;

        // Année scolaire en cours (bandeau) — même source que SchoolNavMenu.
        private string _annee = "—";

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

            // Chargement des élèves de l'école depuis Scolarite.Api (table Eleve / ScolariteDb).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var eleves = await _scolariteEleveService.GetElevesAsync(codeEts);
                _all = eleves.Select(e => new EleveScolariteRow(
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

        // Comparaison de matricule insensible aux espaces (« 24 179 400 X » ~ « 24179400X »).
        private static string Compact(string s) => s.Replace(" ", string.Empty);

        private static string Fmt(decimal montant) => montant.ToString("#,0", _fr) + " F";

        // Ligne de la grille rouge (calque des colonnes du formulaire Access « Scolarités »).
        // Statut et Niveau sont modifiables directement dans la grille → propriétés set.
        public sealed class EleveScolariteRow
        {
            public string Matricule { get; }
            public string TelCorrespondant { get; }
            public string Nom { get; }
            public string Prenoms { get; }
            public bool Inscrit { get; set; }
            public string Statut { get; set; }
            public string Niveau { get; set; }
            public string Classe { get; set; }
            public decimal NetAPayer { get; }
            public decimal Inscription { get; }

            public EleveScolariteRow(
                string matricule, string telCorrespondant, string nom, string prenoms,
                bool inscrit, string statut, string niveau, string classe,
                decimal netAPayer, decimal inscription)
            {
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
