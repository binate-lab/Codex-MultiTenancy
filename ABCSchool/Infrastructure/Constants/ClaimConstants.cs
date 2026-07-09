namespace Infrastructure.Constants
{
    public static class ClaimConstants
    {
        public const string Tenant = "tenant";
        public const string School = "school";
        public const string NomCourtEts = "nomCourtEts";
        public const string Permission = "permission";
        // Statut de l'école (Public / Prive) — porté par le token école-scoped ; permet aux
        // microservices (Pédagogie) de gouverner des règles métier (édition cycle/niveau/statut).
        public const string Statut = "statut";
    }
}
