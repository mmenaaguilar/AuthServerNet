using AuthServer.API.Models;

namespace AuthServer.API.Services;

public interface ITokenService
{
    Task<string> CrearTokenAsync(ApplicationUser usuario);
}