using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Interfaces;
using AuthService.Persistence.Data;
using AuthService.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Conecta a novapay_db — la misma instancia PostgreSQL que usan Novapay-admin y Novapay-user.
        // Las columnas de users/roles se mapean explícitamente en ApplicationDbContext para respetar
        // los timestamps camelCase que genera Sequelize ("createdAt", "updatedAt").
        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuthService, Application.Services.AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        // SwaggerGen con Info y esquema Bearer se registra directamente en Program.cs.
        services.AddEndpointsApiExplorer();

        return services;
    }
}
