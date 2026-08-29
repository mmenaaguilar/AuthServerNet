using System.Security.Claims;
using AuthServer.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PerfilController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PerfilController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Endpoint protegido que retorna la información cargada desde el Token JWT.
    /// </summary>
    [HttpGet("perfil")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { mensaje = "El token no contiene un identificador válido." });
        }

        var usuario = await _userManager.FindByIdAsync(userId);
        if (usuario == null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado en la base de datos." });
        }

        return Ok(new
        {
            mensaje = "¡Acceso autorizado exitosamente!",
            datosToken = new
            {
                id = userId,
                email = userEmail
            },
            datosUsuario = new
            {
                nombreCompleto = usuario.NombreCompleto,
                fechaCreacion = usuario.FechaCreacion
            }
        });
    }
}