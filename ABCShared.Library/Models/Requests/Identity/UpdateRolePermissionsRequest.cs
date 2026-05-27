namespace ABCShared.Library.Models.Requests.Identity
{
    public class UpdateRolePermissionsRequest
    {
        public string RoleId { get; set; }
        public List<string> NewPermissions { get; set; } = [];
    }
}
