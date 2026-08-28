using System.ComponentModel.DataAnnotations;

namespace AuthServer.API.DTOs
{
    public class RegistroDto
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; } = string.Empty;
    }

    public class RespuestaAuthDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Expiracion { get; set; }
    }

    public class GoogleLoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}