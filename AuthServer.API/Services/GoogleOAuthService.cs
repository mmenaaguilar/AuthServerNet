using System.Security.Claims;
using AuthServer.API.DTOs;
using AuthServer.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.API.Services;

public class GoogleOAuthService : IGoogleOAuthService
{
    private const string ProveedorGoogle = "Google";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GoogleOAuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthenticationProperties CrearPropertiesChallenge(string redirectUrl)
    {
        return _signInManager.ConfigureExternalAuthenticationProperties(
            ProveedorGoogle, redirectUrl);
    }

    public async Task<GoogleLoginResultDto> LoginConGoogleAsync()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            var httpContext = _httpContextAccessor.HttpContext 
                ?? throw new InvalidOperationException("No hay contexto HTTP disponible.");

            var authResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

            if (!authResult.Succeeded || authResult.Principal == null)
            {
                throw new InvalidOperationException("No se pudo obtener la información del login externo desde la cookie Identity.External.");
            }

            var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(providerKey))
            {
                throw new InvalidOperationException("No se encontró la clave del proveedor (ProviderKey/Subject).");
            }

            info = new ExternalLoginInfo(
                authResult.Principal,
                ProveedorGoogle,
                providerKey,
                ProveedorGoogle)
            {
                AuthenticationProperties = authResult.Properties
            };
        }

        var resultadoSignIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        ApplicationUser usuario;

        if (resultadoSignIn.Succeeded)
        {
            usuario = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                ?? throw new InvalidOperationException("No se encontró el usuario vinculado con la cuenta de Google.");
        }
        else
        {
            usuario = await ObtenerOCrearUsuarioLocalAsync(info);
            var resultadoVincular = await _userManager.AddLoginAsync(usuario, info);
            if (!resultadoVincular.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo vincular la cuenta de Google: "
                    + string.Join("; ", resultadoVincular.Errors.Select(e => e.Description)));
            }
        }

        var token = await _tokenService.CrearTokenAsync(usuario);

        return new GoogleLoginResultDto
        {
            Token = token,
            Email = usuario.Email ?? string.Empty,
            NombreCompleto = usuario.NombreCompleto ?? string.Empty
        };
    }

    private async Task<ApplicationUser> ObtenerOCrearUsuarioLocalAsync(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Google no proporcionó un correo electrónico.");
        }

        if (!EmailVerificado(info))
        {
            throw new InvalidOperationException("El correo electrónico de Google no está verificado.");
        }

        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario != null)
        {
            return usuario;
        }

        var nuevoUsuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NombreCompleto = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
            FechaCreacion = DateTime.UtcNow
        };

        var resultadoCrear = await _userManager.CreateAsync(nuevoUsuario);
        if (!resultadoCrear.Succeeded)
        {
            throw new InvalidOperationException(
                "No se pudo crear el usuario local: "
                + string.Join("; ", resultadoCrear.Errors.Select(e => e.Description)));
        }

        return nuevoUsuario;
    }

    private static bool EmailVerificado(ExternalLoginInfo info)
    {
        var emailVerificado = info.Principal.FindFirstValue("email_verified");
        return string.IsNullOrEmpty(emailVerificado) || (bool.TryParse(emailVerificado, out var verificado) && verificado);
    }
}