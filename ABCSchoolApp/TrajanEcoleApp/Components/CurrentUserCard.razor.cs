using App.Infrastructure.Extensions;

namespace TrajanEcoleApp.Components
{
    public partial class CurrentUserCard
    {
        private string Firstname { get; set; }
        private string Lastname { get; set; }
        private char FirstLetterOfFirstname { get; set; }
        private string Email { get; set; }
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await SetCurrentUserDetails();
            _isLoading = false;
        }

        private async Task SetCurrentUserDetails()
        {
            var state = await _applicationStateProvider.GetAuthenticationStateAsync();

            var user = state.User;

            Firstname = user.GetFirstname();
            Lastname = user.GetLastname();
            Email = user.GetEmail();

            if (Firstname.Length > 0)
            {
                FirstLetterOfFirstname = Firstname[0];
            }
        }
    }
}
