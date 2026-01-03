using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Service for managing authentication state and extracting user information from claims
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Gets the current user's email from claims
        /// </summary>
        Task<string> GetUserEmailAsync();

        /// <summary>
        /// Gets the current user's role from claims
        /// </summary>
        Task<string> GetUserRoleAsync();

        /// <summary>
        /// Gets the current user's Auth0 user ID from claims
        /// </summary>
        Task<string> GetUserIdAsync();

        /// <summary>
        /// Gets the current ClaimsPrincipal
        /// </summary>
        Task<ClaimsPrincipal> GetClaimsPrincipalAsync();
    }
}
