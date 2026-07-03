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
        // MatriculeInterne de l'ecole + 1 (reste editable).
        private async Task AppliquerContexteEcoleAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var codeEts = authState.User.FindFirst("school")?.Value ?? string.Empty;
            Eleve.CodeEts = codeEts;

            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                Eleve.MatriculeInterne = await _inscriptionService.GetNextMatriculeInterneAsync(codeEts);
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
                if (Eleve.MatriculeInterne is null && !string.IsNullOrWhiteSpace(Eleve.CodeEts))
                {
                    Eleve.MatriculeInterne = await _inscriptionService.GetNextMatriculeInterneAsync(Eleve.CodeEts);
                }

                var result = await _eleveService.CreateAsync(new CreateEleveRequest { EleveDto = Eleve });
                if (result.IsSuccessful)
                {
                    // #3 : on NE vide PAS les controles apres enregistrement ; seul le
                    // bouton « Nouveau » reinitialise le formulaire. Le N° Inscription reste
                    // donc affiche : c'est celui de l'eleve qu'on vient d'enregistrer.
                    _snackbar.Add($"Élève enregistré — N° Inscription : {Eleve.MatriculeInterne}.", Severity.Success);
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
            Eleve.NumeroMatricule = Maj(Eleve.NumeroMatricule);
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

        // #6 : en quittant le champ Matricule National, retire les espaces et met en
        // majuscules (ex. « 24 568 951 n » -> « 24568951N »).
        private void NettoyerMatricule()
        {
            if (!string.IsNullOrEmpty(Eleve.NumeroMatricule))
                Eleve.NumeroMatricule = Eleve.NumeroMatricule.Replace(" ", string.Empty).ToUpperInvariant();
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
