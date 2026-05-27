namespace ABCShared.Library.Models.Responses.Identity
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageFile { get; set; }
        public bool IsActive { get; set; }
    }
}
