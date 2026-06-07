namespace TrajanEcole.Shared.Library.Models.Requests.Identity
{
    public class ChangeUserStatusRequest
    {
        public string UserId { get; set; }
        public bool Activation { get; set; }
    }
}
