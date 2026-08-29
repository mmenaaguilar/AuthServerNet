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
    public IActionResult Login([FromRoute] string proveedor, [FromQuery] string redirectUrl = "")
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return BadRequest(new { mensaje = "La url es obligatorio." });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), "OAuth", new { proveedor })
        };
        
        properties.Items["FrontendRedirectUrl"] = redirectUrl;

        return Challenge(properties, proveedor);
    }

    [HttpGet("{proveedor}/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            
            if (authResult?.Properties?.Items.TryGetValue("FrontendRedirectUrl", out var frontendUrl) != true || string.IsNullOrWhiteSpace(frontendUrl))
            {
                return BadRequest(new { mensaje = "No se encontró la URL de redirección del frontend en la sesión de autenticación." });
            }

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