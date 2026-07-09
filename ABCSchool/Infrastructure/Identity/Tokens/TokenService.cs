using Application;
using Application.Exceptions;
using Application.Features.Identity.Tokens;
using Application.Features.Identity.Users;
using Application.Features.Schools;
using Application.Features.Schools.Memberships;
using Finbuckle.MultiTenant.Abstractions;
using Infrastructure.Constants;
using Infrastructure.Identity.Models;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity.Tokens
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IMultiTenantContextAccessor<TrajanEcoleTenantInfo> _tenantContextAccessor;
        private readonly JwtSettings _jwtSettings;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISchoolService _schoolService;
        private readonly ISchoolMembershipService _membershipService;

        public TokenService(
            UserManager<ApplicationUser> userManager,
            IMultiTenantContextAccessor<TrajanEcoleTenantInfo> tenantContextAccessor,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JwtSettings> jwtSettings,
            ICurrentUserService currentUserService,
            ISchoolService schoolService,
            ISchoolMembershipService membershipService)
        {
            _userManager = userManager;
            _tenantContextAccessor = tenantContextAccessor;
            _roleManager = roleManager;
            _jwtSettings = jwtSettings.Value;
            _currentUserService = currentUserService;
            _schoolService = schoolService;
            _membershipService = membershipService;
        }

        public async Task<TokenResponse> LoginAsync(TokenRequest request)
        {
            var currentTenant = GetCurrentTenant();

            #region Validations
            if (!currentTenant.IsActive)
            {
                throw new UnauthorizedException(["La souscription de l'Ets est désactivée actuellement. Contacter Administrateur."]);
            }

            var userInDb = await _userManager.FindByNameAsync(request.Username)
                ?? throw new UnauthorizedException(["L'authentication a échoué."]);

            if (!await _userManager.CheckPasswordAsync(userInDb, request.Password))
            {
                throw new UnauthorizedException(["Incorrect Username or Password."]);
            }

            if (!userInDb.IsActive)
            {
                throw new UnauthorizedException(["L'utilisateur est inactif actuellement. Contacter Administrateur."]);
            }

            if (!TenancyConstants.IsRoot(currentTenant))
            {
                if (currentTenant.ValidUpTo < DateTime.UtcNow)
                {
                    throw new UnauthorizedException(["La souscription de l'Ets a expiré. Contacter Administrateur."]);
                }
            }
            #endregion

            // Generate jwt
            return await GenerateTokenAndUpdateUserAsync(userInDb);
        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var userPrincipal = GetClaimsPrincipalFromExpiringToken(request.CurrentJwt);
            var userEmail = userPrincipal.GetEmail();

            var userInDb = await _userManager.FindByEmailAsync(userEmail)
                ?? throw new UnauthorizedException(["L'authentication a échoué."]);

            if (userInDb.RefreshToken != request.CurrentRefreshToken || userInDb.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                throw new UnauthorizedException(["Token invalide."]);
            }

            // Si le token expirant était ECOLE-scoped (claim « school »), on régénère un
            // token école-scoped -> le refresh conserve school + nomCourtEts (sinon l'envoi
            // SMS par école casserait après un rafraîchissement). Sinon : token général.
            var codeEts = userPrincipal.FindFirst(ClaimConstants.School)?.Value;
            if (!string.IsNullOrWhiteSpace(codeEts))
            {
                return await GenerateSchoolScopedTokenAsync(userInDb, codeEts);
            }

            return await GenerateTokenAndUpdateUserAsync(userInDb);
        }

        public async Task<TokenResponse> SelectSchoolAsync(SelectSchoolRequest request)
        {
            var currentTenant = GetCurrentTenant();

            var userId = _currentUserService.GetUserId();
            var userInDb = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedException(["L'authentification a échoué."]);

            if (!userInDb.IsActive)
            {
                throw new UnauthorizedException(["L'utilisateur est inactif actuellement. Contacter Administrateur."]);
            }

            // Corps « école-scoped » partagé avec le refresh (pour qu'un token rafraîchi
            // conserve school + nomCourtEts au lieu de retomber sur un token général).
            return await GenerateSchoolScopedTokenAsync(userInDb, request.CodeEts);
        }

        // Construit un token ECOLE-scoped (claims school + nomCourtEts + rôles/permissions
        // de l'utilisateur DANS cette école) et met à jour son refresh token. Utilisé par
        // SelectSchool ET par le refresh d'un token déjà école-scoped.
        private async Task<TokenResponse> GenerateSchoolScopedTokenAsync(ApplicationUser userInDb, string codeEts)
        {
            var school = await _schoolService.GetByCodeEtsAsync(codeEts)
                ?? throw new NotFoundException([$"École '{codeEts}' introuvable."]);

            // admin/root (rôle tenant-wide Admin) : accès à toutes les écoles du tenant,
            // avec toutes ses permissions tenant-wide (Root incluses si tenant racine).
            var isTenantAdmin = await _userManager.IsInRoleAsync(userInDb, TrajanEcole.Shared.Library.Constants.RoleConstants.Admin);

            List<string> roleNames;
            if (isTenantAdmin)
            {
                roleNames = [.. await _userManager.GetRolesAsync(userInDb)];
            }
            else
            {
                // Sinon : il faut une affectation (SchoolMembership) à cette école précise.
                var roleIds = await _membershipService.GetUserRoleIdsInSchoolAsync(userInDb.Id, school.Id);

                if (roleIds.Count == 0)
                {
                    throw new UnauthorizedException(["Vous n'avez pas accès à cette école."]);
                }

                roleNames = [];
                foreach (var roleId in roleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role is not null)
                    {
                        roleNames.Add(role.Name);
                    }
                }
            }

            var claims = await GetSchoolScopedClaims(userInDb, school.CodeEts, school.NomCourtEts, roleNames, school.Statut.ToString());
            var jwt = GenerateEncryptedToken(GenerateSigningCredentials(), claims);

            userInDb.RefreshToken = GenerateRefreshToken();
            userInDb.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryTimeInDays);
            await _userManager.UpdateAsync(userInDb);

            return new TokenResponse
            {
                Jwt = jwt,
                RefreshToken = userInDb.RefreshToken,
                RefreshTokenExpiryDate = userInDb.RefreshTokenExpiryTime
            };
        }

        private async Task<IEnumerable<Claim>> GetSchoolScopedClaims(
            ApplicationUser user, string codeEts, string nomCourtEts, List<string> roleNames, string statut)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);

            var roleClaims = new List<Claim>();
            var permissionClaims = new List<Claim>();

            foreach (var roleName in roleNames)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, roleName));

                var currentRole = await _roleManager.FindByNameAsync(roleName);
                if (currentRole is not null)
                {
                    permissionClaims.AddRange(await _roleManager.GetClaimsAsync(currentRole));
                }
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FirstName ?? string.Empty),
                new(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new(ClaimConstants.Tenant, GetCurrentTenant().Identifier),
                new(ClaimConstants.School, codeEts),
                new(ClaimConstants.NomCourtEts, nomCourtEts ?? string.Empty),
                new(ClaimConstants.Statut, statut ?? string.Empty),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
            }
            .Union(roleClaims)
            .Union(permissionClaims)
            .Union(userClaims);

            return claims;
        }

        private ClaimsPrincipal GetClaimsPrincipalFromExpiringToken(string expiringToken)
        {
            var tkValidationParams = new TokenValidationParameters 
            { 
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var pricipal = tokenHandler.ValidateToken(expiringToken, tkValidationParams, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedException(["Le token est invalide. Echec à la generation d'un nouveau token."]);
            }

            return pricipal;
        }

        private async Task<TokenResponse> GenerateTokenAndUpdateUserAsync(ApplicationUser user)
        {
            // Generate jwt
            var newJwt = await GenerateToken(user);

            // Refresh Token
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryTimeInDays);

            await _userManager.UpdateAsync(user);

            return new TokenResponse
            {
                Jwt = newJwt,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryDate = user.RefreshTokenExpiryTime
            };
        }

        private async Task<string> GenerateToken(ApplicationUser user)
        {
            // encrypted token
            return GenerateEncryptedToken(GenerateSigningCredentials(), await GetUserClaims(user));
        } 

        private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryTimeInMinutes),
               signingCredentials: signingCredentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
        private SigningCredentials GenerateSigningCredentials()
        {
            byte[] secret = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
            return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
        }

        private async Task<IEnumerable<Claim>> GetUserClaims(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);
            
            var roleClaims = new List<Claim>();
            var permissionClaims = new List<Claim>();

            foreach (var userRole in userRoles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, userRole));
                var currentRole = await _roleManager.FindByNameAsync(userRole);

                var allPermissionsForCurrentRole = await _roleManager.GetClaimsAsync(currentRole);

                permissionClaims.AddRange(allPermissionsForCurrentRole);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimConstants.Tenant, GetCurrentTenant().Identifier),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
            }
            .Union(roleClaims)
            .Union(userClaims)
            .Union(permissionClaims);

            return claims;
        }

        private TrajanEcoleTenantInfo GetCurrentTenant()
        {
            return _tenantContextAccessor.MultiTenantContext?.TenantInfo
                ?? throw new UnauthorizedException(["Organisation introuvable. Utilisez son identifiant, par exemple 'heleis', dans le header tenant."]);
        }

        private string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
    }
}
