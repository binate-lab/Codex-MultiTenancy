using TrajanEcole.Shared.Library.Enums;

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
        public int? MatriculeInterne { get; set; } // ancien compteur N° Inscription (conserve pour compat)
        public int? NumOrdre { get; set; }         // N° d'ordre = N° Inscription auto par ecole (remplace MatriculeInterne)
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public int Cycle { get; set; }
        public string Niveau { get; set; } = string.Empty;
        public string Classe { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string LieuDeNaissance { get; set; } = string.Empty;

        // Champs scolarite / identite (calque EleveDto). Valeurs par defaut = celles de l'UI.
        public string BureauEtatCivil { get; set; } = string.Empty;
        public string SousPrefecture { get; set; } = string.Empty;
        public string DateExtrait { get; set; } = string.Empty;
        public string NumExtrait { get; set; } = string.Empty;
        public string Nationalite { get; set; } = "Ivoirienne";
        public string TransfertOuReaffect { get; set; } = string.Empty;
        public string ClassePrecedente { get; set; } = string.Empty;
        public string EtsOrigine { get; set; } = string.Empty;
        public string Red { get; set; } = "NR";
        public StatutEleve Statut { get; set; } = StatutEleve.Naff;
        public string Interne { get; set; } = "Non";
        public string Regime { get; set; } = "NB";
        public string LV_1 { get; set; } = "Anglais";
        public string LV_2 { get; set; } = "Anglais";
        public string Arts { get; set; } = string.Empty;
        public string Serie { get; set; } = "A";
        public string DispenseEps { get; set; } = "Non";
        public DateTime? DateInscription { get; set; } = DateTime.Today;

        // Contexte tenant/ecole. A terme, derive du JWT cote microservice (claims tenant/school).
        public string CodeEts { get; set; } = string.Empty;
        public string Tenant { get; set; } = string.Empty;

        // Parents / correspondant : calque exact de EleveDto.Pere/Mere/Tuteur (ParentDto).
        // « Correspondant » (UI) = Tuteur (backend). Initialises pour le binding du formulaire.
        public ParentRequestDto Pere { get; set; } = new();
        public ParentRequestDto Mere { get; set; } = new();
        public ParentRequestDto Tuteur { get; set; } = new();
    }

    // Calque exact de Eleves.Api/Dtos/ParentDto (= ValueObjects/Tuteur cote backend).
    public class ParentRequestDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string Telephone1 { get; set; } = string.Empty;
        public string Telephone2 { get; set; } = string.Empty;
        public string Telephone3 { get; set; } = string.Empty;
        public string Fonction { get; set; } = string.Empty;
        public ParentStatut Statut { get; set; } = ParentStatut.Actif;
    }
}
