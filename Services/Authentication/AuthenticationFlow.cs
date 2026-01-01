using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services.UserContext;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Unified authentication workflow service for managing complete authentication lifecycle
    /// </summary>
    public class AuthenticationFlow : IAuthenticationFlow
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleValidator _roleValidator;
        private readonly IUserProfileService _userProfileService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuthenticationFlow> _logger;

        public AuthenticationFlow(
            IAuthenticationService authenticationService,
            IUserRoleService userRoleService,
            IRoleValidator roleValidator,
            IUserProfileService userProfileService,
            IAuditLogRepository auditLogRepository,
            ILogger<AuthenticationFlow> logger)
        {
            _authenticationService = authenticationService;
            _userRoleService = userRoleService;
            _roleValidator = roleValidator;
            _userProfileService = userProfileService;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<UserContextState> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                _logger.LogInformation("Authentication failed: User not authenticated");
                return UserContextState.Anonymous;
            }

            var email = await _authenticationService.GetUserEmailAsync();
            var role = await _authenticationService.GetUserRoleAsync();
            var userId = await _authenticationService.GetUserIdAsync();
            var user = await _authenticationService.GetClaimsPrincipalAsync();

            // Get role info from database
            var roleInfo = await _userRoleService.GetUserRoleAsync(email, cancellationToken);

            // Validate role
            var isRoleValid = await _roleValidator.ValidateUserRoleAsync(user, roleInfo.Role, cancellationToken);
            if (!isRoleValid)
            {
                _logger.LogWarning("Role validation failed for {email}: claimed role {claimedRole} does not match database role {databaseRole}", 
                    email, role, roleInfo.Role);
                await LogAuthenticationAttemptAsync("RoleValidation", email, roleInfo.Role, false, $"Role mismatch: claimed={role}, database={roleInfo.Role}", cancellationToken);
                return UserContextState.Anonymous;
            }

            // Check email verification
            var isEmailVerified = await _roleValidator.ValidateEmailVerifiedAsync(user, cancellationToken);
            if (!isEmailVerified)
            {
                _logger.LogWarning("Email verification failed for {email}", email);
                await LogAuthenticationAttemptAsync("EmailVerification", email, roleInfo.Role, false, "Email not verified", cancellationToken);
                return UserContextState.Anonymous;
            }

            // Build user context state using role info from UserRoleService
            // Note: UserRoleService already fetches all role info in parallel, so we don't need to fetch profiles again
            var userContextState = new UserContextState(
                IsAuthenticated: true,
                Role: roleInfo.Role,
                Email: email,
                IsStudentRegistered: roleInfo.StudentId.HasValue,
                IsCompanyRegistered: roleInfo.CompanyId.HasValue,
                IsProfessorRegistered: roleInfo.ProfessorId.HasValue,
                IsResearchGroupRegistered: roleInfo.ResearchGroupId.HasValue,
                Student: null, // Profile objects will be loaded by UserContextService if needed
                Company: null,
                Professor: null,
                ResearchGroup: null,
                RetrievedAt: DateTimeOffset.UtcNow
            );

            // Log successful authentication
            await LogAuthenticationAttemptAsync("Authenticate", email, roleInfo.Role, true, null, cancellationToken);

            return userContextState;
        }

        public async Task<bool> ValidateRoleAsync(ClaimsPrincipal user, string databaseRole, CancellationToken cancellationToken = default)
        {
            return await _roleValidator.ValidateUserRoleAsync(user, databaseRole, cancellationToken);
        }

        public async Task<bool> IsEmailVerifiedAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            return await _roleValidator.ValidateEmailVerifiedAsync(user, cancellationToken);
        }

        public async Task LogAuthenticationAttemptAsync(string action, string email, string role, bool success, string? details = null, CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                Email = email,
                Action = action,
                Role = role,
                Success = success,
                Details = details
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _logger.LogInformation("Authentication attempt logged: {Action} for {Email} - Success: {Success}", action, email, success);
        }

        public async Task InvalidateCacheAsync(string email, CancellationToken cancellationToken = default)
        {
            await _userProfileService.InvalidateProfileCacheAsync(email, cancellationToken);
            _logger.LogInformation("Cache invalidated for {email}", email);
        }
    }
}
