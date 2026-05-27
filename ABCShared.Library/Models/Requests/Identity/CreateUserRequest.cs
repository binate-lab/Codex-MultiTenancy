namespace ABCShared.Library.Models.Requests.Identity
{
    public class CreateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageFile { get; set; }
        public bool IsActive { get; set; }
    }
}
