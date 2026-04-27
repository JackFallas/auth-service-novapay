using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

/// <summary>
/// DTO para solicitar el perfil de un usuario por su identificador
/// </summary>
public class GetProfileByIdDto
{
    /// <summary>
    /// Identificador único del usuario
    /// </summary>
    /// <example>12345</example>
    [Required(ErrorMessage = "El userId es requerido")]
    public string UserId { get; set; } = string.Empty;
}