using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Constants;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class UserManagementService(IUserRepository users, IRoleRepository roles) : IUserManagementService
{
    public async Task<UserResponseDto> UpdateUserRoleAsync(string userId, string roleName)
    {
        roleName = roleName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Invalid userId", nameof(userId));

        if (!RoleConstants.AllowedRoles.Contains(roleName))
            throw new InvalidOperationException(
                $"Rol no permitido. Debe ser uno de: {string.Join(", ", RoleConstants.AllowedRoles)}");

        if (!int.TryParse(userId, out var userIdInt))
            throw new ArgumentException("userId must be a valid integer", nameof(userId));

        var user = await users.GetByIdAsync(userIdInt);

        var role = await roles.GetByNameAsync(roleName)
                       ?? throw new InvalidOperationException($"Rol '{roleName}' no encontrado en la base de datos");

        await users.UpdateUserRoleAsync(userIdInt, role.Id);
        user = await users.GetByIdAsync(userIdInt);

        return new UserResponseDto
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Username = user.Username,
            Email = user.Email,
            Dpi = user.Dpi,
            Nit = user.Nit,
            Telefono = user.Telefono,
            Direccion = user.Direccion,
            NombreTrabajo = user.NombreTrabajo,
            IngresosMensuales = user.IngresosMensuales,
            Role = role.Name,
            Active = user.Active,
            IsEmailVerified = user.UserEmail?.EmailVerified ?? false,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId)
    {
        if (!int.TryParse(userId, out var userIdInt))
            return [];

        var user = await users.GetByIdAsync(userIdInt);
        var roleName = user.Role?.Name ?? string.Empty;
        return string.IsNullOrEmpty(roleName) ? [] : [roleName];
    }

    public Task<IReadOnlyList<UserResponseDto>> GetUsersByRoleAsync(string roleName)
    {
        return Task.FromResult<IReadOnlyList<UserResponseDto>>([]);
    }
}
