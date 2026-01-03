using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services.UserContext;
using QuizManager.Services.Authentication;

namespace QuizManager.Services.UserContext
{
    public class UserContextService : IUserContextService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<UserContextService> _logger;
        private readonly IUserRoleService _userRoleService;
        private readonly ICacheService _cacheService;
        private readonly IRoleValidator _roleValidator;

        public UserContextService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<UserContextService> logger,
            IUserRoleService userRoleService,
            ICacheService cacheService,
            IRoleValidator roleValidator
        )
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _userRoleService = userRoleService;
            _cacheService = cacheService;
            _roleValidator = roleValidator;
        }

        public async Task<UserContextState> GetStateAsync(
            CancellationToken cancellationToken = default
        )
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
 
            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                return UserContextState.Anonymous;
            }
 
            var email = ResolveEmail(user);
            
            // Try to get from cache first (5-minute expiration)
            var cacheKey = $"UserContext_{email}";
            var cachedState = await _cacheService.GetAsync<UserContextState>(cacheKey);
            
            if (cachedState != null && cachedState.RetrievedAt > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                _logger.LogDebug("Using cached user context for {email}", email);
                return cachedState;
            }
            
            // Cache miss - fetch from database using optimized single query
            bool isStudentRegistered = false;
            bool isCompanyRegistered = false;
            bool isProfessorRegistered = false;
            bool isResearchGroupRegistered = false;
            Student? student = null;
            Company? company = null;
            Professor? professor = null;
            ResearchGroup? researchGroup = null;
            string role = string.Empty;
            
            try
            {
                // Enforce email verification before proceeding
                var isEmailVerified = await _roleValidator.ValidateEmailVerifiedAsync(user, cancellationToken);
                if (!isEmailVerified)
                {
                    _logger.LogWarning("User {email} email verification failed", email);
                    return UserContextState.Anonymous;
                }

                // Use UserRoleService for single optimized query instead of 4 separate queries
                var roleInfo = await _userRoleService.GetUserRoleAsync(email, cancellationToken);
                
                isStudentRegistered = roleInfo.StudentId.HasValue;
                isCompanyRegistered = roleInfo.CompanyId.HasValue;
                isProfessorRegistered = roleInfo.ProfessorId.HasValue;
                isResearchGroupRegistered = roleInfo.ResearchGroupId.HasValue;
                
                // Fetch full entity data only for the active role (one query instead of 4)
                if (roleInfo.StudentId.HasValue)
                {
                    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    student = await context.Students.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == roleInfo.StudentId.Value, cancellationToken);
                }
                else if (roleInfo.CompanyId.HasValue)
                {
                    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    company = await context.Companies.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == roleInfo.CompanyId.Value, cancellationToken);
                }
                else if (roleInfo.ProfessorId.HasValue)
                {
                    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    professor = await context.Professors.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == roleInfo.ProfessorId.Value, cancellationToken);
                }
                else if (roleInfo.ResearchGroupId.HasValue)
                {
                    await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                    researchGroup = await context.ResearchGroups.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == roleInfo.ResearchGroupId.Value, cancellationToken);
                }
                
                // Determine role from UserRoleService result
                role = roleInfo.Role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve user context");
                // Fallback to claim-based role on error
                role = ResolveRole(user);
            }
            
            // Cache the result for 5 minutes
            var userContextState = new UserContextState(
                true,
                    role,
                    email,
                    isStudentRegistered,
                    isCompanyRegistered,
                    isProfessorRegistered,
                    isResearchGroupRegistered,
                    student,
                    company,
                    professor,
                    researchGroup,
                    DateTimeOffset.UtcNow
            );
            
            await _cacheService.SetAsync(cacheKey, userContextState, TimeSpan.FromMinutes(5));
            
            return userContextState;
        }

        private static string ResolveRole(ClaimsPrincipal user)
        {
            // Try multiple claim types for role (matching MainLayout.razor.cs pattern)
            return user.FindFirst(ClaimTypes.Role)?.Value
                ?? user.FindFirst(
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                )?.Value
                ?? user.FindFirst("role")?.Value
                ?? string.Empty;
        }

        private static string ResolveEmail(ClaimsPrincipal user)
        {
            // Try multiple claim types for email (matching MainLayout.razor.cs pattern)
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("name")?.Value // Auth0 sometimes uses "name" for email
                ?? user.FindFirst("email")?.Value
                ?? user.Identity?.Name
                ?? string.Empty;
        }
    }
}
