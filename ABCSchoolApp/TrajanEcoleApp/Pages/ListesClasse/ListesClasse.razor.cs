using System.IO;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using TrajanEcole.Shared.Library.Enums;
using TrajanEcole.Shared.Library.Helpers;
using TrajanEcoleApp.Components;

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
        // _dialogService (IDialogService) et _snackbar sont injectés globalement par _Imports.razor.

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
        private string _fSexe = "Tous";       // "Tous" / "G" (garçons) / "F" (filles)
        private string _fInscrit = "Oui";     // par défaut : seulement les inscrits
        private string _fActif = "Oui";       // par défaut : seulement les actifs
        private string _fMatricule = string.Empty; // filtre matricule national (souple, sans espaces)
        private string _fNumOrdre = string.Empty;  // filtre N° Inscription (contient)

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
                e.Tuteur?.Telephone1 ?? string.Empty, e.Tuteur?.Telephone2 ?? string.Empty,
                e.LV_2 ?? string.Empty, e.Arts ?? string.Empty, e.Red ?? string.Empty)).ToList();
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

        // Matricule national formaté « 22 654 456 M » — helper partagé (Shared.Library), commun
        // aux listes et au reçu de paiement.
        private static string FormatMatricule(string mat) => MatriculeFormatter.Format(mat);

        // Libellé de classe collège reformaté (« 6e1 » -> « 6è 1 ») — helper partagé
        // (Shared.Library), commun aux listes et au reçu.
        private static string FormatClasse(string classe) => ClasseFormatter.Format(classe);

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
                && (_fSexe == "Tous" || (_fSexe == "G" ? EstGarcon(e.Sexe) : EstFille(e.Sexe)))
                && (_fInscrit == "Tous" || (_fInscrit == "Oui") == e.Inscrit)
                && (_fActif == "Tous" || (_fActif == "Oui") == e.Actif)
                && (string.IsNullOrWhiteSpace(_fNumOrdre) || e.NumOrdre.ToString().Contains(_fNumOrdre.Trim()))
                && (string.IsNullOrWhiteSpace(_fMatricule)
                    || Compact(e.Matricule).Contains(Compact(_fMatricule), StringComparison.OrdinalIgnoreCase)))
            // Toujours par ordre alphabétique : Nom croissant, puis Prénoms croissant.
            .OrderBy(e => e.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.Prenoms, StringComparer.CurrentCultureIgnoreCase);

        private void Effacer()
        {
            _fCycleId = _fNiveau = _fClasse = string.Empty;
            _fStatut = _fSexe = "Tous";
            _fInscrit = _fActif = "Oui";   // on revient au défaut (inscrits + actifs)
            _fMatricule = _fNumOrdre = string.Empty;
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
        private void OnFiltreSexeChanged(string v) { _fSexe = v ?? "Tous"; AppliquerFiltre(); }
        private void OnFiltreInscritChanged(string v) { _fInscrit = v ?? "Tous"; AppliquerFiltre(); }
        private void OnFiltreActifChanged(string v) { _fActif = v ?? "Tous"; AppliquerFiltre(); }
        private void OnFiltreMatriculeChanged(string v) { _fMatricule = v ?? string.Empty; AppliquerFiltre(); }
        private void OnFiltreNumOrdreChanged(string v) { _fNumOrdre = v ?? string.Empty; AppliquerFiltre(); }

        // Comparaison souple du matricule : on ignore les espaces (« 22 654 456 M » ~ « 22654456M »).
        private static string Compact(string s) => (s ?? string.Empty).Replace(" ", string.Empty);

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

        // LV2 / Arts : déroulantes éditables (toutes écoles, aucun impact échéancier), persistées
        // directement sur dbo.Eleves. Rollback de la cellule si l'appel échoue.
        private async Task OnLv2Changed(EleveRow row, string nouveau)
        {
            var ancien = row.LV_2;
            if (nouveau == ancien) return;

            row.LV_2 = nouveau;
            if (!await _eleveService.MajLv2Async(row.Id, nouveau ?? string.Empty))
            {
                row.LV_2 = ancien;
                _snackbar.Add("Impossible d'enregistrer la LV2.", Severity.Error);
            }
        }

        private async Task OnArtsChanged(EleveRow row, string nouveau)
        {
            var ancien = row.Arts;
            if (nouveau == ancien) return;

            row.Arts = nouveau;
            if (!await _eleveService.MajArtsAsync(row.Id, nouveau ?? string.Empty))
            {
                row.Arts = ancien;
                _snackbar.Add("Impossible d'enregistrer les Arts.", Severity.Error);
            }
        }

        private async Task OnRedChanged(EleveRow row, string nouveau)
        {
            var ancien = row.Red;
            if (nouveau == ancien) return;

            row.Red = nouveau;
            if (!await _eleveService.MajRedAsync(row.Id, nouveau ?? string.Empty))
            {
                row.Red = ancien;
                _snackbar.Add("Impossible d'enregistrer Red.", Severity.Error);
            }
        }

        // ---- Corrections « Activer colonne » : édition inline persistée par cellule (rollback si échec) ----
        // Cycle SEUL : ne touche NI le niveau NI la classe (correction d'incohérence, pas de cascade).
        private async Task OnCycleCorrige(EleveRow row, int nouveau)
        {
            if (nouveau == row.Cycle) return;
            var ancien = row.Cycle;
            row.Cycle = nouveau;
            if (!await _eleveService.MajCycleSeulAsync(row.Id, nouveau))
            {
                row.Cycle = ancien;
                _snackbar.Add("Impossible d'enregistrer le cycle.", Severity.Error);
            }
        }

        private async Task OnDateNaissChanged(EleveRow row, DateTime? nouvelle)
        {
            if (nouvelle == row.DateNaissance) return;
            var ancienne = row.DateNaissance;
            row.DateNaissance = nouvelle;
            if (!await _eleveService.MajDateNaissanceAsync(row.Id, nouvelle))
            {
                row.DateNaissance = ancienne;
                _snackbar.Add("Impossible d'enregistrer la date de naissance.", Severity.Error);
            }
        }

        private async Task OnLieuNaissChanged(EleveRow row, string nouveau)
        {
            if (nouveau == row.LieuNaissance) return;
            var ancien = row.LieuNaissance;
            row.LieuNaissance = nouveau;
            if (!await _eleveService.MajLieuNaissanceAsync(row.Id, nouveau ?? string.Empty))
            {
                row.LieuNaissance = ancien;
                _snackbar.Add("Impossible d'enregistrer le lieu de naissance.", Severity.Error);
            }
        }

        private async Task OnNationaliteChanged(EleveRow row, string nouveau)
        {
            if (nouveau == row.Nationalite) return;
            var ancien = row.Nationalite;
            row.Nationalite = nouveau;
            if (!await _eleveService.MajNationaliteAsync(row.Id, nouveau ?? string.Empty))
            {
                row.Nationalite = ancien;
                _snackbar.Add("Impossible d'enregistrer la nationalité.", Severity.Error);
            }
        }

        private async Task OnTelephoneChanged(EleveRow row, string nouveau)
        {
            if (nouveau == row.Telephone) return;
            var ancien = row.Telephone;
            row.Telephone = nouveau;
            if (!await _eleveService.MajTelephoneAsync(row.Id, nouveau ?? string.Empty))
            {
                row.Telephone = ancien;
                _snackbar.Add("Impossible d'enregistrer le téléphone.", Severity.Error);
            }
        }

        // ================== Opérations en masse (panneau « Go » du bas de grille) ==================
        // Agit sur les élèves ACTUELLEMENT filtrés (Filtered). Persisté côté Pédagogie en une
        // transaction (PUT /eleves/operations), puis rechargement de la grille.
        private string _bulkOp = string.Empty;      // opération choisie (clé)
        private string _bulkValeur = string.Empty;  // valeur pour Copier LV_2 / Arts / Série
        private bool _bulkEnCours;                   // désactive « Go » pendant l'appel

        // Colonne « activée » pour édition inline de correction ("" = aucune → colonnes read-only).
        private string _colonneActive = string.Empty;
        private void OnColonneActiveChanged(string col) => _colonneActive = col ?? string.Empty;

        // Option « vider la colonne » : sentinel affiché (≠ chaîne vide, sinon la garde « valeur
        // obligatoire » la refuserait) ; converti en "" au moment de l'envoi.
        private const string ValeurVide = "(Vide)";

        private static readonly string[] OptionsLv2 = { "Allemand", "Espagnol", ValeurVide };
        private static readonly string[] OptionsArts = { "Arts Plastiques", "Musique", ValeurVide };
        private static readonly string[] OptionsSerie = { "A", "A1", "A2", "C", "D", "x" };
        private static readonly string[] OptionsRed = { "R", "NR" };

        // Les opérations « Copier … » exigent une valeur (2e déroulante).
        private bool BulkNeedsValeur => _bulkOp is "lv2" or "arts" or "serie" or "red";

        private IEnumerable<string> BulkValeurOptions => _bulkOp switch
        {
            "lv2" => OptionsLv2,
            "arts" => OptionsArts,
            "serie" => OptionsSerie,
            "red" => OptionsRed,
            _ => Enumerable.Empty<string>(),
        };

        // Changement d'action : on réinitialise la valeur (une valeur LV2 n'a pas de sens pour Série).
        private void OnBulkOpChanged(string op)
        {
            _bulkOp = op ?? string.Empty;
            _bulkValeur = string.Empty;
        }

        // Libellé lisible pour la confirmation.
        private string BulkLabel(string op) => op switch
        {
            "prenom-minuscule" => "Minuscule (Prénoms)",
            "prenom-majuscule" => "Majuscule (Prénoms)",
            "inscrire" => "Inscrire tous",
            "desinscrire" => "Désinscrire tous",
            "lv2" => $"Copier LV_2 = « {_bulkValeur} »",
            "arts" => $"Copier Arts = « {_bulkValeur} »",
            "serie" => $"Copier Série = « {_bulkValeur} »",
            "red" => $"Copier Red = « {_bulkValeur} »",
            _ => op,
        };

        private async Task ExecuterOperationAsync()
        {
            if (string.IsNullOrWhiteSpace(_bulkOp))
            {
                _snackbar.Add("Choisis une action.", Severity.Info);
                return;
            }
            if (BulkNeedsValeur && string.IsNullOrWhiteSpace(_bulkValeur))
            {
                _snackbar.Add("Choisis une valeur.", Severity.Info);
                return;
            }
            // Gating client (le serveur le refait) : inscrire/désinscrire réservé aux écoles publiques.
            if (_bulkOp is "inscrire" or "desinscrire" && !_ecolePublique)
            {
                _snackbar.Add("Réservé aux écoles publiques.", Severity.Warning);
                return;
            }

            var ids = Filtered.Select(e => e.Id).ToList();
            if (ids.Count == 0)
            {
                _snackbar.Add("Aucun élève affiché.", Severity.Info);
                return;
            }

            // Avertissement Oui/Non (« Non » focalisé par défaut). Backdrop non cliquable : on
            // ferme explicitement. Une action de masse ne se déclenche pas par mégarde.
            var parameters = new DialogParameters
            {
                { nameof(ConfirmerAction.Titre), "Action en masse" },
                { nameof(ConfirmerAction.Message),
                    $"Appliquer « {BulkLabel(_bulkOp)} » à {ids.Count} élève(s) affiché(s) ?" },
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, BackdropClick = false };
            var dialog = await _dialogService.ShowAsync<ConfirmerAction>(null, parameters, options);
            var result = await dialog.Result;
            if (result is null || result.Canceled) return;

            _bulkEnCours = true;
            StateHasChanged();   // affiche la jauge/spinner AVANT l'appel (qui peut être long)
            try
            {
                // « (Vide) » = vider la colonne → on envoie une chaîne vide.
                var valeur = BulkNeedsValeur
                    ? (_bulkValeur == ValeurVide ? string.Empty : _bulkValeur)
                    : null;
                var res = await _eleveService.OperationsEnMasseAsync(ids, _bulkOp, valeur);
                if (!res.IsSuccessful)
                {
                    _snackbar.Add($"Échec : {res.Error}", Severity.Error);
                    return;
                }
                // Recharge depuis Pédagogie pour refléter les changements (prénoms, statuts, LV2…).
                await ChargerElevesAsync();
                SelectionnerPremierDePage();
                _snackbar.Add($"{res.Count} élève(s) mis à jour.", Severity.Success);
            }
            finally
            {
                _bulkEnCours = false;
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

        // Compteurs du PIED DE GRILLE (sur la sélection affichée = Filtered), indépendants du
        // modèle d'impression choisi dans l'aperçu.
        private int GrilleGarcons => Filtered.Count(e => EstGarcon(e.Sexe));
        private int GrilleFilles => Filtered.Count(e => EstFille(e.Sexe));

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
                // LV2 / Arts : éditables pour toutes les écoles, donc toujours copiables.
                case "LV_2":
                    await OnLv2Changed(cible, source.LV_2);
                    break;
                case "Arts":
                    await OnArtsChanged(cible, source.Arts);
                    break;
                case "Red":
                    await OnRedChanged(cible, source.Red);
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
            public DateTime? DateNaissance { get; set; }
            public string LieuNaissance { get; set; }
            public string Nationalite { get; set; }
            public string Telephone { get; set; }
            public bool Inscrit { get; }
            public bool Actif { get; }
            public string ImageFile { get; set; }   // photo de l'élève (base64 data URL), éditable via upload
            public string TuteurNom { get; set; }
            public string TuteurPrenom { get; set; }
            public string TuteurTel1 { get; set; }   // tél.
            public string TuteurTel2 { get; set; }   // WhatsApp
            public string LV_2 { get; set; }         // langue vivante 2 (Allemand / Espagnol), éditable
            public string Arts { get; set; }         // Arts Plastiques / Musique, éditable
            public string Red { get; set; }          // R / NR (redoublant), éditable

            public EleveRow(
                Guid id, int numOrdre, string matricule, string nom, string prenoms,
                int cycle, string niveau, string serie, string classe,
                string statut, string sexe, DateTime? dateNaissance, string lieuNaissance,
                string nationalite, string telephone, bool inscrit, bool actif, string imageFile,
                string tuteurNom, string tuteurPrenom, string tuteurTel1, string tuteurTel2,
                string lv2, string arts, string red)
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
                LV_2 = lv2;
                Arts = arts;
                Red = red;
            }
        }
    }
}
