using AuthServer.API.DTOs;
using AuthServer.API.Models;
using AuthServer.API.Services; // <-- Agregado para usar ITokenService
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService; 

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registra un nuevo usuario en la plataforma.
    /// </summary>
    [HttpPost("registro")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registro([FromBody] RegistroDto dto)
    {
        var usuarioExiste = await _userManager.FindByEmailAsync(dto.Email);
        if (usuarioExiste != null)
        {
            return BadRequest(new { mensaje = "El correo electrónico ya se encuentra registrado." });
        }

        var nuevoUsuario = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            FechaCreacion = DateTime.UtcNow
        };

        var resultado = await _userManager.CreateAsync(nuevoUsuario, dto.Password);

        if (!resultado.Succeeded)
        {
            var errores = resultado.Errors.Select(e => e.Description);
            return BadRequest(new { mensaje = "Error al crear el usuario.", errores });
        }

        return Ok(new { mensaje = "Usuario registrado exitosamente." });
    }

    /// <summary>
    /// Autentica las credenciales de un usuario y retorna el JWT.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email);
        if (usuario == null)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, dto.Password, lockoutOnFailure: false);
        if (!resultado.Succeeded)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        var token = await _tokenService.CrearTokenAsync(usuario);

        return Ok(new 
        { 
            mensaje = "Login exitoso", 
            token = token,
            email = usuario.Email,
            nombre = usuario.NombreCompleto 
        });
    }
}