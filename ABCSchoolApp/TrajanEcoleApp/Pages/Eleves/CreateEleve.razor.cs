using App.Infrastructure.Validators;
using System.Globalization;
using App.Infrastructure.Services.Eleves;
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

        // Référence .NET passée au script clavier (index.html) pour les raccourcis.
        private DotNetObjectReference<CreateEleve> _dotNetRef;

        // Niveaux valides cote domaine (Eleves.Api/ValueObjects/NiveauId).
        private static readonly string[] _niveaux =
            ["6e", "5e", "4e", "3e", "2nde", "1ere", "Tle", "BT"];

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
        // (CodeEts du claim + N° Inscription auto).
        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _anneeEnCours = annee.Data.Libelle;
                Eleve.AnneeScolaire = _anneeEnCours;
            }

            await AppliquerContexteEcoleAsync();
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

                var result = await _eleveService.CreateAsync(new CreateEleveRequest { EleveDto = Eleve });
                if (result.IsSuccessful)
                {
                    // #3 : on NE vide PAS les controles apres enregistrement ; seul le
                    // bouton « Nouveau » reinitialise le formulaire.
                    _snackbar.Add($"Eleve cree (Id : {result.Id}).", Severity.Success);
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
            NettoyerClasse();   // #7 : cycle 1 minuscules / cycle 2 MAJUSCULES
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

        // #7 : Classe a la sortie du champ — cycle 1 en minuscules, cycle 2 en MAJUSCULES.
        private void NettoyerClasse() => Eleve.Classe = Eleve.Cycle == 2 ? Maj(Eleve.Classe) : Min(Eleve.Classe);

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

        // minuscules (accents geres) ; conserve la chaine vide telle quelle.
        private static string Min(string s)
            => string.IsNullOrWhiteSpace(s) ? (s ?? string.Empty) : s.ToLowerInvariant();

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
