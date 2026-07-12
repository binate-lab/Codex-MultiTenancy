using System.IO;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using TrajanEcole.Shared.Library.Enums;

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

        // École PUBLIQUE ? Régit l'édition de Cycle / Niveau / Statut : éditables uniquement en
        // public (en privé, l'échéancier en dépend → colonnes en lecture seule). La Classe reste
        // éditable dans les deux cas.
        private bool _ecolePublique;

        // Nom + logo + ville de l'école active (en-tête « type reçu » de la Fiche Élève et
        // en-tête de la liste imprimée : « MENA-DREN : {Ville} »).
        private string _nomEcole = string.Empty;
        private string _logoEcole = string.Empty;
        private string _villeEcole = string.Empty;

        // CodeEts de l'école active (claim « school ») — mémorisé pour recharger le roster.
        private string _codeEts = string.Empty;

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

        // Page courante (0-based) et taille de page de la grille : pilotées par la MudTable.
        // On les suit pour recaler la sélection sur le 1er élève de la page affichée.
        private int _page;
        private int _rowsPerPage = 10;

        protected override async Task OnInitializedAsync()
        {
            // École active = claim « school » (= CodeEts) du JWT école-scoped.
            var user = await _applicationStateProvider.GetAuthenticationStateProviderUserAsync();
            var codeEts = user.FindFirst("school")?.Value ?? string.Empty;
            _codeEts = codeEts;

            // Année scolaire en cours (bandeau).
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            // Statut de l'école active (Public/Prive) : gouverne l'édition Cycle/Niveau/Statut.
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                var ecoles = await _schoolService.GetMineAsync();
                if (ecoles.IsSuccessful && ecoles.Data is not null)
                {
                    var ecole = ecoles.Data.FirstOrDefault(s => s.CodeEts == codeEts);
                    _ecolePublique = ecole?.Statut == StatutEcole.Public;
                    _nomEcole = ecole?.Name ?? string.Empty;
                    _logoEcole = ecole?.Logo ?? string.Empty;
                    _villeEcole = ecole?.Ville ?? string.Empty;
                }
            }

            // Référentiel Structures : cycles + niveaux (dans l'ordre configuré) + classes.
            _cycles = await _structureService.GetCyclesAsync();
            _niveauxAll = _cycles
                .SelectMany(c => c.Niveaux.OrderBy(n => n.Ordre))
                .Select(n => n.Code)
                .ToList();
            _classesRef = await _structureService.GetClassesAsync(_annee == "—" ? null : _annee);

            // Chargement des élèves de l'école depuis Pedagogie.Api (PedagogieDb).
            await ChargerElevesAsync();

            // À l'affichage : on sélectionne déjà le 1er élève filtré et on montre sa fiche
            // (pas besoin d'attendre un clic).
            SelectionnerPremierDePage();
        }

        // Statut élève : Pedagogie renvoie l'entier de l'enum StatutEleve (Aff=1, Naff=2).
        private static string StatutLibelle(int statut) => statut == 1 ? "Aff" : "Naff";

        // (Re)charge le roster de l'école active depuis Pedagogie.Api et le projette en EleveRow.
        private async Task ChargerElevesAsync()
        {
            if (string.IsNullOrWhiteSpace(_codeEts)) return;

            var eleves = await _eleveService.GetElevesAsync(_codeEts);
            _all = eleves.Select(e => new EleveRow(
                e.Id, e.NumOrdre, e.Matricule, e.Nom, e.Prenom,
                e.Cycle, e.Niveau, e.Serie, e.Classe,
                StatutLibelle(e.Statut), e.Sexe, e.DateNaissance, e.LieuNaissance,
                e.Nationalite, e.Telephone, e.IsInscrit, e.IsActif, e.ImageFile,
                e.Tuteur?.Nom ?? string.Empty, e.Tuteur?.Prenom ?? string.Empty,
                e.Tuteur?.Telephone1 ?? string.Empty, e.Tuteur?.Telephone2 ?? string.Empty)).ToList();
        }

        // Corrige en masse les clés de contrôle des matricules de l'école (garde les chiffres).
        // Idempotent : un matricule déjà valide n'est pas modifié. Écrit le journal côté Pedagogie.
        private async Task RegenererClesAsync()
        {
            var ok = await _js.InvokeAsync<bool>("confirm",
                "Corriger les clés de contrôle de tous les matricules de l'école ?\n" +
                "Les numéros sont conservés ; seule la lettre de clé est recalculée si elle est incohérente.");
            if (!ok) return;

            var res = await _eleveService.RegenererMatriculesAsync(complet: false);
            if (!res.IsSuccessful)
            {
                _snackbar.Add($"Correction des clés impossible : {res.Error}", Severity.Error);
                return;
            }

            _snackbar.Add(
                res.Corriges == 0
                    ? $"Tous les matricules étaient déjà valides ({res.Total})."
                    : $"{res.Corriges} clé(s) corrigée(s) sur {res.Total}.",
                Severity.Success);

            await ChargerElevesAsync();   // reflète les nouveaux matricules dans la grille
            AppliquerFiltre();
        }

        // Formate le matricule national façon Access : « 22654456M » -> « 22 654 456 M »
        // (chiffres groupés par 3 depuis la droite, lettre(s) de contrôle finale séparée(s)).
        private static string FormatMatricule(string mat)
        {
            if (string.IsNullOrWhiteSpace(mat)) return mat ?? string.Empty;
            var compact = mat.Replace(" ", string.Empty);

            // Sépare la lettre de contrôle finale (partie non chiffrée) des chiffres de tête.
            var i = compact.Length;
            while (i > 0 && !char.IsDigit(compact[i - 1])) i--;
            var digits = compact[..i];
            var suffixe = compact[i..];

            if (digits.Length == 0 || !digits.All(char.IsDigit))
                return compact;   // format inattendu : on renvoie tel quel (sans espaces)

            var sb = new System.Text.StringBuilder();
            for (var k = 0; k < digits.Length; k++)
            {
                if (k > 0 && (digits.Length - k) % 3 == 0) sb.Append(' ');
                sb.Append(digits[k]);
            }

            return string.IsNullOrEmpty(suffixe) ? sb.ToString() : $"{sb} {suffixe}";
        }

        // Mise en forme du libellé de classe pour l'impression : les classes de COLLÈGE (cycle 1)
        // « 6e1 / 5e1 / 4e3 / 3e2 » deviennent « 6è 1 / 5è 1 / 4è 3 / 3è 2 » (e→è + espace avant
        // la subdivision). Le 2nd cycle (2nde, 1ere, TleA1, TleD3…) n'est PAS concerné : le motif
        // exige un chiffre de niveau 3–6 suivi de « e » puis d'une subdivision chiffrée.
        private static string FormatClasse(string classe)
        {
            if (string.IsNullOrWhiteSpace(classe)) return classe ?? string.Empty;
            return System.Text.RegularExpressions.Regex
                .Replace(classe.Trim(), @"^([3-6])e(?=\d|$)", "$1è ")
                .Trim();
        }

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
            AppliquerFiltre();
        }

        // Changement du filtre Niveau : vide le filtre Classe s'il ne fait plus partie
        // des classes du nouveau niveau.
        private void OnFiltreNiveauChanged(string niveau)
        {
            _fNiveau = niveau ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_fClasse) && !ClassesFiltre.Any(c => c.Libelle == _fClasse))
                _fClasse = string.Empty;
            AppliquerFiltre();
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
            AppliquerFiltre();
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ---- Sélection d'un élève (clic sur une ligne) → Fiche Élève ----
        private EleveRow? _sel;

        private void OnRowClick(TableRowClickEventArgs<EleveRow> args) => _sel = args.Item;

        // À appeler après TOUT changement de filtre : revient à la 1re page et recale la
        // sélection (donc la fiche) sur le 1er élève de la liste filtrée. Un clic ligne reste
        // prioritaire tant qu'on ne retouche pas les filtres.
        private void AppliquerFiltre()
        {
            _page = 0;
            SelectionnerPremierDePage();
        }

        // Sélectionne le 1er élève de la PAGE actuellement affichée (les éléments visibles vont
        // de _page*_rowsPerPage à +_rowsPerPage dans la liste filtrée).
        private void SelectionnerPremierDePage() =>
            _sel = Filtered.Skip(_page * _rowsPerPage).FirstOrDefault();

        // Flèches du pager : on suit la page et on recale la fiche sur le 1er élève affiché.
        private void OnPageChanged(int page)
        {
            _page = page;
            SelectionnerPremierDePage();
        }

        // Changement de taille de page (5/10/25…) : on revient page 1 et on recale la fiche.
        private void OnRowsPerPageChanged(int taille)
        {
            _rowsPerPage = taille;
            _page = 0;
            SelectionnerPremierDePage();
        }

        // Handlers des filtres à valeur simple (Classe/Statut/Inscrit/Actif) : on passe par
        // Value/ValueChanged (et non @bind-Value:after, qui ne se déclenche pas de façon fiable
        // sur MudSelect) pour garantir le recalage de la fiche.
        private void OnFiltreClasseChanged(string v) { _fClasse = v ?? string.Empty; AppliquerFiltre(); }
        private void OnFiltreStatutChanged(string v) { _fStatut = v ?? "Tous"; AppliquerFiltre(); }
        private void OnFiltreInscritChanged(string v) { _fInscrit = v ?? "Tous"; AppliquerFiltre(); }
        private void OnFiltreActifChanged(string v) { _fActif = v ?? "Tous"; AppliquerFiltre(); }

        // Surligne la ligne sélectionnée dans la grille rouge.
        private string LigneClass(EleveRow row, int _) => row == _sel ? "lc-ligne-sel" : string.Empty;

        // Ajout/modification de la photo : le fichier choisi (fenêtre Windows) est redimensionné,
        // encodé en base64 (data URL) — comme les photos de profil de l'app — puis enregistré
        // dans Pedagogie (Eleve.ImageFile). Le navigateur ne donne pas le chemin Windows réel,
        // on stocke donc le CONTENU de l'image (seul moyen fiable de l'afficher ensuite).
        private async Task OnPhotoSelected(InputFileChangeEventArgs args)
        {
            if (_sel is null) return;

            try
            {
                var img = await args.File.RequestImageFileAsync(args.File.ContentType, 512, 512);
                using var stream = img.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var dataUrl = $"data:{img.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

                if (await _eleveService.MajPhotoAsync(_sel.Id, dataUrl))
                {
                    _sel.ImageFile = dataUrl;   // affichage immédiat (PhotoSrc gère les data URL)
                    _snackbar.Add("Photo enregistrée.", Severity.Success);
                }
                else
                {
                    _snackbar.Add("Impossible d'enregistrer la photo.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Photo : {ex.Message}", Severity.Error);
            }
        }

        // Suppression de la photo : on vide Eleve.ImageFile en base (réutilise le même endpoint
        // PUT avec une chaîne vide) après confirmation.
        private async Task SupprimerPhoto()
        {
            if (_sel is null || string.IsNullOrEmpty(_sel.ImageFile)) return;

            var ok = await _js.InvokeAsync<bool>("confirm",
                $"Supprimer la photo de {_sel.Nom} {_sel.Prenoms} ?");
            if (!ok) return;

            if (await _eleveService.MajPhotoAsync(_sel.Id, string.Empty))
            {
                _sel.ImageFile = string.Empty;   // repasse au placeholder « Pas de photo »
                _snackbar.Add("Photo supprimée.", Severity.Success);
            }
            else
            {
                _snackbar.Add("Impossible de supprimer la photo.", Severity.Error);
            }
        }

        // Source affichable de la photo : data URL / http / chemin absolu tels quels ; sinon
        // on suppose du base64 nu et on préfixe. Null si vide → placeholder « Pas de photo ».
        private static string? PhotoSrc(EleveRow row)
        {
            var img = row?.ImageFile;
            if (string.IsNullOrWhiteSpace(img)) return null;
            if (img.StartsWith("data:") || img.StartsWith("http", StringComparison.OrdinalIgnoreCase) || img.StartsWith("/"))
                return img;
            return $"data:image/jpeg;base64,{img}";
        }

        // ---- Éditions en ligne, PERSISTÉES dans Pedagogie (rollback si l'appel échoue) ----
        private async Task OnStatutChanged(EleveRow row, string nouveau)
        {
            var ancien = row.Statut;
            if (nouveau == ancien) return;

            row.Statut = nouveau;
            if (!await _eleveService.MajStatutAsync(row.Id, nouveau))
            {
                row.Statut = ancien;   // restaure l'ancienne valeur
                _snackbar.Add("Impossible d'enregistrer le statut.", Severity.Error);
            }
        }

        private async Task OnClasseChanged(EleveRow row, string nouveau)
        {
            var ancien = row.Classe;
            if (nouveau == ancien) return;

            row.Classe = nouveau;
            if (!await _eleveService.MajClasseAsync(row.Id, nouveau))
            {
                row.Classe = ancien;
                _snackbar.Add("Impossible d'enregistrer la classe.", Severity.Error);
            }
        }

        // ---- Cycle / Niveau (éditables seulement en école publique) ----

        // Cycle du référentiel par son n° ; niveaux (codes) d'un cycle ; cycle d'un niveau.
        private CycleItem? CyclePourNumero(int numero) =>
            _cycles.FirstOrDefault(c => c.Numero == numero);
        private IEnumerable<string> NiveauxDuCycleNumero(int numero) =>
            CyclePourNumero(numero)?.Niveaux.OrderBy(n => n.Ordre).Select(n => n.Code)
            ?? Enumerable.Empty<string>();
        private int? CycleDuNiveau(string code) =>
            _cycles.FirstOrDefault(c => c.Niveaux.Any(n => n.Code == code))?.Numero;

        // Changement de Cycle : on répercute, on cale le Niveau sur le 1er niveau du cycle et on
        // vide la Classe (le backend fait de même). Persistance atomique Cycle+Niveau ; rollback
        // complet si échec.
        private async Task OnCycleChanged(EleveRow row, int nouveauCycle)
        {
            if (nouveauCycle == row.Cycle) return;

            var (ancienCycle, ancienNiveau, ancienneClasse) = (row.Cycle, row.Niveau, row.Classe);

            row.Cycle = nouveauCycle;
            row.Niveau = NiveauxDuCycleNumero(nouveauCycle).FirstOrDefault() ?? string.Empty;
            row.Classe = string.Empty;

            if (!await _eleveService.MajCycleNiveauAsync(row.Id, row.Cycle, row.Niveau))
            {
                (row.Cycle, row.Niveau, row.Classe) = (ancienCycle, ancienNiveau, ancienneClasse);
                _snackbar.Add("Impossible d'enregistrer le cycle.", Severity.Error);
            }
        }

        // Changement de Niveau : on aligne le Cycle sur celui du niveau et on vide la Classe.
        private async Task OnNiveauChanged(EleveRow row, string nouveauNiveau)
        {
            if (nouveauNiveau == row.Niveau) return;

            var (ancienCycle, ancienNiveau, ancienneClasse) = (row.Cycle, row.Niveau, row.Classe);

            row.Niveau = nouveauNiveau;
            row.Cycle = CycleDuNiveau(nouveauNiveau) ?? row.Cycle;
            row.Classe = string.Empty;

            if (!await _eleveService.MajCycleNiveauAsync(row.Id, row.Cycle, row.Niveau))
            {
                (row.Cycle, row.Niveau, row.Classe) = (ancienCycle, ancienNiveau, ancienneClasse);
                _snackbar.Add("Impossible d'enregistrer le niveau.", Severity.Error);
            }
        }

        // ---- Tuteur (correspondant) éditable dans la fiche, persisté dans Pédagogie ----
        private async Task OnTuteurChanged(string champ, string valeur)
        {
            if (_sel is null) return;

            // Sauvegarde pour rollback en cas d'échec.
            var (n, p, t1, t2) = (_sel.TuteurNom, _sel.TuteurPrenom, _sel.TuteurTel1, _sel.TuteurTel2);

            switch (champ)
            {
                case "Nom": _sel.TuteurNom = valeur; break;
                case "Prenom": _sel.TuteurPrenom = valeur; break;
                case "Tel1": _sel.TuteurTel1 = valeur; break;
                case "Tel2": _sel.TuteurTel2 = valeur; break;
            }

            if (!await _eleveService.MajTuteurAsync(
                    _sel.Id, _sel.TuteurNom, _sel.TuteurPrenom, _sel.TuteurTel1, _sel.TuteurTel2))
            {
                (_sel.TuteurNom, _sel.TuteurPrenom, _sel.TuteurTel1, _sel.TuteurTel2) = (n, p, t1, t2);
                _snackbar.Add("Impossible d'enregistrer le tuteur.", Severity.Error);
            }
        }

        // ---- Impression de la Fiche Élève ----
        // On sélectionne l'élève (sa fiche s'affiche), puis on imprime au rendu suivant (le
        // drapeau garantit que la fiche du BON élève est dans le DOM avant window.print).
        private bool _imprimerFicheDemande;

        private void ImprimerFiche(EleveRow row)
        {
            _sel = row;
            _imprimerFicheDemande = true;
        }

        // ---- Actions par ligne (menu 3-points) ----

        // Fiche élève : pas encore de page dédiée -> stub informatif.
        private void VoirFiche(EleveRow row) =>
            _snackbar.Add($"Fiche de {row.Nom} {row.Prenoms} : fonction à venir.", Severity.Info);

        // La classe est déjà modifiable en ligne : on guide l'utilisateur vers la colonne.
        private void AllerClasse(EleveRow row) =>
            _snackbar.Add("Modifie la classe directement dans la colonne « Classe » (Ctrl+' recopie celle du dessus).", Severity.Info);

        // ================== Aperçu + impression de la liste de classe ==================
        // Modal d'aperçu WYSIWYG (feuille A4) affiché à l'écran, puis impression navigateur qui
        // n'imprime QUE la feuille (classe body « lc-print-liste », cf. wwwroot/index.html).
        private bool _apercuOuvert;

        private void OuvrirApercu() => _apercuOuvert = true;
        private void FermerApercu() => _apercuOuvert = false;

        // Lance l'impression réelle : le helper JS isole « .lc-feuille » le temps du print et
        // bascule la page en paysage pour les modèles larges (appel, trombinoscope).
        private async Task ImprimerListeAsync() =>
            await _js.InvokeVoidAsync("lcImprimerListe", ModeleEstPaysage);

        // ---- Modèles de liste : seul le CORPS (et le titre) change ; en-tête officiel, stats
        // G/F/Total et mécanisme d'impression sont communs à tous. Le modèle se choisit dans une
        // déroulante de la barre d'aperçu (front seul, aucune persistance). ----
        public enum ModeleListe { Classe, Appel, Notes, Trombinoscope, Affectes }

        private ModeleListe _modele = ModeleListe.Classe;

        private void OnModeleChanged(ModeleListe m) => _modele = m;

        // Titre du document selon le modèle (la classe est ajoutée après « : » dans le .razor).
        private string ModeleTitre => _modele switch
        {
            ModeleListe.Appel => "LISTE DE PRÉSENCE",
            ModeleListe.Notes => "FICHE DE NOTES",
            ModeleListe.Trombinoscope => "TROMBINOSCOPE",
            ModeleListe.Affectes => "LISTE DES AFFECTÉS",
            _ => "LISTE DE CLASSE",
        };

        // Modèles imprimés en PAYSAGE (grille large) : cahier d'appel hebdomadaire (jours ×
        // créneaux) et trombinoscope (cartes photo). Les autres restent en portrait.
        private bool ModeleEstPaysage =>
            _modele is ModeleListe.Appel or ModeleListe.Trombinoscope;

        // Fiche de notes = Matricule (pour distinguer les homonymes) + 7 colonnes vides que
        // l'enseignant remplit à la main (notes d'évaluations).
        private bool ModeleNotes => _modele == ModeleListe.Notes;

        private const int NbColonnesVides = 7;

        // Cahier d'appel hebdomadaire (LISTE DE PRÉSENCE) : jours × créneaux horaires, calqués
        // sur le modèle Access de Keita. Cases vides à cocher à la main.
        private static readonly string[] JoursAppel =
            { "LUNDI", "MARDI", "MERCREDI", "JEUDI", "VENDREDI" };
        private static readonly string[] CreneauxAppel =
            { "7 à 8", "8 à 9", "9 à 10", "10 à 11", "11 à 12", "14 à 15", "15 à 16", "16 à 17", "17 à 18" };

        // Source du modèle : « Liste des Affectés » ne garde que les élèves affectés (Statut =
        // Aff) ; les autres modèles prennent toute la sélection filtrée. TOUS respectent donc la
        // barre de filtres (via Filtered).
        private IEnumerable<EleveRow> SourceModele =>
            _modele == ModeleListe.Affectes ? Filtered.Where(e => e.Statut == "Aff") : Filtered;

        // Élèves à imprimer = exactement la sélection filtrée, triée Nom puis Prénoms (ordre
        // d'une liste de classe). La numérotation N° suit cet ordre.
        private List<EleveRow> ElevesImpression =>
            SourceModele
                .OrderBy(e => e.Nom, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => e.Prenoms, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        // Comptage par sexe (robuste aux libellés « M »/« F », « Masculin »/« Féminin »,
        // « G »/« F ») : on ne regarde que la 1re lettre.
        private static bool EstFille(string sexe) =>
            !string.IsNullOrWhiteSpace(sexe) && char.ToUpperInvariant(sexe.Trim()[0]) == 'F';
        private static bool EstGarcon(string sexe) =>
            !string.IsNullOrWhiteSpace(sexe) && "MG".Contains(char.ToUpperInvariant(sexe.Trim()[0]));

        private int NbGarcons => ElevesImpression.Count(e => EstGarcon(e.Sexe));
        private int NbFilles => ElevesImpression.Count(e => EstFille(e.Sexe));

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

            // Recale les offsets des colonnes figées (largeurs mesurées) après chaque rendu :
            // filtre, pagination, tri… peuvent changer la largeur des colonnes.
            await _js.InvokeVoidAsync("lcFreeze.apply");

            // Impression de la fiche demandée : la fiche du bon élève est maintenant rendue.
            if (_imprimerFicheDemande)
            {
                _imprimerFicheDemande = false;
                await _js.InvokeVoidAsync("lcImprimerFiche");
            }
        }

        [JSInvokable]
        public async Task CopierCelluleDuHaut(string sourceId, string cibleId, string col)
        {
            if (!Guid.TryParse(sourceId, out var sId) || !Guid.TryParse(cibleId, out var cId))
                return;

            var source = _all.FirstOrDefault(e => e.Id == sId);
            var cible = _all.FirstOrDefault(e => e.Id == cId);
            if (source is null || cible is null || source == cible)
                return;

            switch (col)
            {
                // Statut / Cycle / Niveau : éditables (donc copiables) uniquement en école publique.
                case "Statut":
                    if (_ecolePublique) await OnStatutChanged(cible, source.Statut);
                    break;
                case "Cycle":
                    if (_ecolePublique) await OnCycleChanged(cible, source.Cycle);
                    break;
                case "Niveau":
                    if (_ecolePublique) await OnNiveauChanged(cible, source.Niveau);
                    break;
                case "Classe":
                    // On ne recopie la classe que si elle est valide pour le niveau de la cible.
                    if (ClassesPour(cible.Niveau).Any(c => c.Libelle == source.Classe))
                        await OnClasseChanged(cible, source.Classe);
                    break;
            }

            StateHasChanged();
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
            public int Cycle { get; set; }      // N° de cycle (éditable en école publique)
            public string Niveau { get; set; }  // éditable en école publique (cascade → classes)
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
            public string ImageFile { get; set; }   // photo de l'élève (base64 data URL), éditable via upload
            public string TuteurNom { get; set; }
            public string TuteurPrenom { get; set; }
            public string TuteurTel1 { get; set; }   // tél.
            public string TuteurTel2 { get; set; }   // WhatsApp

            public EleveRow(
                Guid id, int numOrdre, string matricule, string nom, string prenoms,
                int cycle, string niveau, string serie, string classe,
                string statut, string sexe, DateTime? dateNaissance, string lieuNaissance,
                string nationalite, string telephone, bool inscrit, bool actif, string imageFile,
                string tuteurNom, string tuteurPrenom, string tuteurTel1, string tuteurTel2)
            {
                Id = id;
                NumOrdre = numOrdre;
                Matricule = matricule;
                Nom = nom;
                Prenoms = prenoms;
                Cycle = cycle;
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
                ImageFile = imageFile;
                TuteurNom = tuteurNom;
                TuteurPrenom = tuteurPrenom;
                TuteurTel1 = tuteurTel1;
                TuteurTel2 = tuteurTel2;
            }
        }
    }
}
