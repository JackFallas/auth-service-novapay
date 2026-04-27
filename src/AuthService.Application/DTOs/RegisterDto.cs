using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RegisterDto
{
    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Dpi { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Nit { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string NombreTrabajo { get; set; } = string.Empty;

    [Required]
    [Range(100, double.MaxValue, ErrorMessage = "Los ingresos mensuales deben ser mayores a Q100.")]
    public decimal IngresosMensuales { get; set; }
}
