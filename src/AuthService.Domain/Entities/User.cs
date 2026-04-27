using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Domain.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

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
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

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
    [Column(TypeName = "decimal(10,2)")]
    public decimal IngresosMensuales { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    [Required]
    public int RoleId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegación
    public Role? Role { get; set; }
    public UserEmail? UserEmail { get; set; }
    public UserPasswordReset? UserPasswordReset { get; set; }
}
