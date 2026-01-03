using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    public class RoleValidator : IRoleValidator
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<RoleValidator> _logger;

        public RoleValidator(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<RoleValidator> logger
        )
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> ValidateUserRoleAsync(
            ClaimsPrincipal user,
            string databaseRole,
            CancellationToken cancellationToken = default
        )
        {
            var email = user.FindFirst("name")?.Value ?? string.Empty;
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("ValidateUserRoleAsync: User has no email claim");
                return false;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Check if user has a record in the database for the claimed role
            bool hasDatabaseRecord = databaseRole switch
            {
                "Student" => await context.Students.AnyAsync(s => s.Email == email, cancellationToken),
                "Company" => await context.Companies.AnyAsync(c => c.CompanyEmail == email, cancellationToken),
                "Professor" => await context.Professors.AnyAsync(p => p.ProfEmail == email, cancellationToken),
                "Research Group" => await context.ResearchGroups.AnyAsync(r => r.ResearchGroupEmail == email, cancellationToken),
                "Admin" => true, // Admins don't need database records
                _ => false
            };

            if (!hasDatabaseRecord)
            {
                _logger.LogWarning(
                    "ValidateUserRoleAsync: Role validation failed - User {Email} claims role {ClaimedRole} " +
                    "but has no matching record in database for {DatabaseRole}",
                    email,
                    databaseRole
                );
                
                return false;
            }

            _logger.LogDebug(
                "ValidateUserRoleAsync: Role validation successful - User {Email} has matching {DatabaseRole} record",
                email,
                databaseRole
            );
            
            return true;
        }

        public async Task<bool> ValidateEmailVerifiedAsync(
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default
        )
        {
            var email = user.FindFirst("name")?.Value ?? string.Empty;
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("ValidateEmailVerifiedAsync: User has no email claim");
                return false;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Check if user has a verified email (non-null and not empty)
            // Note: This requires EmailVerified property on role entities
            // For now, we'll check if the user exists in any role table
            // In production, EmailVerified should be added to Student, Company, Professor, ResearchGroup entities
            bool isVerified = await context.Students
                .Where(s => s.Email == email)
                .AnyAsync(cancellationToken) ||
                await context.Companies
                .Where(c => c.CompanyEmail == email)
                .AnyAsync(cancellationToken) ||
                await context.Professors
                .Where(p => p.ProfEmail == email)
                .AnyAsync(cancellationToken);

            if (!isVerified)
            {
                _logger.LogWarning(
                    "ValidateEmailVerifiedAsync: Email verification failed - User {Email} is not verified",
                    email
                );
                
                return false;
            }

            _logger.LogDebug(
                "ValidateEmailVerifiedAsync: Email verification successful - User {Email} is verified",
                email
            );
            
            return true;
        }

        public async Task<bool> HasRequiredRoleAsync(
            ClaimsPrincipal user,
            string requiredRole,
            CancellationToken cancellationToken = default
        )
        {
            var email = user.FindFirst("name")?.Value ?? string.Empty;
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("HasRequiredRoleAsync: User has no email claim");
                return false;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Check if user has a record in the database for the required role
            bool hasDatabaseRecord = requiredRole switch
            {
                "Student" => await context.Students.AnyAsync(s => s.Email == email, cancellationToken),
                "Company" => await context.Companies.AnyAsync(c => c.CompanyEmail == email, cancellationToken),
                "Professor" => await context.Professors.AnyAsync(p => p.ProfEmail == email, cancellationToken),
                "Research Group" => await context.ResearchGroups.AnyAsync(r => r.ResearchGroupEmail == email, cancellationToken),
                "Admin" => true, // Admins don't need database records
                _ => false
            };

            if (!hasDatabaseRecord)
            {
                _logger.LogWarning(
                    "HasRequiredRoleAsync: Role check failed - User {Email} does not have required role {RequiredRole}",
                    email,
                    requiredRole
                );
                
                return false;
            }

            _logger.LogDebug(
                "HasRequiredRoleAsync: Role check successful - User {Email} has required role {RequiredRole}",
                email,
                requiredRole
            );
            
            return true;
        }
    }
}
