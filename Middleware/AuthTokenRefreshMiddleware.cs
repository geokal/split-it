using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuizManager.Middleware
{
    /// <summary>
    /// Middleware to automatically refresh Auth0 tokens when they're close to expiration
    /// </summary>
    public class AuthTokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthTokenRefreshMiddleware> _logger;

        // Token expiration threshold - refresh if token expires within this many minutes
        private const int RefreshThresholdMinutes = 5;

        public AuthTokenRefreshMiddleware(
            RequestDelegate next,
            ILogger<AuthTokenRefreshMiddleware> logger
        )
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authResult = await context.AuthenticateAsync();
            
            if (authResult.Succeeded && authResult.Principal != null)
            {
                var expClaim = authResult.Principal.FindFirst("exp");
                
                if (expClaim != null)
                {
                    _logger.LogWarning("No 'exp' claim found in authentication token");
                    await _next(context);
                    return;
                }

                // Parse Unix timestamp from claim
                if (long.TryParse(expClaim.Value, out var expTimestamp))
                {
                    var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expTimestamp);
                    var timeUntilExpiration = expirationDate - DateTimeOffset.UtcNow;
                    
                    // Log token expiration time
                    _logger.LogDebug(
                        "Token expires at {ExpirationTime} (in {MinutesUntilExpiration} minutes)",
                        expirationDate,
                        timeUntilExpiration.TotalMinutes
                    );

                    // Check if token is close to expiration (within threshold)
                    if (timeUntilExpiration.TotalMinutes < RefreshThresholdMinutes)
                    {
                        _logger.LogInformation(
                            "Token for user {Email} is close to expiration ({MinutesUntilExpiration} minutes remaining). " +
                            "Consider refreshing authentication.",
                            authResult.Principal.FindFirst("name")?.Value ?? "unknown",
                            timeUntilExpiration.TotalMinutes
                        );

                        // Add warning header to inform client
                        context.Response.Headers.Append(
                            "X-Auth-Token-Warning",
                            $"Token expires in {timeUntilExpiration.TotalMinutes:F1} minutes"
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to parse 'exp' claim value: {ExpValue}", expClaim.Value);
                }
            }

            await _next(context);
        }
    }
}
