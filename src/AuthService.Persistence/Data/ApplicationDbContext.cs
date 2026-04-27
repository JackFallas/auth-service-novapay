using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistence.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserEmail> UserEmails { get; set; }
    public DbSet<UserPasswordReset> UserPasswordResets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── roles (tabla existente de NovaPay, gestionada por Sequelize) ─────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name");
            entity.HasIndex(e => e.Name).IsUnique();
            // Sequelize genera timestamps camelCase ("createdAt", "updatedAt")
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });

        // ── users (tabla existente de NovaPay, gestionada por Sequelize) ─────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.Apellido).HasColumnName("apellido");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Dpi).HasColumnName("dpi");
            entity.HasIndex(e => e.Dpi).IsUnique();
            entity.Property(e => e.Nit).HasColumnName("nit");
            entity.HasIndex(e => e.Nit).IsUnique();
            entity.Property(e => e.Telefono).HasColumnName("telefono");
            entity.Property(e => e.Direccion).HasColumnName("direccion");
            entity.Property(e => e.NombreTrabajo).HasColumnName("nombre_trabajo");
            entity.Property(e => e.IngresosMensuales)
                  .HasColumnName("ingresos_mensuales")
                  .HasColumnType("decimal(10,2)");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            // Sequelize genera timestamps camelCase ("createdAt", "updatedAt")
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RoleId);

            entity.HasOne(e => e.UserEmail)
                  .WithOne(ue => ue.User)
                  .HasForeignKey<UserEmail>(ue => ue.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UserPasswordReset)
                  .WithOne(upr => upr.User)
                  .HasForeignKey<UserPasswordReset>(upr => upr.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── user_emails (nueva tabla, solo gestionada por auth-service) ───────
        modelBuilder.Entity<UserEmail>(entity =>
        {
            entity.ToTable("user_emails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified");
            entity.Property(e => e.EmailVerificationToken).HasColumnName("email_verification_token");
            entity.Property(e => e.EmailVerificationTokenExpiry).HasColumnName("email_verification_token_expiry");
        });

        // ── user_password_resets (nueva tabla, solo gestionada por auth-service) ─
        modelBuilder.Entity<UserPasswordReset>(entity =>
        {
            entity.ToTable("user_password_resets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token");
            entity.Property(e => e.PasswordResetTokenExpiry).HasColumnName("password_reset_token_expiry");
        });
    }
}
