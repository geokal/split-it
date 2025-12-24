using System;
using Microsoft.AspNetCore.Authentication;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizManager.Pages
{
  public class LoginModel : PageModel
  {
    public async Task OnGet(string? redirectUri = null, string? returnUrl = null)
    {
      var target = NormalizeTargetUri(redirectUri, returnUrl);

      var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
          .WithRedirectUri(target)
          .Build();

      await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    private static string NormalizeTargetUri(string? redirectUri, string? returnUrl)
    {
      var target = string.IsNullOrWhiteSpace(redirectUri) ? returnUrl : redirectUri;

      if (string.IsNullOrWhiteSpace(target))
      {
        return "/";
      }

      // Prevent protocol-relative URLs and absolute URLs
      if (target.StartsWith("//", StringComparison.Ordinal) || 
          target.Contains("://", StringComparison.Ordinal))
      {
        return "/";
      }

      if (!target.StartsWith("/", StringComparison.Ordinal))
      {
        target = "/" + target.TrimStart('/');
      }

      return target;
    }
  }
}
