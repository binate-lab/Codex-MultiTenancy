using App.Infrastructure.Validators;
using System.Globalization;
using App.Infrastructure.Services.Eleves;
using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace TrajanEcoleApp.Pages.Eleves
{
    public partial class CreateEleve : IAsyncDisposable
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private AuthenticationStateProvider _authProvider { get; set; }
        [Inject] private IInscriptionService _inscriptionService { get; set; }
        [Inject] private IStructureService _structureService { get; set; }

        // Référence .NET passée au script clavier (index.html) pour les raccourcis.
        private DotNetObjectReference<CreateEleve> _dotNetRef;

        // Référentiel structures de l'école (module Structures de pedagogie-api) :
        // cycles→niveaux + classes ouvertes pour l'année en cours. Les sélecteurs
        // Cycle/Niveau/Classe sont alimentés par CE référentiel, plus par des listes figées.
        private IReadOnlyList<CycleItem> _cyclesRef = new List<CycleItem>();
        private IReadOnlyList<ClasseItem> _classesRef = new List<ClasseItem>();

        // Niveaux du cycle sélectionné, puis classes du niveau sélectionné (cascade).
        private IEnumerable<NiveauItem> NiveauxDuCycle =>
            _cyclesRef.FirstOrDefault(c => c.Numero == Eleve.Cycle)?.Niveaux ?? new List<NiveauItem>();

        private IEnumerable<ClasseItem> ClassesDuNiveau =>
            _classesRef.Where(c => c.NiveauCode == Eleve.Niveau);

        private EleveRequestDto Eleve { get; set; } = new()
        {
            AnneeScolaire = AnneeScolaireRepli,
            Cycle = 1
        };

        private MudForm _form = default!;
        private readonly CreateEleveRequestValidator _validator = new();
        private bool _isSaving;

        // Annee scolaire en cours (bandeau + DTO) : servie par l'API annees scolaires,
        // avec repli statique si elle est indisponible a l'ouverture.
        private const string AnneeScolaireRepli = "2025-2026";
        private string _anneeEnCours = AnneeScolaireRepli;

        // #5 : a l'ouverture, applique l'annee en cours puis le contexte ecole
        // (CodeEts du claim + N° Inscription auto), et charge le referentiel structures.
        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _anneeEnCours = annee.Data.Libelle;
                Eleve.AnneeScolaire = _anneeEnCours;
            }

            await AppliquerContexteEcoleAsync();
            await ChargerReferentielAsync();
        }

        // Charge cycles→niveaux + classes de l'annee en cours depuis le module Structures.
        private async Task ChargerReferentielAsync()
        {
            _cyclesRef = await _structureService.GetCyclesAsync();
            _classesRef = await _structureService.GetClassesAsync(_anneeEnCours);

            if (_cyclesRef.Count == 0)
            {
                _snackbar.Add(
                    "Aucune structure pédagogique configurée pour cette école — menu Structure → Cycles, niveaux & classes.",
                    Severity.Warning);
            }
        }

        // Cascade : changer de cycle invalide le niveau (et la classe) s'ils n'en font plus partie.
        private void OnCycleChange()
        {
            if (!NiveauxDuCycle.Any(n => n.Code == Eleve.Niveau))
            {
                Eleve.Niveau = string.Empty;
                Eleve.Classe = string.Empty;
            }

            // Cycle 1 (collège) : pas de séries -> Série = « x » (sans objet). Les séries
            // ne concernent que le cycle 2 (lycée), où l'utilisateur choisit librement.
            if (Eleve.Cycle == 1)
            {
                Eleve.Serie = "x";
            }
        }

        // Cascade : changer de niveau invalide la classe si elle n'en fait plus partie.
        private void OnNiveauChange()
        {
            if (!ClassesDuNiveau.Any(c => c.Libelle == Eleve.Classe))
            {
                Eleve.Classe = string.Empty;
            }
        }

        // #5 : CodeEts vient du JWT (claim « school », pose au clic sur la carte ecole) et
        // remplit le champ cache Eleve.CodeEts ; pre-remplit le N° Inscription = dernier
        // NumOrdre de l'ecole + 1 (reste editable).
        private async Task AppliquerContexteEcoleAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var codeEts = authState.User.FindFirst("school")?.Value ?? string.Empty;
            Eleve.CodeEts = codeEts;

            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                Eleve.NumOrdre = await _inscriptionService.GetNextNumOrdreAsync(codeEts);
            }
        }

        private async Task SubmitAsync()
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }

            // #4 : le Correspondant n'est plus obligatoire — un eleve est enregistrable
            // avec seulement Matricule National + Nom + Prenoms ; le reste (parents inclus)
            // pourra etre complete plus tard.

            _isSaving = true;
            try
            {
                // #2 : normalise la casse juste avant l'envoi.
                Normaliser();

                // N° Inscription : si le pré-remplissage a échoué à l'ouverture (Scolarite.Api
                // indisponible), on retente ici pour que le numéro envoyé — et affiché après
                // enregistrement — soit toujours celui de l'élève saisi (repris ensuite dans /scolarites).
                if (Eleve.NumOrdre is null && !string.IsNullOrWhiteSpace(Eleve.CodeEts))
                {
                    Eleve.NumOrdre = await _inscriptionService.GetNextNumOrdreAsync(Eleve.CodeEts);
                }

                var result = await _eleveService.CreateAsync(new CreateEleveRequest { EleveDto = Eleve });

                // Reapplique le format d'affichage du matricule (Normaliser l'a depouille).
                if (!string.IsNullOrEmpty(Eleve.NumeroMatricule))
                    Eleve.NumeroMatricule = FormaterMatricule(Eleve.NumeroMatricule);

                if (result.IsSuccessful)
                {
                    // #3 : on NE vide PAS les controles apres enregistrement ; seul le
                    // bouton « Nouveau » reinitialise le formulaire. Le N° Inscription affiche
                    // est le DEFINITIF attribue par Pedagogie (unique par ecole) : il peut
                    // differer du pre-rempli en cas de saisies simultanees.
                    if (result.NumOrdre > 0)
                    {
                        Eleve.NumOrdre = result.NumOrdre;
                    }
                    _snackbar.Add($"Élève enregistré — N° Inscription : {Eleve.NumOrdre}.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Echec de la creation : {result.Error}", Severity.Error);
                }
            }
            finally
            {
                _isSaving = false;
            }
        }

        // #2 : normalise la casse avant envoi. Par defaut MAJUSCULES ; exceptions :
        // prenoms (eleve + parents) en « Nom Propre » ; Classe selon le cycle (1=min, 2=MAJ) ;
        // Nationalite avec seulement la 1re lettre en majuscule.
        private void Normaliser()
        {
            // Forme canonique envoyee au backend : sans espaces (« 24568951N »),
            // le format d'affichage groupe est reapplique apres l'enregistrement.
            Eleve.NumeroMatricule = Maj(Eleve.NumeroMatricule).Replace(" ", string.Empty);
            Eleve.Nom = Maj(Eleve.Nom);
            Eleve.Prenom = NomPropre(Eleve.Prenom);
            // La classe vient du référentiel structures (sélecteur) : on la garde telle
            // quelle pour qu'elle corresponde exactement à la classe configurée (#7 abandonné).
            Eleve.LieuDeNaissance = Maj(Eleve.LieuDeNaissance);
            Eleve.BureauEtatCivil = Maj(Eleve.BureauEtatCivil);
            Eleve.SousPrefecture = Maj(Eleve.SousPrefecture);
            Eleve.NumExtrait = Maj(Eleve.NumExtrait);
            Eleve.DateExtrait = Maj(Eleve.DateExtrait);
            Eleve.Nationalite = Capitaliser(Eleve.Nationalite);
            Eleve.TransfertOuReaffect = Maj(Eleve.TransfertOuReaffect);
            Eleve.ClassePrecedente = Maj(Eleve.ClassePrecedente);
            Eleve.EtsOrigine = Maj(Eleve.EtsOrigine);

            NormaliserParent(Eleve.Pere);
            NormaliserParent(Eleve.Mere);
            NormaliserParent(Eleve.Tuteur);
        }

        // #6 : en quittant le champ Matricule National, majuscules puis format d'affichage
        // « 24 568 951 N » (chiffres groupés par 3 depuis la droite, lettre détachée).
        // La forme canonique SANS espaces est retablie dans Normaliser() avant l'envoi.
        private async Task NettoyerMatricule()
        {
            if (string.IsNullOrEmpty(Eleve.NumeroMatricule))
                return;

            // Forme canonique (sans espaces, majuscules) pour l'analyse.
            var brut = Eleve.NumeroMatricule.Replace(" ", string.Empty).ToUpperInvariant();
            var chiffres = new string(brut.TakeWhile(char.IsDigit).ToArray());
            var suffixe = brut[chiffres.Length..];

            // Auto-cle : des que les 8 chiffres sont saisis, on calcule la lettre de cle.
            // Suffixe absent -> on complete silencieusement ; suffixe incoherent -> on corrige.
            if (chiffres.Length == 8)
            {
                var cle = MatriculeCle.Cle(chiffres).ToString();
                if (suffixe.Length == 0)
                {
                    brut = chiffres + cle;
                }
                else if (!suffixe.Equals(cle, StringComparison.OrdinalIgnoreCase))
                {
                    brut = chiffres + cle;
                    _snackbar.Add($"Clé de contrôle corrigée : {suffixe} → {cle}.", Severity.Info);
                }
            }

            Eleve.NumeroMatricule = FormaterMatricule(brut);

            // Controle en direct du doublon : on interroge Pedagogie avec la forme canonique
            // (sans espaces, majuscules) — celle qui sera stockee. Si deja pris dans l'ecole,
            // on previent tout de suite (la garde definitive reste la creation).
            var canonique = Eleve.NumeroMatricule.Replace(" ", string.Empty).ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(Eleve.CodeEts)
                && await _eleveService.MatriculeExisteAsync(Eleve.CodeEts, canonique))
            {
                _snackbar.Add(
                    $"Risque de doublon : le matricule national « {Eleve.NumeroMatricule} » est déjà utilisé par un élève de cette école.",
                    Severity.Warning);
            }
        }

        // Bouton « Générer » : matricule aléatoire valide (8 chiffres + clé) et unique dans
        // l'école. Réutilise le contrôle MatriculeExiste pour éviter un doublon (test/démo ou
        // numérotation interne — un vrai matricule national se SAISIT). Se déclenche sur clic.
        private async Task GenererMatricule()
        {
            // Préfixe = 2 derniers chiffres de l'année de fin de l'année scolaire (« 2025-2026 » → « 26 »).
            var candidat = MatriculeCle.GenererPourAnnee(Eleve.AnneeScolaire);

            if (!string.IsNullOrWhiteSpace(Eleve.CodeEts))
            {
                for (var essai = 0; essai < 20
                        && await _eleveService.MatriculeExisteAsync(Eleve.CodeEts, candidat); essai++)
                {
                    candidat = MatriculeCle.GenererPourAnnee(Eleve.AnneeScolaire);
                }
            }

            Eleve.NumeroMatricule = FormaterMatricule(candidat);
            _snackbar.Add($"Matricule généré : {Eleve.NumeroMatricule}", Severity.Success);
        }

        // « 24568951n » / « 24 568 951 N » -> « 24 568 951 N ».
        private static string FormaterMatricule(string s)
        {
            var brut = s.Replace(" ", string.Empty).ToUpperInvariant();
            var chiffres = new string(brut.TakeWhile(char.IsDigit).ToArray());
            var suffixe = brut[chiffres.Length..];

            var groupes = new List<string>();
            for (var fin = chiffres.Length; fin > 0; fin -= 3)
            {
                var debut = Math.Max(0, fin - 3);
                groupes.Insert(0, chiffres[debut..fin]);
            }

            var format = string.Join(" ", groupes);
            return suffixe.Length == 0 ? format
                 : format.Length == 0 ? suffixe
                 : $"{format} {suffixe}";
        }

        // #5 : Nom en MAJUSCULES a la sortie du champ.
        private void NettoyerNom() => Eleve.Nom = Maj(Eleve.Nom);

        // #6 : Prenoms en « Nom Propre » a la sortie du champ
        // (coulibaly daouda alassane -> Coulibaly Daouda Alassane).
        private void NettoyerPrenom() => Eleve.Prenom = NomPropre(Eleve.Prenom);

        private static void NormaliserParent(ParentRequestDto p)
        {
            if (p is null) return;
            p.Nom = Maj(p.Nom);
            p.Prenom = NomPropre(p.Prenom);
            p.Profession = Maj(p.Profession);
            p.Fonction = Maj(p.Fonction);
        }

        // MAJUSCULES (accents geres) ; conserve la chaine vide telle quelle.
        private static string Maj(string s)
            => string.IsNullOrWhiteSpace(s) ? (s ?? string.Empty) : s.ToUpperInvariant();

        // 1re lettre de chaque mot en majuscule, reste en minuscule (jean-marc → Jean-Marc).
        private static string NomPropre(string s)
            => string.IsNullOrWhiteSpace(s) ? (s ?? string.Empty)
               : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());

        // Seulement la 1re lettre en majuscule, reste en minuscule (ivoirienne → Ivoirienne).
        private static string Capitaliser(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s ?? string.Empty;
            var t = s.Trim();
            return char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant();
        }

        // Fermer : on revient à l'espace école (page principale ORION DALOA),
        // pas à l'accueil MainLayout.
        private void Cancel()
        {
            _navigation.NavigateTo("/ecole");
        }

        // Nouveau : vide tous les contrôles pour une nouvelle saisie. Le nouveau
        // EleveRequestDto restaure toutes les valeurs par defaut (Nationalite, Red, Statut,
        // Regime, langues, Serie, parents, etc.) via les initialiseurs du DTO.
        private async Task Nouveau()
        {
            // ResetAsync ecrit null/defaut CLR a travers les bindings : il doit passer sur
            // l'ANCIEN DTO (jete ensuite), jamais apres l'assignation du neuf — sinon il
            // ecrase les valeurs par defaut (Cycle, DispenseEps, Red, Regime, langues...)
            // et l'enregistrement suivant part en 500 cote pedagogie-api.
            if (_form is not null)
            {
                await _form.ResetAsync();
            }

            Eleve = new EleveRequestDto { AnneeScolaire = _anneeEnCours, Cycle = 1 };

            // #5 : reapplique CodeEts + prochain N° Inscription pour la nouvelle saisie.
            await AppliquerContexteEcoleAsync();
        }

        // Branche les raccourcis clavier (Entrée→champ suivant, Ctrl+S/F2→Saisir-Modifier,
        // F8→Nouveau) une fois le DOM rendu.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("trajanCreateEleve.register", _dotNetRef);
            }
        }

        // Appelée depuis le script clavier (Ctrl+S / F2).
        [JSInvokable]
        public async Task SaveFromShortcut()
        {
            await SubmitAsync();
            StateHasChanged();
        }

        // Appelée depuis le script clavier (F8).
        [JSInvokable]
        public async Task NewFromShortcut()
        {
            await Nouveau();
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            // Retire d'abord le listener clavier global (await) PUIS libère la référence .NET :
            // sinon une touche pendant le teardown invoque une ref deja disposee -> JSInterop
            // « GetObjectReference : no tracked object with id ». JSDisconnectedException est
            // avale (page en cours de fermeture / circuit coupe).
            try { await JS.InvokeVoidAsync("trajanCreateEleve.unregister"); }
            catch (JSDisconnectedException) { }
            _dotNetRef?.Dispose();
        }
    }
}
