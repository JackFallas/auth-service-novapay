# NovaPay Auth Service

Microservicio de **autenticación, gestión de usuarios y emisión de JWT** para la plataforma **NovaPay**.

Construido con **ASP.NET Core 8** y **Entity Framework Core**, se conecta a la misma instancia PostgreSQL (`novapay_db`) que usan `Novapay-admin` (puerto 3001) y `Novapay-user` (puerto 3002). Los tokens que emite son consumidos por ambos microservicios para validar identidad y rol.

Para la guía de uso completa, flujo paso a paso y referencia de endpoints, consulta **[AUTH_SERVICE.md](./AUTH_SERVICE.md)**.

---

## Stack

| Tecnología | Uso |
|---|---|
| ASP.NET Core 8 | Framework web |
| Entity Framework Core 8 + Npgsql | ORM y driver PostgreSQL |
| JWT Bearer | Emisión y validación de tokens |
| BCrypt.Net | Hash de contraseñas |
| Serilog | Logging estructurado |
| Swashbuckle (Swagger) | Documentación de API |
| DotNetEnv | Carga de variables de entorno en desarrollo local |

---

## Roles

| Nombre | Descripción |
|---|---|
| `Administrador` | Acceso completo al panel de administración |
| `Cliente` | Usuario final de la plataforma (rol por defecto al registrarse) |

El claim JWT `role` viaja con el nombre en texto (`"Administrador"` / `"Cliente"`), que es el mismo que los middlewares de `Novapay-admin` y `Novapay-user` esperan recibir.

---

## Estructura del JWT

```json
{
  "sub": "5",
  "jti": "uuid-v4",
  "iat": 1714000000,
  "role": "Cliente",
  "iss": "NovaPayAuthService",
  "aud": "NovaPayServices"
}
```

---

## Variables de entorno

Copia `.env.example` y completa los valores antes de levantar:

```bash
cp auth-service-novapay/.env.example auth-service-novapay/.env
```

Variables críticas:

| Variable | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a `novapay_db` |
| `JwtSettings__SecretKey` | Clave secreta JWT (mínimo 32 caracteres) |
| `SmtpSettings__Username` / `Password` | Credenciales Gmail + App Password |

---

## Levantar con Docker

```bash
cd auth-service-novapay
docker build -t novapay-auth-service .
docker run -p 3000:3000 --env-file .env novapay-auth-service
```

Swagger disponible en `http://localhost:3000/swagger`.

---

## Tablas en `novapay_db`

| Tabla | Gestionada por | Descripción |
|---|---|---|
| `roles` | Sequelize + auth-service (seed) | Roles del sistema |
| `users` | Sequelize + auth-service (registro/login) | Usuarios de la plataforma |
| `user_emails` | auth-service exclusivo | Tokens de verificación de email |
| `user_password_resets` | auth-service exclusivo | Tokens de recuperación de contraseña |
