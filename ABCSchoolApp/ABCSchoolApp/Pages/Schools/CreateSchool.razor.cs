using ABCShared.Library.Models.Requests.Schools;
using App.Infrastructure.Validators;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ABCSchoolApp.Pages.Schools
{
    public partial class CreateSchool
    {
        [CascadingParameter] private IMudDialogInstance _dialogInstance { get; set; }
        private CreateSchoolRequest CreateSchoolRequest { get; set; } = new();
        private MudForm _form;
        private MudDatePicker _datePicker = default!;
        private CreateSchoolRequestValidator _validator = new();

        private DateTime? EstablishedDatePicker
        {
            get => CreateSchoolRequest.EstablishedDate == default
                ? null
                : CreateSchoolRequest.EstablishedDate;
            set
            {
                if (value.HasValue)
                {
                    CreateSchoolRequest.EstablishedDate = value.Value;
                }
            }
        }

        private async Task SubmitAsync()
        {
            await _form.Validate();
            if (_form.IsValid)
            {
                await SaveSchoolAsync();
            }
        }

        private async Task SaveSchoolAsync()
        {
            var result = await _schoolService.CreateAsync(CreateSchoolRequest);
            if (result.IsSuccessful)
            {
                _snackbar.Add(result.Messages[0], Severity.Success);
                _dialogInstance.Close(DialogResult.Ok(true));
            }
            else
            {
                foreach (var message in result.Messages)
                {
                    _snackbar.Add(message, Severity.Error);
                }
            }
        }
        private void CancelDialog()
        {
            _dialogInstance.Close();
        }
    }
}
