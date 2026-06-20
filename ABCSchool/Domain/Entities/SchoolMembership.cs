namespace Domain.Entities
{
    // Appartenance d'un utilisateur à une école avec un rôle donné.
    // « L'utilisateur U a le rôle R dans l'école S » (le tout sous un tenant via Finbuckle).
    // Un utilisateur peut être membre de plusieurs écoles, avec des rôles différents par école.
    public class SchoolMembership
    {
        public int Id { get; set; }

        // FK -> Identity.Users(Id)
        public string UserId { get; set; }

        // FK -> Academics.Schools(Id)
        public int SchoolId { get; set; }

        // FK -> Identity.Roles(Id) : réutilise les rôles/permissions ASP.NET Identity existants
        public string RoleId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public School School { get; set; }
    }
}
