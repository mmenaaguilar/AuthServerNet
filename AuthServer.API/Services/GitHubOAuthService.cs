using System.Security.Claims;
using AuthServer.API.DTOs;
using AuthServer.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.API.Services;

public class GitHubOAuthService : IOAuthService
{
    private const string ProveedorGitHub = "GitHub";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GitHubOAuthService(
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
            ProveedorGitHub, redirectUrl);
    }

    public async Task<GoogleLoginResultDto> LoginConProveedorAsync()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            var httpContext = _httpContextAccessor.HttpContext 
                ?? throw new InvalidOperationException("No hay contexto HTTP disponible.");

            var authResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

            if (!authResult.Succeeded || authResult.Principal == null)
            {
                throw new InvalidOperationException("No se pudo obtener la información de GitHub desde la cookie Identity.External.");
            }

            var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(providerKey))
            {
                throw new InvalidOperationException("No se encontró la clave del proveedor (ProviderKey/Subject) para GitHub.");
            }

            info = new ExternalLoginInfo(
                authResult.Principal,
                ProveedorGitHub,
                providerKey,
                ProveedorGitHub)
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
                ?? throw new InvalidOperationException("No se encontró el usuario vinculado con la cuenta de GitHub.");
        }
        else
        {
            usuario = await ObtenerOCrearUsuarioLocalAsync(info);
            var resultadoVincular = await _userManager.AddLoginAsync(usuario, info);
            if (!resultadoVincular.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo vincular la cuenta de GitHub: "
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
        // En GitHub el email puede venir en ClaimTypes.Email o urn:github:email
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) 
                    ?? info.Principal.FindFirstValue("urn:github:email");

        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("GitHub no proporcionó un correo electrónico. Asegúrate de que el usuario tenga un email verificado.");
        }

        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario != null)
        {
            return usuario;
        }

        var nombreCompleto = info.Principal.FindFirstValue(ClaimTypes.Name) 
                             ?? info.Principal.FindFirstValue("urn:github:login") 
                             ?? email;

        var nuevoUsuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NombreCompleto = nombreCompleto,
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
}