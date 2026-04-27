namespace AuthService.Domain.Constants;

/// <summary>
/// Roles de NovaPay. Estos nombres viajan en el claim "role" del JWT
/// y los microservicios Novapay-admin y Novapay-user los leen para
/// aplicar control de acceso.
/// </summary>
public static class RoleConstants
{
    public const string ADMINISTRADOR = "Administrador";
    public const string CLIENTE = "Cliente";

    public static readonly string[] AllowedRoles = [ADMINISTRADOR, CLIENTE];

    public const string DEFAULT_ROLE = CLIENTE;
    public const string ADMIN_ROLE = ADMINISTRADOR;
    public const string USER_ROLE = CLIENTE;
}
