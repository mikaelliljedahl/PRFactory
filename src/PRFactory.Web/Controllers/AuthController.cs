using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PRFactory.Core.Application.Services;
using System.Security.Claims;

namespace PRFactory.Web.Controllers;

/// <summary>
/// Handles OAuth authentication callbacks and user session management
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string ErrorPagePath = "/auth/error";
    private const string WelcomePagePath = "/auth/welcome";
    private const string LoginPagePath = "/auth/login";
    private const string PersonalAccountNotSupportedPath = "/auth/personal-account-not-supported";
    private const string DefaultReturnPath = "/";

    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IProvisioningService _provisioningService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IProvisioningService provisioningService,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiates external OAuth login
    /// </summary>
    /// <param name="provider">OAuth provider name (Microsoft, Google)</param>
    /// <param name="returnUrl">URL to redirect after successful login</param>
    /// <returns>Challenge result to initiate OAuth flow</returns>
    [HttpGet("login")]
    public IActionResult Login(string provider, string? returnUrl = null)
    {
        _logger.LogInformation("Initiating login with provider: {Provider}, ReturnUrl: {ReturnUrl}", provider, returnUrl);

        // Initiate external authentication challenge
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handles OAuth callback after external authentication
    /// </summary>
    /// <param name="returnUrl">URL to redirect after successful login</param>
    /// <returns>Redirect to appropriate page</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        try
        {
            // 1. Get external login info
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("External login info was null");
                return Redirect(LoginPagePath);
            }

            // 2. Extract claims and validate provider
            var (externalUserId, email, displayName, identityProvider, externalTenantId, validationResult) =
                ExtractAndValidateExternalLoginInfo(info);

            if (validationResult != null)
            {
                return validationResult; // Early return if validation failed
            }

            // Validate required claims
            if (string.IsNullOrEmpty(externalUserId))
            {
                _logger.LogError("External user ID is missing from claims");
                return Redirect(ErrorPagePath);
            }

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("Email is missing from claims");
                return Redirect(ErrorPagePath);
            }

            // 4. Provision or get existing user
            var (tenant, user, isNewTenant) = await _provisioningService.ProvisionUserAsync(
                externalUserId,
                identityProvider,
                externalTenantId,
                email,
                displayName ?? email);

            _logger.LogInformation(
                "User {Email} provisioned. IsNewTenant: {IsNewTenant}, Tenant: {TenantName}",
                email, isNewTenant, tenant.Name);

            // 5. Sign in to Identity
            var identityUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (identityUser == null)
            {
                identityUser = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var createResult = await _userManager.CreateAsync(identityUser);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create Identity user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect(ErrorPagePath);
                }

                var addLoginResult = await _userManager.AddLoginAsync(identityUser, info);
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError("Failed to add external login: {Errors}",
                        string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    return Redirect(ErrorPagePath);
                }

                _logger.LogInformation("Created Identity user for {Email}", email);
            }

            // 6. Add custom claims
            var existingClaims = await _userManager.GetClaimsAsync(identityUser);
            var claimsToAdd = new List<Claim>
            {
                new Claim("prfactory_user_id", user.Id.ToString()),
                new Claim("prfactory_tenant_id", tenant.Id.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim("identity_provider", identityProvider)
            };

            // Remove old claims and add new ones
            foreach (var claim in existingClaims.Where(c =>
                c.Type == "prfactory_user_id" ||
                c.Type == "prfactory_tenant_id" ||
                c.Type == "role" ||
                c.Type == "identity_provider"))
            {
                await _userManager.RemoveClaimAsync(identityUser, claim);
            }

            await _userManager.AddClaimsAsync(identityUser, claimsToAdd);

            _logger.LogInformation(
                "Added custom claims for user {Email}: UserId={UserId}, TenantId={TenantId}, Role={Role}",
                email, user.Id, tenant.Id, user.Role);

            // 7. Sign in
            await _signInManager.SignInAsync(identityUser, isPersistent: true);

            _logger.LogInformation("User {Email} signed in successfully", email);

            // 8. Redirect
            if (isNewTenant)
            {
                _logger.LogInformation("Redirecting new tenant to welcome page");
                return Redirect(WelcomePagePath);
            }

            // Security: Validate returnUrl to prevent open redirect attacks
            var safeReturnUrl = ValidateReturnUrl(returnUrl);
            return Redirect(safeReturnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during external login callback");
            return Redirect(ErrorPagePath);
        }
    }

    /// <summary>
    /// Validates and sanitizes return URL to prevent open redirect attacks
    /// </summary>
    /// <param name="returnUrl">User-provided return URL</param>
    /// <returns>Safe local URL or default path</returns>
    private string ValidateReturnUrl(string? returnUrl)
    {
        // If no return URL provided, use default
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultReturnPath;
        }

        // Security: Only allow local URLs (must start with /)
        // This prevents open redirect attacks to external domains
        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        // If URL is not local, log warning and redirect to default
        _logger.LogWarning("Rejected non-local return URL: {ReturnUrl}", returnUrl);
        return DefaultReturnPath;
    }

    /// <summary>
    /// Extracts and validates external login information from OAuth provider
    /// </summary>
    /// <param name="info">External login info from OAuth provider</param>
    /// <returns>Tuple containing extracted claims and optional validation error result</returns>
    private (string? externalUserId, string? email, string? displayName, string identityProvider,
        string? externalTenantId, IActionResult? validationResult) ExtractAndValidateExternalLoginInfo(ExternalLoginInfo info)
    {
        var externalUserId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var displayName = info.Principal.FindFirstValue(ClaimTypes.Name);
        var identityProvider = info.LoginProvider; // "Microsoft" or "Google"
        string? externalTenantId = null;

        // Extract tenant identifier based on provider
        if (identityProvider == "Microsoft")
        {
            externalTenantId = info.Principal.FindFirstValue("tid"); // Azure AD tenant ID
            identityProvider = "AzureAD";
            _logger.LogInformation(
                "Azure AD login: Email={Email}, TenantId={TenantId}",
                email, externalTenantId);
        }
        else if (identityProvider == "Google")
        {
            externalTenantId = info.Principal.FindFirstValue("hd"); // Google Workspace domain

            if (!string.IsNullOrEmpty(externalTenantId))
            {
                identityProvider = "GoogleWorkspace";
                _logger.LogInformation(
                    "Google Workspace login: Email={Email}, Domain={Domain}",
                    email, externalTenantId);
            }
            else
            {
                _logger.LogWarning("Personal Google account attempted to sign in: {Email}", email);
                return (null, null, null, identityProvider, null, Redirect(PersonalAccountNotSupportedPath));
            }
        }

        return (externalUserId, email, displayName, identityProvider, externalTenantId, null);
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    /// <returns>Redirect to home page</returns>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return Redirect(DefaultReturnPath);
    }
}
