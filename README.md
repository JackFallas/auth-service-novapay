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

## Ejecucion local (sin Docker)

```bash
dotnet restore
dotnet run --project src/AuthService.Api
```

Asegurate de que PostgreSQL este corriendo y que el archivo `.env` este configurado correctamente.

---

## Ejecucion con Docker

Desde la carpeta raiz de NovaPay:

```bash
docker-compose up --build auth-service
```

O de forma individual:

```bash
cd auth-service-novapay
docker build -t novapay-auth-service .
docker run -p 3000:3000 --env-file .env novapay-auth-service
```

Swagger disponible en `http://localhost:3000/swagger`.

---

## Errores comunes

### Redireccion HTTPS (error 301/302)

El auth-service redirige HTTP a HTTPS por defecto. En Docker, establece `DISABLE_HTTPS_REDIRECT: "true"` en el `docker-compose.yml` bajo el servicio `auth-service`.

### Token SMTP de Gmail no funciona

Google requiere una **App Password**, no la contraseña normal de Gmail. Generala en: Google Account > Security > 2-Step Verification > App Passwords.

### Rate Limiting (error 429)

El servicio limita a 5 requests/minuto en endpoints de auth (login, register, forgot-password). Si superas el limite, espera un minuto o reinicia el contenedor.

---

## Tablas en `novapay_db`

| Tabla | Gestionada por | Descripción |
|---|---|---|
| `roles` | Sequelize + auth-service (seed) | Roles del sistema |
| `users` | Sequelize + auth-service (registro/login) | Usuarios de la plataforma |
| `user_emails` | auth-service exclusivo | Tokens de verificación de email |
| `user_password_resets` | auth-service exclusivo | Tokens de recuperación de contraseña |
