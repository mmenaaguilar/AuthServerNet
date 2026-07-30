using Microsoft.AspNetCore.Identity;

namespace AuthServer.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? NombreCompleto { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}