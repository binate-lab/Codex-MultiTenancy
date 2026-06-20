namespace Application.Features.Identity.Tokens
{
    // Émet un JWT école-scoped pour l'école choisie (code établissement).
    public class SelectSchoolRequest
    {
        public string CodeEts { get; set; }
    }
}
