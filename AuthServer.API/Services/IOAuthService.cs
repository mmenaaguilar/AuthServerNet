using AuthServer.API.DTOs;
using Microsoft.AspNetCore.Authentication;

namespace AuthServer.API.Services;

public interface IOAuthService
{
    AuthenticationProperties CrearPropertiesChallenge(string redirectUrl);
    Task<GoogleLoginResultDto> LoginConProveedorAsync();
}