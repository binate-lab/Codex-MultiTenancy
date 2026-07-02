namespace App.Infrastructure.Services.Structures
{
    // DTOs du module Structures de pedagogie-api (schéma "structures" de PedagogieDb).
    public record NiveauItem(Guid Id, Guid CycleId, string Code, string Libelle, int Ordre, int NbClasses);
    public record CycleItem(Guid Id, int Numero, string Libelle, int Ordre, List<NiveauItem> Niveaux);
    public record ClasseItem(Guid Id, Guid NiveauId, string NiveauCode, string AnneeScolaire, string Libelle);
    public record MatiereItem(Guid Id, string Code, string Libelle, int Ordre);

    // Résultat d'écriture : Error porte le message métier du backend (409 doublon,
    // suppression refusée...) à afficher tel quel dans la snackbar.
    public record StructureOpResult(bool IsSuccessful, string Error = null);

    public interface IStructureService
    {
        Task<IReadOnlyList<CycleItem>> GetCyclesAsync();
        Task<StructureOpResult> CreateCycleAsync(int numero, string libelle, int ordre);
        Task<StructureOpResult> UpdateCycleAsync(Guid id, int numero, string libelle, int ordre);
        Task<StructureOpResult> DeleteCycleAsync(Guid id);

        Task<StructureOpResult> CreateNiveauAsync(Guid cycleId, string code, string libelle, int ordre);
        Task<StructureOpResult> UpdateNiveauAsync(Guid id, Guid cycleId, string code, string libelle, int ordre);
        Task<StructureOpResult> DeleteNiveauAsync(Guid id);

        Task<IReadOnlyList<ClasseItem>> GetClassesAsync(string annee = null);
        Task<StructureOpResult> CreateClasseAsync(Guid niveauId, string anneeScolaire, string libelle);
        Task<StructureOpResult> UpdateClasseAsync(Guid id, Guid niveauId, string anneeScolaire, string libelle);
        Task<StructureOpResult> DeleteClasseAsync(Guid id);

        Task<IReadOnlyList<MatiereItem>> GetMatieresAsync();
        Task<StructureOpResult> CreateMatiereAsync(string code, string libelle, int ordre);
        Task<StructureOpResult> UpdateMatiereAsync(Guid id, string code, string libelle, int ordre);
        Task<StructureOpResult> DeleteMatiereAsync(Guid id);
    }
}
