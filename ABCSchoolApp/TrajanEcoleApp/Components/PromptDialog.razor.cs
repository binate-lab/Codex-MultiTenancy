using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Components
{
    public partial class PromptDialog
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
        [Parameter] public string Title { get; set; } = "Raison";
        [Parameter] public string Label { get; set; } = "Raison";
        [Parameter] public string ButtonText { get; set; } = "Confirmer";
        [Parameter] public Color Color { get; set; } = Color.Primary;
        [Parameter] public string InputIcon { get; set; } = Icons.Material.Filled.Edit;

        private string _valeur = string.Empty;

        private void Cancel() => MudDialog.Cancel();
        private void Confirmer() => MudDialog.Close(DialogResult.Ok(_valeur));
    }
}
