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
    public partial class CreateEleve : IDisposable
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
            AnneeScolaire = "2025-2026",
            Cycle = 1
        };

        private MudForm _form = default!;
        private readonly CreateEleveRequestValidator _validator = new();
        private bool _isSaving;

        // #5 : a l'ouverture, applique le contexte ecole (CodeEts du claim + N° Inscription auto).
        protected override async Task OnInitializedAsync()
        {
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

        // #2 : normalise la casse avant envoi — tous les champs texte en MAJUSCULES,
        // sauf les prenoms (eleve + parents) en « Nom Propre » (1re lettre de chaque mot).
        private void Normaliser()
        {
            Eleve.NumeroMatricule = Maj(Eleve.NumeroMatricule);
            Eleve.Nom = Maj(Eleve.Nom);
            Eleve.Prenom = NomPropre(Eleve.Prenom);
            Eleve.Classe = Maj(Eleve.Classe);
            Eleve.LieuDeNaissance = Maj(Eleve.LieuDeNaissance);
            Eleve.BureauEtatCivil = Maj(Eleve.BureauEtatCivil);
            Eleve.SousPrefecture = Maj(Eleve.SousPrefecture);
            Eleve.NumExtrait = Maj(Eleve.NumExtrait);
            Eleve.DateExtrait = Maj(Eleve.DateExtrait);
            Eleve.Nationalite = Maj(Eleve.Nationalite);
            Eleve.TransfertOuReaffect = Maj(Eleve.TransfertOuReaffect);
            Eleve.ClassePrecedente = Maj(Eleve.ClassePrecedente);
            Eleve.EtsOrigine = Maj(Eleve.EtsOrigine);

            NormaliserParent(Eleve.Pere);
            NormaliserParent(Eleve.Mere);
            NormaliserParent(Eleve.Tuteur);
        }

        private static void NormaliserParent(ParentRequestDto p)
        {
            if (p is null) return;
            p.Nom = Maj(p.Nom);
            p.Prenom = NomPropre(p.Prenom);
            p.Profession = Maj(p.Profession);
            p.Fonction = Maj(p.Fonction);
        }

        // MAJUSCULES (accents geres) ; conserve la chaine vide telle quelle.
        private static string Maj(string? s)
            => string.IsNullOrWhiteSpace(s) ? (s ?? string.Empty) : s.ToUpperInvariant();

        // 1re lettre de chaque mot en majuscule, reste en minuscule (jean-marc → Jean-Marc).
        private static string NomPropre(string? s)
            => string.IsNullOrWhiteSpace(s) ? (s ?? string.Empty)
               : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());

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
            Eleve = new EleveRequestDto { AnneeScolaire = "2025-2026", Cycle = 1 };

            if (_form is not null)
            {
                await _form.ResetAsync();
            }

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

        public void Dispose()
        {
            // Désenregistre le listener clavier global puis libère la référence .NET.
            _ = JS.InvokeVoidAsync("trajanCreateEleve.unregister");
            _dotNetRef?.Dispose();
        }
    }
}
