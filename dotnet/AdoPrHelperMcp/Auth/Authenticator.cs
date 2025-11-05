using AdoPrHelperMcp.Models;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AdoPrHelperMcp.Auth;

/// <summary>
/// Provides authentication tokens for Azure DevOps API
/// </summary>
public interface IAuthenticator
{
    Task<AuthOptions> GetAuthOptionsAsync();
}

/// <summary>
/// OAuth-based authenticator using MSAL
/// </summary>
public class OAuthAuthenticator : IAuthenticator
{
    private const string ClientId = "0d50963b-7bb9-4fe7-94c7-a99af00b5136";
    private const string Authority = "https://login.microsoftonline.com/common";
    private static readonly string[] Scopes = ["499b84ac-1321-427f-aa17-267ca6975798/.default"];

    private IAccount? _accountId;
    private readonly IPublicClientApplication _publicClientApp;

    public OAuthAuthenticator()
    {
        _publicClientApp = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task<AuthOptions> GetAuthOptionsAsync()
    {
        var token = await GetTokenAsync();
        return new AuthOptions
        {
            Type = "interactive",
            Token = token
        };
    }

    private async Task<string> GetTokenAsync()
    {
        AuthenticationResult? authResult = null;

        // Try silent authentication first
        if (_accountId != null)
        {
            try
            {
                authResult = await _publicClientApp.AcquireTokenSilent(Scopes, _accountId)
                    .ExecuteAsync();
            }
            catch
            {
                authResult = null;
            }
        }

        // Fall back to interactive authentication
        if (authResult == null)
        {
            authResult = await _publicClientApp.AcquireTokenInteractive(Scopes)
                .WithUseEmbeddedWebView(false)
                .WithSystemWebViewOptions(new SystemWebViewOptions
                {
                    OpenBrowserAsync = OpenBrowserAsync
                })
                .ExecuteAsync();

            _accountId = authResult.Account;
        }

        if (string.IsNullOrEmpty(authResult.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain Azure DevOps OAuth token.");
        }

        return authResult.AccessToken;
    }

    private static Task OpenBrowserAsync(Uri url)
    {
        try
        {
            // Cross-platform browser opening
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url.ToString()) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url.ToString());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open browser: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Personal Access Token authenticator
/// </summary>
public class PatAuthenticator : IAuthenticator
{
    private readonly string _pat;

    public PatAuthenticator(string pat)
    {
        if (string.IsNullOrEmpty(pat))
        {
            throw new ArgumentException("Personal Access Token (PAT) cannot be null or empty.", nameof(pat));
        }

        _pat = pat;
    }

    public Task<AuthOptions> GetAuthOptionsAsync()
    {
        return Task.FromResult(new AuthOptions
        {
            Type = "pat",
            Token = _pat
        });
    }
}

/// <summary>
/// Factory for creating authenticators
/// </summary>
public static class AuthenticatorFactory
{
    public static IAuthenticator CreateAuthenticator(string authenticationType)
    {
        return authenticationType.ToLowerInvariant() switch
        {
            "pat" => CreatePatAuthenticator(),
            "interactive" => new OAuthAuthenticator(),
            _ => new OAuthAuthenticator()
        };
    }

    private static PatAuthenticator CreatePatAuthenticator()
    {
        var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
        if (string.IsNullOrEmpty(pat))
        {
            throw new InvalidOperationException(
                "Personal Access Token (PAT) not found in AZURE_DEVOPS_PAT environment variable.");
        }

        return new PatAuthenticator(pat);
    }
}
