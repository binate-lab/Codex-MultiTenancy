using App.Infrastructure.Constants;
using App.Infrastructure.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Auth
{
    public partial class Login
    {
        [Inject] private ILocalStorageService _localStorage { get; set; } = default!;

        private LoginRequest _loginRequest = new();

        private InputType _inputType = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
        private string _loginImagePath = "images/img1.png";
        private bool _isPasswordVisisble;
        private bool _isSubmitting;
        private bool _rememberMe;
        private MudForm _form = default;

        protected override async Task OnInitializedAsync()
        {
            var imageNumber = Random.Shared.Next(1, 6);
            _loginImagePath = $"images/img{imageNumber}.png";

            var state = await _applicationStateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated is true)
            {
                _navigation.NavigateTo("/");
                return;
            }

            // Pré-remplissage si « Se souvenir de moi » était actif (jamais le mot de passe).
            if (await _localStorage.GetItemAsync<bool>(StorageConstants.RememberMe))
            {
                _rememberMe = true;
                _loginRequest.Tenant = await _localStorage.GetItemAsync<string>(StorageConstants.RememberedTenant);
                _loginRequest.Username = await _localStorage.GetItemAsync<string>(StorageConstants.RememberedUsername);
            }
        }

        private async Task OnRememberMeChangedAsync(bool value)
        {
            _rememberMe = value;

            // Décoché : on efface immédiatement les informations sauvegardées.
            if (!value)
            {
                await EffacerInfosMemoriseesAsync();
            }
        }

        private async Task SauvegarderInfosMemoriseesAsync()
        {
            await _localStorage.SetItemAsync(StorageConstants.RememberMe, true);
            await _localStorage.SetItemAsync(StorageConstants.RememberedTenant, _loginRequest.Tenant);
            await _localStorage.SetItemAsync(StorageConstants.RememberedUsername, _loginRequest.Username);
        }

        private async Task EffacerInfosMemoriseesAsync()
        {
            await _localStorage.RemoveItemAsync(StorageConstants.RememberMe);
            await _localStorage.RemoveItemAsync(StorageConstants.RememberedTenant);
            await _localStorage.RemoveItemAsync(StorageConstants.RememberedUsername);
        }

        private async Task SubmitAsync()
        {
            if (_isSubmitting)
            {
                return;
            }

            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }

            _isSubmitting = true;

            try
            {
                var result = await _tokenService
                    .LoginAsync(tenant: _loginRequest.Tenant, request: _loginRequest);

                if (result.IsSuccessful)
                {
                    if (_rememberMe)
                        await SauvegarderInfosMemoriseesAsync();
                    else
                        await EffacerInfosMemoriseesAsync();

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
            catch (Exception ex)
            {
                _snackbar.Add($"Connexion impossible : {ex.Message}", Severity.Error);
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        void TogglePasswordVisibility()
        {
            if (_isPasswordVisisble)
            {
                _isPasswordVisisble = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _inputType = InputType.Password;
            }
            else
            {
                _isPasswordVisisble = true; 
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _inputType = InputType.Text;
            }
        }      
        
        private void FillRootAdminCredentialsDuringDevelopment()
        {
            _loginRequest.Tenant = "root";
            _loginRequest.Username = "keita_amara@hotmail.com";
            _loginRequest.Password = "P@ssw0rd@123";
        }
    }
}
