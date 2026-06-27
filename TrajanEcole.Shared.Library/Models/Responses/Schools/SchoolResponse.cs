using TrajanEcole.Shared.Library.Enums;

namespace TrajanEcole.Shared.Library.Models.Responses.Schools
{
    public class SchoolResponse
    {
        public int Id { get; set; }
        public string CodeEts { get; set; }
        public string NomCourtEts { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Ville { get; set; }
        public StatutEcole Statut { get; set; }
        public DateTime EstablishedDate { get; set; }
        public string Logo { get; set; }
        public string Devise { get; set; }
    }
}
