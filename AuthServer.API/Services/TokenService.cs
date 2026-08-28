using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<string> CrearTokenAsync(ApplicationUser usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, usuario.Id),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("nombreCompleto", usuario.NombreCompleto ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(usuario);
        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var secretKey = _config["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("Falta configurar JwtSettings:SecretKey.");
            
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _ = double.TryParse(_config["JwtSettings:ExpirationInMinutes"], out double expirationMinutes);
        if (expirationMinutes <= 0) expirationMinutes = 60;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}