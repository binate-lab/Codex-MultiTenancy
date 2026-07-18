namespace App.Infrastructure.Services.Orange
{
    // Compte marchand Orange Money (config d'intégration, module Scolarite.Api).
    // Le secret HMAC n'est jamais renvoyé en clair sur les lectures : SecretApercu = « …4 derniers ».
    public class CompteMarchandItem
    {
        public int Id { get; set; }
        public string CodeMarchand { get; set; } = string.Empty;
        public string? Libelle { get; set; }
        public bool Actif { get; set; } = true;
        public string SecretApercu { get; set; } = string.Empty;
    }

    // Résultat d'écriture. Secret n'est renseigné qu'à la création / rotation (visible une fois).
    public record MarchandOpResult(bool IsSuccessful, string? Error = null, string? Secret = null);

    public interface ICompteMarchandOrangeService
    {
        Task<IReadOnlyList<CompteMarchandItem>> GetAsync();
        Task<MarchandOpResult> CreateAsync(string codeMarchand, string? libelle);
        Task<MarchandOpResult> UpdateAsync(int id, string codeMarchand, string? libelle, bool actif);
        Task<MarchandOpResult> RotationSecretAsync(int id);
        Task<MarchandOpResult> DeleteAsync(int id);
    }
}
