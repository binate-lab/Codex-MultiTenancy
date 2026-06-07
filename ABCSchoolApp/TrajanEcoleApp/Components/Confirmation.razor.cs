using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Components
{
    public partial class Confirmation
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
        [Parameter]
        public string Title { get; set; }
        [Parameter]
        public string Message { get; set; }
        [Parameter]
        public string ButtonText { get; set; }
        [Parameter]
        public Color Color { get; set; }
        [Parameter]
        public string InputIcon { get; set; }

        private void Confirmed()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Cancel()
        {
            MudDialog?.Cancel();
        }
    }
}
