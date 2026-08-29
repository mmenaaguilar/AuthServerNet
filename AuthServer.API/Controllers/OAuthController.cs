using AuthServer.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public OAuthController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet("{proveedor}/login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login([FromRoute] string proveedor, [FromQuery] string redirectUrl = "http://127.0.0.1:5500/index.html")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), "OAuth", new { proveedor })
        };
        
        properties.Items["FrontendRedirectUrl"] = redirectUrl;

        return Challenge(properties, proveedor);
    }

    [HttpGet("{proveedor}/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback([FromRoute] string proveedor)
    {
        try
        {
            IOAuthService oauthService = proveedor.ToLower() switch
            {
                "google" => _serviceProvider.GetRequiredService<GoogleOAuthService>(),
                "github" => _serviceProvider.GetRequiredService<GitHubOAuthService>(),
                _ => throw new InvalidOperationException($"El proveedor '{proveedor}' no está soportado.")
            };

            var resultado = await oauthService.LoginConProveedorAsync();

            var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            
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