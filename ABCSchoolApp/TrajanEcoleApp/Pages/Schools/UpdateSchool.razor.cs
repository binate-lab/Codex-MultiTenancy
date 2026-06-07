using TrajanEcole.Shared.Library.Models.Requests.Schools;
using TrajanEcole.Shared.Library.Models.Responses.Schools;
using App.Infrastructure.Validators;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Schools
{
    public partial class UpdateSchool
    {
        [CascadingParameter] private IMudDialogInstance _dialogInstance { get; set; }

        [Parameter] public SchoolResponse School { get; set; } = default!;

        private UpdateSchoolRequest UpdateSchoolRequest { get; set; } = new();
        private MudForm _form = default!;
        private MudDatePicker _datePicker = default!;
        private UpdateSchoolRequestValidator _validator = new();

        protected override void OnParametersSet()
        {
            UpdateSchoolRequest = new UpdateSchoolRequest
            {
                Id = School.Id,
                CodeEts = School.CodeEts,
                NomCourtEts = School.NomCourtEts,
                Name = School.Name,
                Email = School.Email,
                Telephone = School.Telephone,
                Ville = School.Ville,
                Statut = School.Statut,
                EstablishedDate = School.EstablishedDate
            };
        }

        private DateTime? EstablishedDatePicker
        {
            get => UpdateSchoolRequest.EstablishedDate == default
                ? null
                : UpdateSchoolRequest.EstablishedDate;
            set
            {
                if (value.HasValue)
                {
                    UpdateSchoolRequest.EstablishedDate = value.Value;
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
            var result = await _schoolService.UpdateAsync(UpdateSchoolRequest);
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
