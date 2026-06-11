namespace App.Infrastructure
{
    public class ApiSettings
    {
        public string BaseApiUrl { get; set; }
        public TokenEndpoints TokenEndpoints { get; set; }
        public UserEndpoints UserEndpoints { get; set; }
        public TenantEndpoints TenantEndpoints { get; set; }
        public RoleEndpoints RoleEndpoints { get; set; }
        public SchoolEndpoints SchoolEndpoints { get; set; }
        public ChatEndpoints ChatEndpoints { get; set; }
        public CertificatEndpoints CertificatEndpoints { get; set; }
    }

    public class ChatEndpoints
    {
        public string Send { get; set; }
    }

    public class CertificatEndpoints
    {
        public string SoumettreDemandeUrl { get; set; }
        public string MesDemandesUrl { get; set; }
        public string MesAppareilsUrl { get; set; }
        public string DemandesPendantesUrl { get; set; }
        public string ApprouverUrl { get; set; }
        public string RejeterUrl { get; set; }
        public string RevoquerUrl { get; set; }

        public string SupprimerUrl { get; set; }
        public string GetApprouver(Guid demandeId) => $"{ApprouverUrl}{demandeId}/approuver";
        public string GetRejeter(Guid demandeId) => $"{RejeterUrl}{demandeId}/rejeter";
        public string GetRevoquer(Guid certificatId) => $"{RevoquerUrl}{certificatId}/revoquer";
        public string GetReactiver(Guid certificatId) => $"{RevoquerUrl}{certificatId}/reactiver";
        public string GetSupprimer(Guid demandeId) => $"{SupprimerUrl}{demandeId}";
    }

    public class TokenEndpoints
    {
        public string Login { get; set; }
        public string RefreshToken { get; set; }
    }

    public class UserEndpoints
    {
        public string Update { get; set; }
        public string ResetPassword { get; set; }
        public string All { get; set; }
        public string ById { get; set; }
        public string Register { get; set; }
        public string RolesById { get; set; }
        public string UpdateRoles { get; set; }
        public string UpdateStatus { get; set; }

        public string GetById(string userId) =>  $"{ById}{userId}";
        public string GetRolesById(string userId) =>  $"{RolesById}{userId}";
        public string UpdateRolesById(string userId) =>  $"{UpdateRoles}{userId}";
    }

    public class TenantEndpoints
    {
        public string Create { get; set; }
        public string Upgrade { get; set; }
        public string All { get; set; }
        public string ById { get; set; }
        public string Activate { get; set; }
        public string DeActivate { get; set; }
        public string Delete { get; set; }

        public string GetById(string tenantId)
        {
            return $"{ById}{tenantId}";
        }

        public string FullActivate(string tenantId)
        {
            return $"{Activate}{tenantId}/activate";
        }

        public string FullDeActivate(string tenantId)
        {
            return $"{DeActivate}{tenantId}/deactivate";
        }

        public string GetDelete(string tenantId)
        {
            return $"{Delete}{tenantId}";
        }
    }

    public class RoleEndpoints
    {
        public string Create { get; set; }
        public string Update { get; set; }
        public string PartialById { get; set; }
        public string FullById { get; set; }
        public string All { get; set; }
        public string Delete { get; set; }
        public string UpdatePermissions { get; set; }

        public string GetPartial(string roleId)
            => $"{PartialById}{roleId}";

        public string GetFull(string roleId)
            => $"{FullById}{roleId}";

        public string GetDelete(string roleId)
            => $"{Delete}{roleId}";
    }

    public class SchoolEndpoints
    {
        public string Create { get; set; }
        public string Update { get; set; }
        public string Delete { get; set; }
        public string ById { get; set; }
        public string ByName { get; set; }
        public string All { get; set; }

        public string GetById(string schoolId)
            => $"{ById}{schoolId}";
        public string GetByName(string schoolName)
            => $"{ByName}{schoolName}";
        public string GetDelete(string schoolId)
            => $"{Delete}{schoolId}";
    }
}
