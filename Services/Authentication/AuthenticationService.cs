using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Service for managing authentication state and extracting user information from claims
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return Task.FromResult(user?.Identity?.IsAuthenticated ?? false);
        }

        public Task<string> GetUserEmailAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var email = user?.FindFirst("name")?.Value ?? string.Empty;
            return Task.FromResult(email);
        }

        public Task<string> GetUserRoleAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var role = user?.FindFirst("http://schemas.microsoft.com/identity/claims/role")?.Value 
                     ?? user?.FindFirst("role")?.Value 
                     ?? string.Empty;
            return Task.FromResult(role);
        }

        public Task<string> GetUserIdAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst("sub")?.Value ?? string.Empty;
            return Task.FromResult(userId);
        }

        public Task<ClaimsPrincipal> GetClaimsPrincipalAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return Task.FromResult(user);
        }
    }
}
