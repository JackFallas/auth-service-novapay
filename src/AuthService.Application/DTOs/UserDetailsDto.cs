namespace AuthService.Application.DTOs;

public class UserDetailsDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
