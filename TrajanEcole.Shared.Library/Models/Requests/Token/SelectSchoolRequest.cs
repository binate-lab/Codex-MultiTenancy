namespace TrajanEcole.Shared.Library.Models.Requests.Token
{
    // Corps du POST api/token/select-school : l'école choisie (code établissement).
    public class SelectSchoolRequest
    {
        public string CodeEts { get; set; }
    }
}
