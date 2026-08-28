using AuthServer.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IGoogleOAuthService googleOAuthService,
        ILogger<OAuthController> logger)
    {
        _googleOAuthService = googleOAuthService;
        _logger = logger;
    }

    [HttpGet("google/login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult LoginGoogle([FromQuery] string redirectUrl = "http://127.0.0.1:5500/index.html")
    {

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(CallbackGoogle), "OAuth")
        };
        
        properties.Items["FrontendRedirectUrl"] = redirectUrl;

        return Challenge(properties, "Google");
    }

    [HttpGet("google/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CallbackGoogle()
    {

        try
        {
            var resultado = await _googleOAuthService.LoginConGoogleAsync();

            var authResult = await HttpContext.AuthenticateAsync("Identity.External");
            
            var frontendUrl = authResult?.Properties?.Items["FrontendRedirectUrl"] 
                              ?? "http://127.0.0.1:5500/index.html";

            var separator = frontendUrl.Contains("?") ? "&" : "?";
            var targetUrl = $"{frontendUrl}{separator}token={resultado.Token}&email={Uri.EscapeDataString(resultado.Email)}";

            return Redirect(targetUrl);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { mensaje = ex.Message });
        }
    }
}