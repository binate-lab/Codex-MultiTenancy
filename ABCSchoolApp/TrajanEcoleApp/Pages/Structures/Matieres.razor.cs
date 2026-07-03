using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Structures
{
    public partial class Matieres
    {
        [Inject] private IStructureService _structureService { get; set; }

        // Année scolaire en cours (bandeau).
        private string _annee = "—";

        private List<MatiereRow> _matieres = new();

        // Champs de la ligne d'ajout.
        private string _nouvelleMatiereCode = string.Empty;
        private string _nouvelleMatiereLibelle = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            await ChargerMatieresAsync();
        }

        private async Task ChargerMatieresAsync()
        {
            var matieres = await _structureService.GetMatieresAsync();
            _matieres = matieres.Select(m => new MatiereRow(m)).ToList();
        }

        // Affiche le message métier du backend (doublon...) tel quel.
        private bool Verifier(StructureOpResult result, string messageSucces)
        {
            if (result.IsSuccessful)
            {
                _snackbar.Add(messageSucces, Severity.Success);
                return true;
            }

            _snackbar.Add(result.Error, Severity.Error);
            return false;
        }

        private async Task AjouterMatiereAsync()
        {
            if (string.IsNullOrWhiteSpace(_nouvelleMatiereCode) || string.IsNullOrWhiteSpace(_nouvelleMatiereLibelle))
            {
                _snackbar.Add("Le code et le libellé de la matière sont obligatoires.", Severity.Warning);
                return;
            }

            var ordre = _matieres.Count + 1;
            var result = await _structureService.CreateMatiereAsync(_nouvelleMatiereCode, _nouvelleMatiereLibelle, ordre);
            if (Verifier(result, $"Matière « {_nouvelleMatiereCode.ToUpperInvariant()} » ajoutée."))
            {
                _nouvelleMatiereCode = string.Empty;
                _nouvelleMatiereLibelle = string.Empty;
                await ChargerMatieresAsync();
            }
        }

        // Enregistrement direct façon Access : part en base à la sortie de la cellule,
        // seulement si code ou libellé a réellement changé ; restauration si refus.
        private async Task EnregistrerMatiereSiModifieeAsync(MatiereRow row)
        {
            if (row.Code == row.CodeInitial && row.Libelle == row.LibelleInitial)
            {
                return;
            }

            var result = await _structureService.UpdateMatiereAsync(row.Id, row.Code, row.Libelle, row.Ordre);
            if (Verifier(result, $"Matière « {row.Code} » enregistrée."))
            {
                await ChargerMatieresAsync();
            }
            else
            {
                row.Code = row.CodeInitial;
                row.Libelle = row.LibelleInitial;
            }
        }

        private async Task SupprimerMatiereAsync(MatiereRow row)
        {
            var result = await _structureService.DeleteMatiereAsync(row.Id);
            if (Verifier(result, $"Matière « {row.Code} » supprimée."))
            {
                await ChargerMatieresAsync();
            }
        }

        public sealed class MatiereRow
        {
            public MatiereRow(MatiereItem m)
            {
                Id = m.Id; Code = m.Code; CodeInitial = m.Code;
                Libelle = m.Libelle; LibelleInitial = m.Libelle; Ordre = m.Ordre;
            }
            public Guid Id { get; }
            public string Code { get; set; }
            public string CodeInitial { get; }
            public string Libelle { get; set; }
            public string LibelleInitial { get; }
            public int Ordre { get; set; }
        }
    }
}
