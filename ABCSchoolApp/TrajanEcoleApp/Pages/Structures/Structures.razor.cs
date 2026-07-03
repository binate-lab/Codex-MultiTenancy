using App.Infrastructure.Services.Structures;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TrajanEcoleApp.Pages.Structures
{
    public partial class Structures
    {
        [Inject] private IStructureService _structureService { get; set; }

        // Année scolaire en cours : les classes se gèrent pour cette année.
        private string _annee = "—";

        private List<CycleRow> _cycles = new();
        private List<NiveauRow> _niveaux = new();
        private List<ClasseRow> _classes = new();

        // Champs des lignes d'ajout.
        private int _nouveauCycleNumero = 1;
        private string _nouveauCycleLibelle = string.Empty;
        private Guid? _nouveauNiveauCycleId;
        private string _nouveauNiveauCode = string.Empty;
        private string _nouveauNiveauLibelle = string.Empty;
        private Guid? _nouvelleClasseNiveauId;
        private string _nouvelleClasseLibelle = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var annee = await _anneeScolaireService.GetAnneeEnCoursAsync();
            if (annee.IsSuccessful && annee.Data is not null)
            {
                _annee = annee.Data.Libelle;
            }

            await ChargerToutAsync();
        }

        private async Task ChargerToutAsync()
        {
            await ChargerCyclesEtNiveauxAsync();
            await ChargerClassesAsync();
        }

        // L'arbre cycles→niveaux arrive d'un bloc ; on l'aplatit pour les deux grilles.
        private async Task ChargerCyclesEtNiveauxAsync()
        {
            var cycles = await _structureService.GetCyclesAsync();
            _cycles = cycles.Select(c => new CycleRow(c)).ToList();
            _niveaux = cycles.SelectMany(c => c.Niveaux).Select(n => new NiveauRow(n)).ToList();
        }

        private async Task ChargerClassesAsync()
        {
            var classes = await _structureService.GetClassesAsync(_annee == "—" ? null : _annee);
            _classes = classes.Select(c => new ClasseRow(c)).ToList();
        }

        // Affiche le message métier du backend (doublon, suppression refusée...) tel quel.
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

        // ------------------------------- Cycles -------------------------------

        private async Task AjouterCycleAsync()
        {
            if (string.IsNullOrWhiteSpace(_nouveauCycleLibelle))
            {
                _snackbar.Add("Le libellé du cycle est obligatoire.", Severity.Warning);
                return;
            }

            var ordre = _cycles.Count + 1;
            var result = await _structureService.CreateCycleAsync(_nouveauCycleNumero, _nouveauCycleLibelle, ordre);
            if (Verifier(result, $"Cycle « {_nouveauCycleLibelle} » ajouté."))
            {
                _nouveauCycleNumero = _cycles.Count + 2; // prochain numéro probable
                _nouveauCycleLibelle = string.Empty;
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        private async Task EnregistrerCycleAsync(CycleRow row)
        {
            var result = await _structureService.UpdateCycleAsync(row.Id, row.Numero, row.Libelle, row.Ordre);
            if (Verifier(result, $"Cycle « {row.Libelle} » enregistré."))
            {
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        private async Task SupprimerCycleAsync(CycleRow row)
        {
            var result = await _structureService.DeleteCycleAsync(row.Id);
            if (Verifier(result, $"Cycle « {row.Libelle} » supprimé."))
            {
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        // ------------------------------- Niveaux -------------------------------

        private async Task AjouterNiveauAsync()
        {
            if (_nouveauNiveauCycleId is null)
            {
                _snackbar.Add("Choisissez d'abord le cycle du niveau.", Severity.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_nouveauNiveauCode) || string.IsNullOrWhiteSpace(_nouveauNiveauLibelle))
            {
                _snackbar.Add("Le code et le libellé du niveau sont obligatoires.", Severity.Warning);
                return;
            }

            var ordre = _niveaux.Count + 1;
            var result = await _structureService.CreateNiveauAsync(
                _nouveauNiveauCycleId.Value, _nouveauNiveauCode, _nouveauNiveauLibelle, ordre);
            if (Verifier(result, $"Niveau « {_nouveauNiveauCode} » ajouté."))
            {
                _nouveauNiveauCode = string.Empty;
                _nouveauNiveauLibelle = string.Empty;
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        private async Task EnregistrerNiveauAsync(NiveauRow row)
        {
            var result = await _structureService.UpdateNiveauAsync(row.Id, row.CycleId, row.Code, row.Libelle, row.Ordre);
            if (Verifier(result, $"Niveau « {row.Code} » enregistré."))
            {
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        private async Task SupprimerNiveauAsync(NiveauRow row)
        {
            var result = await _structureService.DeleteNiveauAsync(row.Id);
            if (Verifier(result, $"Niveau « {row.Code} » supprimé."))
            {
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        // ------------------------------- Classes -------------------------------

        private async Task AjouterClasseAsync()
        {
            if (_nouvelleClasseNiveauId is null)
            {
                _snackbar.Add("Choisissez d'abord le niveau de la classe.", Severity.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_nouvelleClasseLibelle))
            {
                _snackbar.Add("Le libellé de la classe est obligatoire.", Severity.Warning);
                return;
            }

            var result = await _structureService.CreateClasseAsync(
                _nouvelleClasseNiveauId.Value, _annee, _nouvelleClasseLibelle);
            if (Verifier(result, $"Classe « {_nouvelleClasseLibelle} » ajoutée."))
            {
                _nouvelleClasseLibelle = string.Empty;
                await ChargerClassesAsync();
                await ChargerCyclesEtNiveauxAsync(); // rafraîchit les compteurs de classes
            }
        }

        private async Task EnregistrerClasseAsync(ClasseRow row)
        {
            var result = await _structureService.UpdateClasseAsync(row.Id, row.NiveauId, row.AnneeScolaire, row.Libelle);
            if (Verifier(result, $"Classe « {row.Libelle} » enregistrée."))
            {
                await ChargerClassesAsync();
            }
        }

        private async Task SupprimerClasseAsync(ClasseRow row)
        {
            var result = await _structureService.DeleteClasseAsync(row.Id);
            if (Verifier(result, $"Classe « {row.Libelle} » supprimée."))
            {
                await ChargerClassesAsync();
                await ChargerCyclesEtNiveauxAsync();
            }
        }

        // ------------------- ViewModels mutables des grilles -------------------

        public sealed class CycleRow
        {
            public CycleRow(CycleItem c)
            {
                Id = c.Id; Numero = c.Numero; Libelle = c.Libelle; Ordre = c.Ordre; NbNiveaux = c.Niveaux.Count;
            }
            public Guid Id { get; }
            public int Numero { get; set; }
            public string Libelle { get; set; }
            public int Ordre { get; set; }
            public int NbNiveaux { get; }
        }

        public sealed class NiveauRow
        {
            public NiveauRow(NiveauItem n)
            {
                Id = n.Id; CycleId = n.CycleId; Code = n.Code; Libelle = n.Libelle; Ordre = n.Ordre; NbClasses = n.NbClasses;
            }
            public Guid Id { get; }
            public Guid CycleId { get; set; }
            public string Code { get; set; }
            public string Libelle { get; set; }
            public int Ordre { get; set; }
            public int NbClasses { get; }
        }

        public sealed class ClasseRow
        {
            public ClasseRow(ClasseItem c)
            {
                Id = c.Id; NiveauId = c.NiveauId; AnneeScolaire = c.AnneeScolaire; Libelle = c.Libelle;
            }
            public Guid Id { get; }
            public Guid NiveauId { get; set; }
            public string AnneeScolaire { get; set; }
            public string Libelle { get; set; }
        }

    }
}
