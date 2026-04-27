using System;
using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Email;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Obtiene el perfil del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Requiere un token JWT válido en el header Authorization.
    /// </remarks>
    /// <response code="200">Perfil obtenido exitosamente.</response>
    /// <response code="401">No autorizado.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<object>> GetProfile()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            return Unauthorized();
        }

        var user = await authService.GetUserByIdAsync(userIdClaim.Value);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            success = true,
            message = "Perfil obtenido exitosamente",
            data = user
        });
    }

    /// <summary>
    /// Obtiene el perfil de un usuario mediante su ID.
    /// </summary>
    /// <remarks>
    /// Permite consultar información de un usuario enviando su ID en el cuerpo de la petición.
    /// </remarks>
    /// <param name="request">Objeto que contiene el ID del usuario.</param>
    /// <response code="200">Perfil obtenido exitosamente.</response>
    /// <response code="400">El userId es requerido.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPost("profile/by-id")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<ActionResult<object>> GetProfileById([FromBody] GetProfileByIdDto request)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest(new
            {
                success = false,
                message = "El userId es requerido"
            });
        }

        var user = await authService.GetUserByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Usuario no encontrado"
            });
        }

        return Ok(new
        {
            success = true,
            message = "Perfil obtenido exitosamente",
            data = user
        });
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <remarks>
    /// Este endpoint permite registrar un usuario enviando los datos mediante multipart/form-data.
    /// Soporta carga de imagen de perfil.
    /// </remarks>
    /// <param name="registerDto">Datos del usuario a registrar.</param>
    /// <response code="201">Usuario registrado exitosamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">El usuario ya existe.</response>
    [HttpPost("register")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<RegisterResponseDto>> Register([FromForm] RegisterDto registerDto)
    {
        var result = await authService.RegisterAsync(registerDto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Inicia sesión en el sistema.
    /// </summary>
    /// <remarks>
    /// Permite autenticar a un usuario con sus credenciales.
    /// </remarks>
    /// <param name="loginDto">Credenciales del usuario.</param>
    /// <response code="200">Login exitoso.</response>
    /// <response code="401">Credenciales inválidas.</response>
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        var result = await authService.LoginAsync(loginDto);
        return Ok(result);
    }

    /// <summary>
    /// Verifica el correo electrónico del usuario.
    /// </summary>
    /// <param name="verifyEmailDto">Token de verificación y correo.</param>
    /// <response code="200">Correo verificado correctamente.</response>
    /// <response code="400">Token inválido o expirado.</response>
    [HttpPost("verify-email")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<ActionResult<EmailResponseDto>> VerifyEmail([FromBody] VerifyEmailDto verifyEmailDto)
    {
        var result = await authService.VerifyEmailAsync(verifyEmailDto);
        return Ok(result);
    }

    /// <summary>
    /// Reenvía el correo de verificación.
    /// </summary>
    /// <param name="resendDto">Correo del usuario.</param>
    /// <response code="200">Correo reenviado exitosamente.</response>
    /// <response code="400">El correo ya está verificado.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="503">Error al enviar el correo.</response>
    [HttpPost("resend-verification")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<EmailResponseDto>> ResendVerification([FromBody] ResendVerificationDto resendDto)
    {
        var result = await authService.ResendVerificationEmailAsync(resendDto);

        if (!result.Success)
        {
            if (result.Message.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
                return NotFound(result);

            if (result.Message.Contains("ya ha sido verificado", StringComparison.OrdinalIgnoreCase))
                return BadRequest(result);

            return StatusCode(503, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Solicita recuperación de contraseña.
    /// </summary>
    /// <remarks>
    /// Siempre devuelve éxito por seguridad, incluso si el usuario no existe.
    /// </remarks>
    /// <param name="forgotPasswordDto">Correo del usuario.</param>
    /// <response code="200">Correo enviado (si aplica).</response>
    /// <response code="503">Error al enviar el correo.</response>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<EmailResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var result = await authService.ForgotPasswordAsync(forgotPasswordDto);

        if (!result.Success)
        {
            return StatusCode(503, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Restablece la contraseña del usuario.
    /// </summary>
    /// <param name="resetPasswordDto">Token y nueva contraseña.</param>
    /// <response code="200">Contraseña actualizada correctamente.</response>
    /// <response code="400">Token inválido o expirado.</response>
    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult<EmailResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var result = await authService.ResetPasswordAsync(resetPasswordDto);
        return Ok(result);
    }
}