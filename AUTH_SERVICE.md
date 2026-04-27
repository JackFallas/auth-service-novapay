# NovaPay — Auth Service

Microservicio de autenticación y gestión de usuarios de la plataforma **NovaPay**. Construido con **ASP.NET Core 8** y **PostgreSQL**, expone los JWT que consumen `Novapay-admin` y `Novapay-user`.

---

## Vista general

| Recurso | URL (local) |
|---|---|
| Swagger UI | `http://localhost:3000/swagger` |
| Health check | `http://localhost:3000/health` |
| OpenAPI JSON (Postman) | `http://localhost:3000/swagger/v1/swagger.json` |

---

## Flujo completo paso a paso

### Paso 1 — Registrar un usuario

```
POST /api/v1/auth/register
Content-Type: multipart/form-data
```

**Body (form-data):**

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `name` | string | ✅ | Nombre del usuario |
| `surname` | string | ✅ | Apellido |
| `username` | string | ✅ | Nombre de usuario único |
| `email` | string | ✅ | Correo electrónico único |
| `password` | string | ✅ | Mínimo 8 caracteres |
| `phone` | string | ❌ | Teléfono de contacto |
| `profilePicture` | file | ❌ | JPG/PNG, máx. 10 MB |

**Respuesta exitosa:** `201 Created`

```json
{
  "success": true,
  "message": "Usuario registrado exitosamente. Por favor, verifica tu email para activar la cuenta.",
  "emailVerificationRequired": true,
  "user": { "id": "...", "email": "...", "username": "..." }
}
```

> **Importante:** El servicio envía automáticamente un correo de verificación a la dirección registrada. La cuenta permanece inactiva hasta completar el Paso 2.

---

### Paso 2 — Verificar el email

```
POST /api/v1/auth/verify-email
Content-Type: application/json
```

**Body:**

```json
{
  "token": "<token-del-correo-de-verificación>"
}
```

El token llega en el enlace del correo. Extráelo de la URL o cópialo directamente del cuerpo del email.

**Respuesta exitosa:** `200 OK`

```json
{
  "success": true,
  "message": "Email verificado exitosamente",
  "data": { "email": "usuario@ejemplo.com", "verified": true }
}
```

**Errores comunes:**

| Código | Causa |
|---|---|
| `400` | Token inválido o expirado (solicitar uno nuevo con `/resend-verification`) |

---

### Paso 3 — Login

```
POST /api/v1/auth/login
Content-Type: application/json
```

**Body:**

```json
{
  "emailOrUsername": "usuario@ejemplo.com",
  "password": "tuContraseña"
}
```

Puedes autenticarte con **email** o con **username** indistintamente.

**Respuesta exitosa:** `200 OK`

```json
{
  "success": true,
  "message": "Login exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-26T21:00:00.000Z",
  "userDetails": {
    "id": "...",
    "username": "...",
    "profilePicture": "...",
    "role": "USER_ROLE"
  }
}
```

> **Copia el valor de `token`** — lo necesitas en los pasos 4 y 5.

**Errores comunes:**

| Código | Causa |
|---|---|
| `401` | Credenciales inválidas o cuenta no verificada |

---

### Paso 4 — Autorizar en Swagger

1. Abre `http://localhost:3000/swagger`.
2. Haz clic en el botón **Authorize** (candado, esquina superior derecha).
3. En el campo **Value**, escribe exactamente:

   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

   > Respeta el espacio entre `Bearer` y el token. Sin él, recibirás `401`.

4. Haz clic en **Authorize** y luego en **Close**.

A partir de este momento, todas las peticiones desde Swagger incluirán el header `Authorization: Bearer <token>` de forma automática.

---

### Paso 5 — Probar un endpoint protegido

```
GET /api/v1/auth/profile
Authorization: Bearer <token>
```

No requiere body. Devuelve el perfil completo del usuario dueño del JWT.

**Respuesta exitosa:** `200 OK`

```json
{
  "success": true,
  "message": "Perfil obtenido exitosamente",
  "data": {
    "id": "...",
    "name": "...",
    "surname": "...",
    "username": "...",
    "email": "...",
    "profilePicture": "...",
    "phone": "...",
    "role": "USER_ROLE",
    "status": true,
    "isEmailVerified": true,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```

