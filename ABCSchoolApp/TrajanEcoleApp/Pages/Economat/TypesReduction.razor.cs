using App.Infrastructure.Services.Economat;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Economat
{
    public partial class TypesReduction
    {
        [Inject] private ITypeReductionService _typeService { get; set; } = default!;

        private List<TypeRow> _lignes = new();

        // Champ de la ligne d'ajout.
        private string _nouveauLibelle = string.Empty;
        private bool _enCours;

        protected override async Task OnInitializedAsync() => await ChargerAsync();

        private async Task ChargerAsync()
        {
            var types = await _typeService.GetTypesAsync();
            _lignes = types.OrderBy(t => t.Ordre).ThenBy(t => t.Libelle)
                           .Select(t => new TypeRow(t)).ToList();
        }

        private bool Verifier(TypeReductionOpResult result, string messageSucces)
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
            if (string.IsNullOrWhiteSpace(_nouveauLibelle))
            {
                _snackbar.Add("Saisis d'abord le libellé du type.", Severity.Warning);
                return;
            }

            _enCours = true;
            try
            {
                // Ordre = null -> le backend le place en fin de liste ; OK par defaut.
                var result = await _typeService.CreateAsync(_nouveauLibelle.Trim(), null, ok: true);
                if (Verifier(result, $"Type « {_nouveauLibelle.Trim()} » ajouté."))
                {
                    _nouveauLibelle = string.Empty;
                    await ChargerAsync();
                }
            }
            finally { _enCours = false; }
        }

        private async Task EnregistrerAsync(TypeRow row)
        {
            if (!row.EstModifiee) return;

            _enCours = true;
            try
            {
                var result = await _typeService.UpdateAsync(row.VersItem());
                if (Verifier(result, $"Type « {row.Libelle} » enregistré."))
                    row.FigerSnapshot();
                else
                    row.Restaurer();
            }
            finally { _enCours = false; }
        }

        private async Task SupprimerAsync(TypeRow row)
        {
            _enCours = true;
            try
            {
                var result = await _typeService.DeleteAsync(row.Id);
                if (Verifier(result, $"Type « {row.Libelle} » supprimé."))
                    await ChargerAsync();
            }
            finally { _enCours = false; }
        }

        private void Fermer() => _navigation.NavigateTo("/ecole");

        // ------------------- ViewModel mutable de la grille -------------------
        public sealed class TypeRow
        {
            public TypeRow(TypeReductionItem t)
            {
                Id = t.Id; Ordre = t.Ordre; Libelle = t.Libelle; OK = t.OK;
                FigerSnapshot();
            }

            public int Id { get; }
            public int Ordre { get; set; }
            public string Libelle { get; set; }
            public bool OK { get; set; }

            private (int Ordre, string Libelle, bool OK) _snapshot;
            public bool EstModifiee => (Ordre, Libelle, OK) != _snapshot;
            public void FigerSnapshot() => _snapshot = (Ordre, Libelle, OK);
            public void Restaurer() => (Ordre, Libelle, OK) = _snapshot;

            public TypeReductionItem VersItem() => new()
            {
                Id = Id, Ordre = Ordre, Libelle = Libelle, OK = OK,
            };
        }
    }
}
