using AuthServer.API.DTOs;
using Microsoft.AspNetCore.Authentication;

namespace AuthServer.API.Services;

public interface IGoogleOAuthService
{
    AuthenticationProperties CrearPropertiesChallenge(string redirectUrl);
    Task<GoogleLoginResultDto> LoginConGoogleAsync();
}