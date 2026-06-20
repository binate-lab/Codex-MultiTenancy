using System.Collections.ObjectModel;

namespace Infrastructure.Constants
{
    public static class SchoolAction
    {
        public const string Read = nameof(Read);
        public const string Create = nameof(Create);
        public const string Update = nameof(Update);
        public const string Delete = nameof(Delete);
        public const string RefreshToken = nameof(RefreshToken);
        public const string UpgradeSubscription = nameof(UpgradeSubscription);
    }

    public static class SchoolFeature
    {
        public const string Tenants = nameof(Tenants);
        public const string Users = nameof(Users);
        public const string Roles = nameof(Roles);
        public const string UserRoles = nameof(UserRoles);
        public const string RoleClaims = nameof(RoleClaims);
        public const string Schools = nameof(Schools);
        public const string SchoolMemberships = nameof(SchoolMemberships);
        public const string Tokens = nameof(Tokens);
        public const string Certificats = nameof(Certificats);
    }

    public record SchoolPermission(string Action, string Feature, string Description, string Group, bool IsBasic = false, bool IsRoot = false)
    {
        public string Name => NameFor(Action, Feature);

        public static string NameFor(string action, string feature) => $"Permission.{feature}.{action}";
    }

    public static class SchoolPermissions
    {
        private static readonly SchoolPermission[] _allPermissions =
        [
            new SchoolPermission(SchoolAction.Create, SchoolFeature.Tenants, "Créer Tenants", "Tenancy", IsRoot: true),
            new SchoolPermission(SchoolAction.Read, SchoolFeature.Tenants, "Lire Tenants", "Tenancy", IsRoot: true),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.Tenants, "Mis à jour Tenants", "Tenancy", IsRoot: true),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.Tenants, "Supprimer Tenants", "Tenancy", IsRoot: true),
            new SchoolPermission(SchoolAction.UpgradeSubscription, SchoolFeature.Tenants, "Upgrader Souscription d'un Tenant", "Tenancy", IsRoot: true),

            new SchoolPermission(SchoolAction.Create, SchoolFeature.Users, "Créer Utilisateurs", "SystemAccess"),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.Users, "Mis à jour Utilisateurs", "SystemAccess"),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.Users, "Supprimer Utilisateurs", "SystemAccess"),
            new SchoolPermission(SchoolAction.Read, SchoolFeature.Users, "Lire Utilisateurs", "SystemAccess"),

            new SchoolPermission(SchoolAction.Read, SchoolFeature.UserRoles, "Lire Rôles Utilisateurs", "SystemAccess"),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.UserRoles, "Mis à jour Rôles Utilisateurs", "SystemAccess"),

            new SchoolPermission(SchoolAction.Read, SchoolFeature.Roles, "Lire Rôles", "SystemAccess"),
            new SchoolPermission(SchoolAction.Create, SchoolFeature.Roles, "Créer Rôles", "SystemAccess"),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.Roles, "Mis à jour Rôles", "SystemAccess"),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.Roles, "Supprimer Rôles", "SystemAccess"),

            new SchoolPermission(SchoolAction.Read, SchoolFeature.RoleClaims, "Lire Permissions", "SystemAccess"),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.RoleClaims, "Mis à jour Permissions", "SystemAccess"),

            new SchoolPermission(SchoolAction.Read, SchoolFeature.Schools, "Lire Ecoles", "Academics", IsBasic: true),
            new SchoolPermission(SchoolAction.Create, SchoolFeature.Schools, "Créer Ecoles", "Academics"),
            new SchoolPermission(SchoolAction.Update, SchoolFeature.Schools, "Mis à jour Ecoles", "Academics"),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.Schools, "Supprimer Ecoles", "Academics"),

            new SchoolPermission(SchoolAction.Read, SchoolFeature.SchoolMemberships, "Lire Affectations Ecole", "SystemAccess"),
            new SchoolPermission(SchoolAction.Create, SchoolFeature.SchoolMemberships, "Affecter Utilisateur à une Ecole", "SystemAccess"),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.SchoolMemberships, "Retirer Affectation Ecole", "SystemAccess"),

            new SchoolPermission(SchoolAction.RefreshToken, SchoolFeature.Tokens, "Generate Refresh Token", "SystemAccess", IsBasic: true),

            // Certificats appareils — Root uniquement (Keita & équipe)
            new SchoolPermission(SchoolAction.Create, SchoolFeature.Certificats, "Approuver demande certificat", "Certificats", IsRoot: true),
            new SchoolPermission(SchoolAction.Delete, SchoolFeature.Certificats, "Révoquer certificat", "Certificats", IsRoot: true),
            new SchoolPermission(SchoolAction.Read, SchoolFeature.Certificats, "Lire demandes en attente", "Certificats", IsRoot: true),

            // Certificats appareils — Admin tenant (Update couvre soumettre + lire ses propres données)
            new SchoolPermission(SchoolAction.Update, SchoolFeature.Certificats, "Gérer ses demandes et certificats", "Certificats"),
        ];

        public static IReadOnlyList<SchoolPermission> All { get; } 
            = new ReadOnlyCollection<SchoolPermission>(_allPermissions);

        public static IReadOnlyList<SchoolPermission> Root { get; } 
            = new ReadOnlyCollection<SchoolPermission>(_allPermissions.Where(p => p.IsRoot).ToArray());

        public static IReadOnlyList<SchoolPermission> Admin { get; } 
            = new ReadOnlyCollection<SchoolPermission>(_allPermissions.Where(p => !p.IsRoot).ToArray());

        public static IReadOnlyList<SchoolPermission> Basic { get; } 
            = new ReadOnlyCollection<SchoolPermission>(_allPermissions.Where(p => p.IsBasic).ToArray());
    }
}
