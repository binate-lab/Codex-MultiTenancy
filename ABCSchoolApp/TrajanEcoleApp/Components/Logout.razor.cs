using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Components
{
    public partial class Logout
    {
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }
        [Parameter]
        public string Title { get; set; }
        [Parameter]
        public string ConfirmationMessage { get; set; }
        [Parameter]
        public string ButtonText { get; set; }
        [Parameter]
        public Color Color { get; set; }

        private async Task LogoutAsync()
        {
            var result = await _tokenService.LogoutAsync();
            if (result.IsSuccessful)
            {
                _navigation.NavigateTo("/");
                MudDialog.Close(DialogResult.Ok(true));
            }
        }

        private void Cancel()
        {
            MudDialog?.Cancel();
        }
    }
}
