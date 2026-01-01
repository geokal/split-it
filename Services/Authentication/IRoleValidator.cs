using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    public interface IRoleValidator
    {
        /// <summary>
        /// Validates that the user's Auth0 role claim matches their database role.
        /// This prevents privilege escalation attacks where users modify their Auth0 tokens.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="databaseRole">The role from the database (Student, Company, Professor, ResearchGroup, Admin)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the roles match, false otherwise</returns>
        Task<bool> ValidateUserRoleAsync(
            ClaimsPrincipal user,
            string databaseRole,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Validates that the user's email is verified (not null/empty).
        /// This ensures only verified users can access the system.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the email is verified, false otherwise</returns>
        Task<bool> ValidateEmailVerifiedAsync(
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Checks if the user has the required role for a specific action.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="requiredRole">The role required to perform the action</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the user has the required role, false otherwise</returns>
        Task<bool> HasRequiredRoleAsync(
            ClaimsPrincipal user,
            string requiredRole,
            CancellationToken cancellationToken = default
        );
    }
}