**Errores comunes:**

| Código | Causa |
|---|---|
| `401` | Token ausente, malformado o expirado |
| `404` | El usuario del token ya no existe en la base de datos |

---

## Exportar a Postman

1. Abre Postman → **Import**.
2. Selecciona **Link** y pega:
   ```
   http://localhost:3000/swagger/v1/swagger.json
   ```
3. Postman importa todos los endpoints automáticamente con sus esquemas de body y parámetros.

---

## Referencia completa de endpoints

### `POST /api/v1/auth/register`

Registra un nuevo usuario. Responde `201` y envía email de verificación.

---

### `POST /api/v1/auth/verify-email`

Activa la cuenta usando el token recibido por correo.

---

### `POST /api/v1/auth/login`

Autentica al usuario y devuelve el JWT. La cuenta debe estar verificada.

---

### `GET /api/v1/auth/profile`

Devuelve el perfil del usuario autenticado. **Requiere JWT.**

---

### `POST /api/v1/profile/by-id`

Consulta el perfil de cualquier usuario por su ID. Útil para comunicación entre microservicios.

```json
{ "userId": "<uuid-del-usuario>" }
```

**Respuestas:**

| Código | Descripción |
|---|---|
| `200` | Perfil encontrado |
| `400` | `userId` ausente en el body |
| `404` | Usuario no encontrado |

---

### `POST /api/v1/auth/resend-verification`

Reenvía el correo de verificación cuando el token original expiró.

```json
{ "email": "usuario@ejemplo.com" }
```

**Respuestas:**

| Código | Descripción |
|---|---|
| `200` | Correo enviado exitosamente |
| `400` | El email ya fue verificado |
| `404` | No existe usuario con ese email |
| `503` | Error del servicio SMTP (revisar configuración SMTP en `.env`) |

---

### `POST /api/v1/auth/forgot-password`

Inicia el flujo de recuperación de contraseña. **Siempre responde `200`** — incluso si el email no existe en la base de datos (medida de seguridad para no revelar qué cuentas existen).

```json
{ "email": "usuario@ejemplo.com" }
```

**Respuestas:**

| Código | Descripción |
|---|---|
| `200` | Correo enviado si el email existe (respuesta genérica) |
| `503` | Fallo al enviar el email (SMTP no disponible) |

---

### `POST /api/v1/auth/reset-password`

Establece una nueva contraseña usando el token recibido por correo.

```json
{
  "token": "<token-del-correo-de-recuperación>",
  "newPassword": "nuevaContraseñaSegura"
}
```

> La contraseña debe tener **mínimo 8 caracteres**.

**Respuestas:**

| Código | Descripción |
|---|---|
| `200` | Contraseña actualizada exitosamente |
| `400` | Token inválido o expirado |

---

### `GET /health`

Health check del servicio. No requiere autenticación.

**Respuesta:** `200 OK`

```json
{
  "status": "Healthy",
  "timestamp": "2026-04-26T21:00:00.000Z"
}
```

---

## Rate Limiting

El servicio aplica rate limiting por IP para prevenir abuso:

| Política | Endpoints | Límite |
|---|---|---|
| `AuthPolicy` | `register`, `login`, `resend-verification`, `forgot-password`, `reset-password` | 5 req / minuto |
| `ApiPolicy` | `profile/by-id`, `verify-email` | 100 tokens, +20/min |

Al superar el límite se recibe `429 Too Many Requests`.

---

## Variables de entorno

Copia `.env.example` a `.env` y completa los valores antes de levantar el servicio:

```bash
cp auth-service-novapay/.env.example auth-service-novapay/.env
```

Las variables críticas son:

| Variable | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión PostgreSQL |
| `JwtSettings__SecretKey` | Clave secreta para firmar JWT (mínimo 32 chars) |
| `SmtpSettings__Username` / `Password` | Credenciales Gmail + App Password |
| `CloudinarySettings__*` | Credenciales Cloudinary para subida de avatares |

---

## Levantar con Docker

```bash
cd auth-service-novapay
docker build -t novapay-auth-service .
docker run -p 3000:3000 --env-file .env novapay-auth-service
```

El servicio queda disponible en `http://localhost:3000`.
