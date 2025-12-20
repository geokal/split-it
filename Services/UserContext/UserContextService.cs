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

namespace QuizManager.Services.UserContext
{
    public class UserContextService : IUserContextService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<UserContextService> _logger;

        public UserContextService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<UserContextService> logger)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<UserContextState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                return UserContextState.Anonymous;
            }

            var role = ResolveRole(user);
            var email = ResolveEmail(user);

            bool isStudentRegistered = false;
            bool isCompanyRegistered = false;
            bool isProfessorRegistered = false;
            bool isResearchGroupRegistered = false;
            Student? student = null;
            Company? company = null;
            Professor? professor = null;
            ResearchGroup? researchGroup = null;

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                if (!string.IsNullOrEmpty(email))
                {
                    student = await context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
                    isStudentRegistered = student != null;

                    company = await context.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyEmail == email, cancellationToken);
                    isCompanyRegistered = company != null;

                    professor = await context.Professors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProfEmail == email, cancellationToken);
                    isProfessorRegistered = professor != null;

                    researchGroup = await context.ResearchGroups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.ResearchGroupEmail == email, cancellationToken);
                    isResearchGroupRegistered = researchGroup != null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve user context");
            }

            return new UserContextState(
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
                DateTimeOffset.UtcNow);
        }

        private static string ResolveRole(ClaimsPrincipal user)
        {
            // Try multiple claim types for role (matching MainLayout.razor.cs pattern)
            return user.FindFirst(ClaimTypes.Role)?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value
                ?? user.FindFirst("role")?.Value
                ?? string.Empty;
        }

        private static string ResolveEmail(ClaimsPrincipal user)
        {
            // Try multiple claim types for email (matching MainLayout.razor.cs pattern)
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("name")?.Value  // Auth0 sometimes uses "name" for email
                ?? user.FindFirst("email")?.Value
                ?? user.Identity?.Name
                ?? string.Empty;
        }
    }
}

