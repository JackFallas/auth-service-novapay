using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Constants;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistence.Data;

/// <summary>
/// Seed inicial para NovaPay: crea los roles del dominio y un usuario
/// Administrador por defecto si la tabla users está vacía.
///
/// Roles sembrados:
///   - Administrador
///   - Cliente
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHashService? passwordHasher = null)
    {
        foreach (var roleName in RoleConstants.AllowedRoles)
        {
            var exists = await context.Roles.AnyAsync(r => r.Name == roleName);
            if (!exists)
            {
                await context.Roles.AddAsync(new Role
                {
                    Name = roleName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        var adminEmail = "admin@novapay.local";
        var adminExists = await context.Users.AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleConstants.ADMINISTRADOR);

            if (adminRole != null)
            {
                var hasher = passwordHasher ?? new PasswordHashService();

                var admin = new User
                {
                    Nombre = "Admin",
                    Apellido = "NovaPay",
                    Username = "admin.novapay",
                    Email = adminEmail,
                    Dpi = "1234567890101",
                    Nit = "1234567890",
                    Telefono = "50000000",
                    Direccion = "Ciudad de Guatemala",
                    NombreTrabajo = "NovaPay S.A.",
                    IngresosMensuales = 10000.00m,
                    Password = hasher.HashPassword("NovaPay1234!"),
                    Active = true,
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UserEmail = new UserEmail
                    {
                        EmailVerified = true,
                        EmailVerificationToken = null,
                        EmailVerificationTokenExpiry = null
                    },
                    UserPasswordReset = new UserPasswordReset
                    {
                        PasswordResetToken = null,
                        PasswordResetTokenExpiry = null
                    }
                };

                await context.Users.AddAsync(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}
