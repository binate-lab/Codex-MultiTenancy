namespace TrajanEcole.Shared.Library.Models.Requests.Eleves
{
    // Enveloppe attendue par Eleves.Api : POST /eleves { "eleveDto": { ... } }.
    public class CreateEleveRequest
    {
        public EleveRequestDto EleveDto { get; set; } = new();
    }

    // DTO plat (primitifs) calque sur Eleves.Api/Dtos/EleveDto. Version minimale :
    // seuls les champs essentiels sont exposes par l'UI, le reste prend ses defauts API.
    public class EleveRequestDto
    {
        public string NumeroMatricule { get; set; } = string.Empty;
        public string MatriculeInterne { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public int Cycle { get; set; }
        public string Niveau { get; set; } = string.Empty;
        public string Classe { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string LieuDeNaissance { get; set; } = string.Empty;
        // Contexte tenant/ecole. A terme, derive du JWT cote microservice (claims tenant/school).
        public string CodeEts { get; set; } = string.Empty;
        public string Tenant { get; set; } = string.Empty;
    }
}
