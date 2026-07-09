using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TrajanEcoleApp.Pages.ListesClasse
{
    // Page « Liste de classe » : trombinoscope/roster des élèves de l'école, filtrable en
    // cascade Cycle -> Niveau -> Classe (référentiel Structures) + Statut / Inscrit / Actif.
    // Même source que /scolarites (Scolarite.Api, ScolariteDb) et même look Access (grille
    // rouge 26px). Colonnes Statut et Classe éditables en ligne, avec navigation clavier
    // Haut/Bas et copie de la cellule du dessus (Ctrl+') — réutilise le handler JS global
    // « svtGrilleEleves » et la classe .acc-grid-eleves (cf. wwwroot/index.html).
    //
    // PÉRIMÈTRE (front seul) : les éditions Statut/Classe restent EN MÉMOIRE (aucune
    // persistance backend, comme la grille /scolarites aujourd'hui).
    public partial class ListesClasse : IDisposable
    {
        // Source des élèves = Pedagogie.Api (PedagogieDb), via le client typé _eleveService
        // (IEleveService, injecté globalement par _Imports.razor). NE PAS taper Scolarite.Api.
        [Inject] private IStructureService _structureService { get; set; } = default!;
        [Inject] private IJSRuntime _js { get; set; } = default!;

        // Année scolaire en cours (bandeau) — même source que SchoolNavMenu.
        private string _annee = "—";

        // Référentiel Structures de l'école : cycles (avec leurs niveaux) + classes de l'année.
        // Alimente les filtres en cascade et la déroulante Classe des lignes.
        private IReadOnlyList<CycleItem> _cycles = new List<CycleItem>();
        private List<string> _niveauxAll = new();
        private IReadOnlyList<ClasseItem> _classesRef = new List<ClasseItem>();

        // Élèves de l'école, chargés depuis Scolarite.Api (ScolariteDb).
        private List<EleveRow> _all = new();

        // ---- État des filtres (barre de recherche, tous cumulés en ET) ----
        private string _fCycleId = string.Empty;   // "" = tous (Guid du cycle sinon)
        private string _fNiveau = string.Empty;     // "" = tous
        private string _fClasse = string.Empty;     // "" = toutes
        private string _fStatut = "Tous";
        private string _fInscrit = "Tous";
        private string _fActif = "Tous";

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

            // Référentiel Structures : cycles + niveaux (dans l'ordre configuré) + classes.
            _cycles = await _structureService.GetCyclesAsync();
            _niveauxAll = _cycles
                .SelectMany(c => c.Niveaux.OrderBy(n => n.Ordre))
                .Select(n => n.Code)
                .ToList();
            _classesRef = await _structureService.GetClassesAsync(_annee == "—" ? null : _annee);

            // Chargement des élèves de l'école depuis Pedagogie.Api (PedagogieDb).
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var eleves = await _eleveService.GetElevesAsync(codeEts);
                _all = eleves.Select(e => new EleveRow(
                    e.Id, e.NumOrdre, e.Matricule, e.Nom, e.Prenom,
                    CycleLibelle(e.Cycle), e.Niveau, e.Serie, e.Classe,
                    StatutLibelle(e.Statut), e.Sexe, e.DateNaissance, e.LieuNaissance,
                    e.Nationalite, e.Telephone, e.IsInscrit, e.IsActif)).ToList();
            }
        }

        // Libellé du cycle (référentiel Structures) à partir de son numéro : Pedagogie renvoie
        // le cycle en entier. Repli sur le numéro si absent du référentiel.
        private string CycleLibelle(int numero) =>
            _cycles.FirstOrDefault(c => c.Numero == numero)?.Libelle ?? numero.ToString();

        // Statut élève : Pedagogie renvoie l'entier de l'enum StatutEleve (Aff=1, Naff=2).
        private static string StatutLibelle(int statut) => statut == 1 ? "Aff" : "Naff";

        // ---- Cascade des filtres (Cycle -> Niveau -> Classe) ----

        // Codes des niveaux d'un cycle donné (par son Id), dans l'ordre.
        private IEnumerable<string> NiveauxDuCycle(string cycleId) =>
            _cycles.FirstOrDefault(c => c.Id.ToString() == cycleId)?
                .Niveaux.OrderBy(n => n.Ordre).Select(n => n.Code)
            ?? Enumerable.Empty<string>();

        // Niveaux proposés par le filtre : ceux du cycle choisi, sinon tous.
        private IEnumerable<string> NiveauxFiltre =>
            string.IsNullOrWhiteSpace(_fCycleId) ? _niveauxAll : NiveauxDuCycle(_fCycleId);

        // Classes proposées par le filtre : celles du niveau choisi ; sinon celles du cycle
        // choisi ; sinon toutes les classes de l'école.
        private IEnumerable<ClasseItem> ClassesFiltre
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_fNiveau))
                    return _classesRef.Where(c => c.NiveauCode == _fNiveau);
                if (!string.IsNullOrWhiteSpace(_fCycleId))
                {
                    var codes = NiveauxDuCycle(_fCycleId).ToHashSet();
                    return _classesRef.Where(c => codes.Contains(c.NiveauCode));
                }
                return _classesRef;
            }
        }

        // Classes ouvertes pour un niveau (déroulante Classe d'une ligne).
        private IEnumerable<ClasseItem> ClassesPour(string niveau) =>
            _classesRef.Where(c => c.NiveauCode == niveau);

        // Changement du filtre Cycle : on répercute et on vide Niveau/Classe s'ils ne
        // font plus partie du nouveau cycle (cascade descendante).
        private void OnFiltreCycleChanged(string cycleId)
        {
            _fCycleId = cycleId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_fNiveau) && !NiveauxFiltre.Contains(_fNiveau))
                _fNiveau = string.Empty;
            if (!string.IsNullOrWhiteSpace(_fClasse) && !ClassesFiltre.Any(c => c.Libelle == _fClasse))
                _fClasse = string.Empty;
        }

        // Changement du filtre Niveau : vide le filtre Classe s'il ne fait plus partie
        // des classes du nouveau niveau.
        private void OnFiltreNiveauChanged(string niveau)
        {
            _fNiveau = niveau ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_fClasse) && !ClassesFiltre.Any(c => c.Libelle == _fClasse))
                _fClasse = string.Empty;
        }

        // Filtre client sur la source. Tous les critères se cumulent (ET).
        private IEnumerable<EleveRow> Filtered =>
            _all.Where(e =>
                (string.IsNullOrWhiteSpace(_fCycleId) || NiveauxDuCycle(_fCycleId).Contains(e.Niveau))
                && (string.IsNullOrWhiteSpace(_fNiveau) || e.Niveau == _fNiveau)
                && (string.IsNullOrWhiteSpace(_fClasse) || e.Classe == _fClasse)
                && (_fStatut == "Tous" || e.Statut == _fStatut)
                && (_fInscrit == "Tous" || (_fInscrit == "Oui") == e.Inscrit)
                && (_fActif == "Tous" || (_fActif == "Oui") == e.Actif));

        private void Effacer()
        {
            _fCycleId = _fNiveau = _fClasse = string.Empty;
            _fStatut = _fInscrit = _fActif = "Tous";
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ---- Éditions en ligne (en mémoire pour l'instant) ----
        private void OnStatutChanged(EleveRow row, string nouveau) => row.Statut = nouveau;
        private void OnClasseChanged(EleveRow row, string nouveau) => row.Classe = nouveau;

        // ---- Actions par ligne (menu 3-points) ----

        // Fiche élève : pas encore de page dédiée -> stub informatif.
        private void VoirFiche(EleveRow row) =>
            _snackbar.Add($"Fiche de {row.Nom} {row.Prenoms} : fonction à venir.", Severity.Info);

        // La classe est déjà modifiable en ligne : on guide l'utilisateur vers la colonne.
        private void AllerClasse(EleveRow row) =>
            _snackbar.Add("Modifie la classe directement dans la colonne « Classe » (Ctrl+' recopie celle du dessus).", Severity.Info);

        // Impression de la liste : impression navigateur (front seul, réel). Un export
        // serveur (PDF/Excel) sera câblé plus tard si besoin.
        private async Task ImprimerAsync() => await _js.InvokeVoidAsync("window.print");

        // ================== Navigation clavier & copie « cellule du dessus » ==================
        // Réutilise le handler JS global « svtGrilleEleves » (index.html) : Haut/Bas déplacent
        // le focus entre lignes, Ctrl+' recopie la valeur de la cellule du dessus via ce
        // rappel .NET — indispensable pour les colonnes déroulantes (Statut/Classe).
        private DotNetObjectReference<ListesClasse>? _dotnetRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotnetRef = DotNetObjectReference.Create(this);
                await _js.InvokeVoidAsync("svtGrilleEleves.init", _dotnetRef);
            }
        }

        [JSInvokable]
        public Task CopierCelluleDuHaut(string sourceId, string cibleId, string col)
        {
            if (!Guid.TryParse(sourceId, out var sId) || !Guid.TryParse(cibleId, out var cId))
                return Task.CompletedTask;

            var source = _all.FirstOrDefault(e => e.Id == sId);
            var cible = _all.FirstOrDefault(e => e.Id == cId);
            if (source is null || cible is null || source == cible)
                return Task.CompletedTask;

            switch (col)
            {
                case "Statut":
                    cible.Statut = source.Statut;
                    break;
                case "Classe":
                    // On ne recopie la classe que si elle est valide pour le niveau de la cible.
                    if (ClassesPour(cible.Niveau).Any(c => c.Libelle == source.Classe))
                        cible.Classe = source.Classe;
                    break;
            }

            StateHasChanged();
            return Task.CompletedTask;
        }

        public void Dispose() => _dotnetRef?.Dispose();

        // Ligne de la grille (roster), projetée depuis Pedagogie. Statut et Classe modifiables
        // en ligne -> set. NumOrdre / Red / LV_2 non exposés par le DTO de sortie (reportés).
        public sealed class EleveRow
        {
            public Guid Id { get; }
            public int NumOrdre { get; }        // N° Inscription (unique par école)
            public string Matricule { get; }
            public string Nom { get; }
            public string Prenoms { get; }
            public string CycleLibelle { get; }
            public string Niveau { get; }       // lecture seule (fige les classes proposées)
            public string Serie { get; }
            public string Classe { get; set; }
            public string Statut { get; set; }
            public string Sexe { get; }
            public DateTime? DateNaissance { get; }
            public string LieuNaissance { get; }
            public string Nationalite { get; }
            public string Telephone { get; }
            public bool Inscrit { get; }
            public bool Actif { get; }

            public EleveRow(
                Guid id, int numOrdre, string matricule, string nom, string prenoms,
                string cycleLibelle, string niveau, string serie, string classe,
                string statut, string sexe, DateTime? dateNaissance, string lieuNaissance,
                string nationalite, string telephone, bool inscrit, bool actif)
            {
                Id = id;
                NumOrdre = numOrdre;
                Matricule = matricule;
                Nom = nom;
                Prenoms = prenoms;
                CycleLibelle = cycleLibelle;
                Niveau = niveau;
                Serie = serie;
                Classe = classe;
                Statut = statut;
                Sexe = sexe;
                DateNaissance = dateNaissance;
                LieuNaissance = lieuNaissance;
                Nationalite = nationalite;
                Telephone = telephone;
                Inscrit = inscrit;
                Actif = actif;
            }
        }
    }
}
