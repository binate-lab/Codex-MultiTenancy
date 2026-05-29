namespace Infrastructure.Tenancy
{
    public class TenancyConstants
    {
        public const string TenantIdName = "tenant";
        public const string DefaultPassword = "P@ssw0rd@123";
        public const string FirstName = "KEITA";
        public const string LastName = "Amara";

        public static class Root
        {
            public const string Id = "00000000-0000-0000-0000-000000000001";
            public const string Identifier = "root";
            public const string Name = "Root";
            public const string Email = "keita_amara@hotmail.com";
        }

        public static bool IsRoot(ABCSchoolTenantInfo tenant)
        {
            return tenant?.Identifier == Root.Identifier;
        }
    }
}
