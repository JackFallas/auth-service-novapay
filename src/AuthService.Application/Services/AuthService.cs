using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Exceptions;
using AuthService.Application.Extensions;
using AuthService.Domain.Constants;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AuthService.Application.DTOs.Email;

namespace AuthService.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await userRepository.ExistsByEmailAsync(registerDto.Email))
        {
            logger.LogRegistrationWithExistingEmail();
            throw new BusinessException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El email ya está registrado");
        }

        if (await userRepository.ExistsByUsernameAsync(registerDto.Username))
        {
            logger.LogRegistrationWithExistingUsername();
            throw new BusinessException(ErrorCodes.USERNAME_ALREADY_EXISTS, "El username ya está en uso");
        }

        if (await userRepository.ExistsByDpiAsync(registerDto.Dpi))
            throw new BusinessException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El DPI ya está registrado");

        if (await userRepository.ExistsByNitAsync(registerDto.Nit))
            throw new BusinessException(ErrorCodes.EMAIL_ALREADY_EXISTS, "El NIT ya está registrado");

        var defaultRole = await roleRepository.GetByNameAsync(RoleConstants.CLIENTE);
        if (defaultRole == null)
            throw new InvalidOperationException(
                $"Rol por defecto '{RoleConstants.CLIENTE}' no encontrado. Asegúrate de que el seed haya corrido antes del registro.");

        var emailVerificationToken = TokenGenerator.GenerateEmailVerificationToken();

        var user = new User
        {
            Nombre = registerDto.Nombre,
            Apellido = registerDto.Apellido,
            Username = registerDto.Username,
            Email = registerDto.Email.ToLowerInvariant(),
            Dpi = registerDto.Dpi,
            Nit = registerDto.Nit,
            Telefono = registerDto.Telefono,
            Direccion = registerDto.Direccion,
            NombreTrabajo = registerDto.NombreTrabajo,
            IngresosMensuales = registerDto.IngresosMensuales,
            Password = passwordHashService.HashPassword(registerDto.Password),
            Active = false,
            RoleId = defaultRole.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserEmail = new UserEmail
            {
                EmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            },
            UserPasswordReset = new UserPasswordReset
            {
                PasswordResetToken = null,
                PasswordResetTokenExpiry = null
            }
        };

        var createdUser = await userRepository.CreateAsync(user);
        logger.LogUserRegistered(createdUser.Username);

        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendEmailVerificationAsync(createdUser.Email, createdUser.Username, emailVerificationToken);
                logger.LogInformation("Verification email sent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send verification email");
            }
        });

        return new RegisterResponseDto
        {
            Success = true,
            User = MapToUserResponseDto(createdUser),
            Message = "Usuario registrado exitosamente. Por favor, verifica tu email para activar la cuenta.",
            EmailVerificationRequired = true
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        User? user = null;

        if (loginDto.EmailOrUsername.Contains('@'))
            user = await userRepository.GetByEmailAsync(loginDto.EmailOrUsername.ToLowerInvariant());
        else
            user = await userRepository.GetByUsernameAsync(loginDto.EmailOrUsername);

        if (user == null)
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("Credenciales inválidas");
        }

        if (!user.Active)
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("Cuenta desactivada o pendiente de verificación de email");
        }

        if (!passwordHashService.VerifyPassword(loginDto.Password, user.Password))
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("Credenciales inválidas");
        }

        logger.LogUserLoggedIn();

        var token = jwtTokenService.GenerateToken(user);
        var expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryInMinutes"] ?? "120");

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login exitoso",
            Token = token,
            UserDetails = MapToUserDetailsDto(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public async Task<EmailResponseDto> VerifyEmailAsync(VerifyEmailDto verifyEmailDto)
    {
        var user = await userRepository.GetByEmailVerificationTokenAsync(verifyEmailDto.Token);
        if (user == null || user.UserEmail == null)
        {
            return new EmailResponseDto
            {
                Success = false,
                Message = "Token de verificación inválido o expirado"
            };
        }

        user.UserEmail.EmailVerified = true;
        user.Active = true;
        user.UserEmail.EmailVerificationToken = null;
        user.UserEmail.EmailVerificationTokenExpiry = null;

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email, user.Username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        logger.LogInformation("Email verified successfully for user {Username}", user.Username);

        return new EmailResponseDto
        {
            Success = true,
            Message = "Email verificado exitosamente",
            Data = new { email = user.Email, verified = true }
        };
    }

    public async Task<EmailResponseDto> ResendVerificationEmailAsync(ResendVerificationDto resendDto)
    {
        var user = await userRepository.GetByEmailAsync(resendDto.Email);
        if (user == null || user.UserEmail == null)
        {
            return new EmailResponseDto
            {
                Success = false,
                Message = "Usuario no encontrado",
                Data = new { email = resendDto.Email, sent = false }
            };
        }

        if (user.UserEmail.EmailVerified)
        {
            return new EmailResponseDto
            {
                Success = false,
                Message = "El email ya ha sido verificado",
                Data = new { email = user.Email, verified = true }
            };
        }

        var newToken = TokenGenerator.GenerateEmailVerificationToken();
        user.UserEmail.EmailVerificationToken = newToken;
        user.UserEmail.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendEmailVerificationAsync(user.Email, user.Username, newToken);
            return new EmailResponseDto
            {
                Success = true,
                Message = "Email de verificación enviado exitosamente",
                Data = new { email = user.Email, sent = true }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            return new EmailResponseDto
            {
                Success = false,
                Message = "Error al enviar el email de verificación",
                Data = new { email = user.Email, sent = false }
            };
        }
    }

    public async Task<EmailResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var user = await userRepository.GetByEmailAsync(forgotPasswordDto.Email);
        if (user == null)
        {
            // Por seguridad, siempre devolvemos éxito aunque el usuario no exista
            return new EmailResponseDto
            {
                Success = true,
                Message = "Si el email existe, se ha enviado un enlace de recuperación",
                Data = new { email = forgotPasswordDto.Email, initiated = true }
            };
        }

        var resetToken = TokenGenerator.GeneratePasswordResetToken();

        if (user.UserPasswordReset == null)
        {
            user.UserPasswordReset = new UserPasswordReset
            {
                UserId = user.Id,
                PasswordResetToken = resetToken,
                PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
            };
        }
        else
        {
            user.UserPasswordReset.PasswordResetToken = resetToken;
            user.UserPasswordReset.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        }

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendPasswordResetAsync(user.Email, user.Username, resetToken);
            logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return new EmailResponseDto
        {
            Success = true,
            Message = "Si el email existe, se ha enviado un enlace de recuperación",
            Data = new { email = forgotPasswordDto.Email, initiated = true }
        };
    }

    public async Task<EmailResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var user = await userRepository.GetByPasswordResetTokenAsync(resetPasswordDto.Token);
        if (user == null || user.UserPasswordReset == null)
        {
            return new EmailResponseDto
            {
                Success = false,
                Message = "Token de reset inválido o expirado",
                Data = new { token = resetPasswordDto.Token, reset = false }
            };
        }

        user.Password = passwordHashService.HashPassword(resetPasswordDto.NewPassword);
        user.UserPasswordReset.PasswordResetToken = null;
        user.UserPasswordReset.PasswordResetTokenExpiry = null;

        await userRepository.UpdateAsync(user);

        logger.LogInformation("Password reset successfully for user {Username}", user.Username);

        return new EmailResponseDto
        {
            Success = true,
            Message = "Contraseña actualizada exitosamente",
            Data = new { email = user.Email, reset = true }
        };
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(string userId)
    {
        if (!int.TryParse(userId, out var id))
            return null;

        try
        {
            var user = await userRepository.GetByIdAsync(id);
            return MapToUserResponseDto(user);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static UserResponseDto MapToUserResponseDto(User user) => new()
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
        Role = user.Role?.Name ?? RoleConstants.DEFAULT_ROLE,
        Active = user.Active,
        IsEmailVerified = user.UserEmail?.EmailVerified ?? false,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static UserDetailsDto MapToUserDetailsDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Nombre = user.Nombre,
        Apellido = user.Apellido,
        Role = user.Role?.Name ?? RoleConstants.DEFAULT_ROLE
    };
}
