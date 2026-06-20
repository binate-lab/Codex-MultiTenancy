namespace Application.Features.Schools.Memberships
{
    public class AssignSchoolMembershipRequest
    {
        public string UserId { get; set; }
        public int SchoolId { get; set; }
        public string RoleId { get; set; }
    }
}
