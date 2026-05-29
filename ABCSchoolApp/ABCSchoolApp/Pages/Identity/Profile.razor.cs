using ABCShared.Library.Models.Requests.Identity;
using App.Infrastructure.Extensions;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace ABCSchoolApp.Pages.Identity
{
    public partial class Profile
    {
        private UpdateUserRequest UpdateUserRequest { get; set; } = new();
        private string Firstname { get; set; }
        private string Lastname { get; set; }
        private char FirstLetterOfFirstname { get; set; }
        private string Email { get; set; }
        private string _profileImageDataUrl;

        public string UserId { get; set; }
        private MudForm _form;
        protected override async Task OnInitializedAsync()
        {
            await SetCurrentUserDetails();
        }

        private async Task SetCurrentUserDetails()
        {
            var state = await _applicationStateProvider.GetAuthenticationStateAsync();

            var user = state.User;

            Firstname = user.GetFirstname();
            Lastname = user.GetLastname();
            Email = user.GetEmail();
            UserId = user.GetUserId();

            UpdateUserRequest.Id = UserId;
            UpdateUserRequest.FirstName = Firstname;
            UpdateUserRequest.LastName = Lastname;
            UpdateUserRequest.Email = Email;
            UpdateUserRequest.PhoneNumber = user.GetPhoneNumber();

            var userResult = await _userService.GetByIdAsync(UserId);
            if (userResult.IsSuccessful)
            {
                UpdateUserRequest.Email = userResult.Data.Email;
                UpdateUserRequest.PhoneNumber = userResult.Data.PhoneNumber;
                UpdateUserRequest.ImageFile = userResult.Data.ImageFile;
                _profileImageDataUrl = userResult.Data.ImageFile;
            }

            if (Firstname.Length > 0)
            {
                FirstLetterOfFirstname = Firstname[0];
            }
        }

        private async Task UpdateUserDetailsAsync()
        {
            var result = await _userService.UpdateUserAsync(UpdateUserRequest);

            if (result.IsSuccessful)
            {
                await _tokenService.LogoutAsync();
                _snackbar.Add("Votre profil a bien été mise à jour. Connectez vous de nouveau.", Severity.Info);
                _navigation.NavigateTo("/");
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }

        private async Task OnProfileImageChanged(InputFileChangeEventArgs args)
        {
            var imageFile = await args.File.RequestImageFileAsync(args.File.ContentType, 512, 512);
            using var stream = imageFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);

            _profileImageDataUrl = $"data:{imageFile.ContentType};base64,{Convert.ToBase64String(memoryStream.ToArray())}";
            UpdateUserRequest.ImageFile = _profileImageDataUrl;
        }
    }
}
