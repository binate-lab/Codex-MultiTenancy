using App.Infrastructure.Validators;
using MudBlazor;
using TrajanEcole.Shared.Library.Models.Requests.Eleves;

namespace TrajanEcoleApp.Pages.Eleves
{
    public partial class CreateEleve
    {
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

        private async Task SubmitAsync()
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }

            _isSaving = true;
            try
            {
                var result = await _eleveService.CreateAsync(new CreateEleveRequest { EleveDto = Eleve });
                if (result.IsSuccessful)
                {
                    _snackbar.Add($"Eleve cree (Id : {result.Id}).", Severity.Success);
                    Eleve = new EleveRequestDto { AnneeScolaire = "2025-2026", Cycle = 1 };
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

        private void Cancel()
        {
            _navigation.NavigateTo("/");
        }
    }
}
