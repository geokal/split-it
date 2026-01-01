using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Unified authentication workflow interface for managing complete authentication lifecycle
    /// </summary>
    public interface IAuthenticationFlow
    {
        /// <summary>
        /// Authenticates user and returns complete user context state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>UserContextState with authentication status, email, role, and entity IDs</returns>
        Task<QuizManager.Services.UserContext.UserContextState> AuthenticateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that user's Auth0 role matches database records
        /// </summary>
        /// <param name="user">Current ClaimsPrincipal</param>
        /// <param name="databaseRole">Role from database</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if role is valid, false otherwise</returns>
        Task<bool> ValidateRoleAsync(ClaimsPrincipal user, string databaseRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if user's email is verified in Auth0
        /// </summary>
        /// <param name="user">Current ClaimsPrincipal</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if email is verified, false otherwise</returns>
        Task<bool> IsEmailVerifiedAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs authentication attempt to audit log
        /// </summary>
        /// <param name="action">Action being performed (e.g., "Login", "Logout", "RoleValidation")</param>
        /// <param name="email">User email</param>
        /// <param name="role">User role</param>
        /// <param name="success">Whether the action succeeded</param>
        /// <param name="details">Optional additional details</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        Task LogAuthenticationAttemptAsync(string action, string email, string role, bool success, string? details = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cached user data (profiles, roles, etc.)
        /// </summary>
        /// <param name="email">User email to invalidate cache for</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        Task InvalidateCacheAsync(string email, CancellationToken cancellationToken = default);
    }
}
