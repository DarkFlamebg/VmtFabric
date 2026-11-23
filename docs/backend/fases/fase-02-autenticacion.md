# Fase 02 · Autenticación, usuarios y auditoría

**Estado:** ✅ Terminada — 2026-11-22
**Objetivo:** que solo los usuarios autenticados usen la API, con permisos por rol, gestión de usuarios por el Administrador (sin correo) y rastro de auditoría de todo cambio.
**Requerimientos:** RF-01, RF-02, RF-51, RF-52, RNF-01, RNF-02, RNF-07.

## Alcance
- [x] Login con correo y contraseña → access token JWT (15 min) + refresh token (7 días).
- [x] Refresh token **rotativo**, guardado solo como hash SHA-256; detección de reutilización (cierra todas las sesiones del usuario).
- [x] Logout (revoca el refresh token; funciona aunque el access token haya vencido).
- [x] Cambio de contraseña; contraseña temporal obligatoria de cambiar (`DebeCambiarPassword`).
- [x] Bloqueo de cuenta: 5 intentos fallidos → 15 min (`IntentosFallidos`, `BloqueadoHasta`).
- [x] Límite de 10 peticiones por minuto y por IP en `/api/auth/*` → 429 con `Retry-After`.
- [x] Contraseñas con BCrypt (factor 12); rehash automático si cambia el factor.
- [x] Administrador inicial: el marcador `<REEMPLAZAR_CON_HASH>` del script se reemplaza al arrancar con `AdminInicial:Password`.
- [x] `GET /api/auth/me` para el header del frontend.
- [x] CRUD de usuarios (solo Administrador): listar paginado con filtros, ver, crear, editar, activar/desactivar, restablecer contraseña, listar roles.
- [x] Política de respaldo: **todo endpoint exige sesión** salvo los marcados `[AllowAnonymous]` (login, refresh, logout, `/health`, OpenAPI/Scalar en desarrollo).
- [x] Políticas `Administrador` y `Talento` (Administrador o Gestor) para las fases siguientes.
- [x] `ICurrentUser` para que los servicios conozcan al usuario sin tocar `HttpContext`.
- [x] Auditoría automática (interceptor de EF) + eventos explícitos de seguridad.
- [x] Validación con FluentValidation (filtro global) con mensajes en español; claves de `errors` en camelCase.
- [x] Fechas devueltas en UTC con `Z` (convención `UtcDateTimeConverter`).
- [x] Esquema Bearer en OpenAPI: Scalar permite probar con token.

## Endpoints

| Método | Ruta | Acceso | Respuestas |
|---|---|---|---|
| POST | `/api/auth/login` | Anónimo (rate limit) | 200 `TokenResponse` · 400 · 401 · 423 bloqueada · 429 |
| POST | `/api/auth/refresh` | Anónimo (rate limit) | 200 `TokenResponse` · 401 |
| POST | `/api/auth/logout` | Anónimo (rate limit) | 204 |
| POST | `/api/auth/cambiar-password` | Sesión (también con contraseña temporal) | 200 `TokenResponse` · 400 · 422 |
| GET | `/api/auth/me` | Sesión (también con contraseña temporal) | 200 `UsuarioActualDto` |
| GET | `/api/usuarios?buscar=&rolId=&activo=&pagina=1&tamanoPagina=20` | Administrador | 200 `PaginaResultado<UsuarioDto>` |
| GET | `/api/usuarios/{id}` | Administrador | 200 · 404 |
| POST | `/api/usuarios` | Administrador | 201 `PasswordTemporalResponse` · 400 · 409 |
| PUT | `/api/usuarios/{id}` | Administrador | 200 · 404 · 409 · 422 |
| PATCH | `/api/usuarios/{id}/estado` | Administrador | 200 · 422 |
| POST | `/api/usuarios/{id}/restablecer-password` | Administrador | 200 `PasswordTemporalResponse` · 422 |
| GET | `/api/usuarios/roles` | Administrador | 200 `RolDto[]` |

### Contrato para el frontend (Angular)
1. `POST /api/auth/login` → guardar `accessToken` (memoria) y `refreshToken`. Enviar `Authorization: Bearer <accessToken>` en cada petición.
2. Si `usuario.debeCambiarPassword` es `true`, o cualquier petición responde **403 con `codigo: "DEBE_CAMBIAR_PASSWORD"`**, llevar al usuario a la pantalla de cambio de contraseña. Al cambiarla llegan tokens nuevos.
3. Ante un 401, llamar **una vez** a `/api/auth/refresh` con el refresh token; si también da 401, volver al login. Cada refresh devuelve un refresh token **nuevo** (el anterior deja de servir).
4. 423 = cuenta bloqueada (mostrar `detail`); 429 = demasiados intentos (esperar `Retry-After` segundos).
5. Errores de validación (400): `errors` es un diccionario `{ "Campo": ["mensaje"] }`.

### Reglas de negocio de usuarios
- El servidor genera la contraseña temporal (14 caracteres, CSPRNG) y la devuelve **una sola vez**; el Administrador la entrega por un canal privado. No hay correo (DT-12).
- Correo único (sin distinguir mayúsculas), guardado en minúsculas.
- Siempre queda al menos un Administrador activo. Un usuario no puede quitarse el rol de Administrador, desactivarse ni restablecer su propia contraseña (usa "Cambiar contraseña").
- Desactivar, cambiar de rol o restablecer la contraseña cierra las sesiones del usuario (revoca sus refresh tokens).
- Restablecer también desbloquea la cuenta. Los usuarios no se eliminan: se desactivan (quedan en la auditoría).

### Política de contraseñas
8 a 64 caracteres, al menos una letra y un número, máximo 72 bytes (límite de BCrypt), distinta de la actual.

## Auditoría (S-13 / RNF-07)
- **Automática** (`Data/Interceptors/AuditoriaInterceptor.cs`): cada entidad creada, editada o eliminada genera una fila `Crear`/`Editar`/`Eliminar` con el usuario y un JSON con los valores o el `antes/después` de lo que cambió. Además completa `CreadoPor`, `ModificadoPor` y `FechaModificacion`.
  - `PasswordHash` y `TokenHash` se registran como `***`.
  - No se auditan las tablas técnicas (`RefreshToken`, `RecuperacionPassword`) ni los cambios que solo tocan `UltimoAcceso`/`IntentosFallidos`/`BloqueadoHasta`.
- **Eventos explícitos** (`IAuditoriaService`): `Login`, `LoginFallido` (con motivo), `Logout`, `CambioPassword`, `ResetPassword`, `TokenReutilizado`, con la IP de origen.

## Archivos nuevos o modificados (en `backend/`)

| Ruta | Qué hace |
|---|---|
| `Controllers/AuthController.cs`, `Controllers/UsuariosController.cs` | Endpoints (delgados) |
| `Services/Auth/AuthService.cs` | Login, bloqueo, refresh con rotación, logout, cambio de contraseña, `me` |
| `Services/Auth/AdminInicialService.cs` | Reemplaza el hash pendiente del admin inicial al arrancar |
| `Services/Usuarios/UsuarioService.cs` | CRUD de usuarios y reglas |
| `Services/Auditoria/AuditoriaService.cs` | Eventos de seguridad |
| `Security/` *(nueva)* | `TokenService` (JWT HS256), `BCryptPasswordHasher`, `PasswordGenerator`, `CurrentUser`, `ClaimNames`, `Politicas` |
| `Validators/` *(nueva)* | Validadores FluentValidation + `ReglasComunes` |
| `Filters/ValidationFilter.cs` *(nueva carpeta)* | Ejecuta los validadores antes del controller |
| `Middleware/PasswordPendienteMiddleware.cs` + `PermitirConPasswordPendienteAttribute.cs` | Bloquea todo salvo lo marcado mientras la contraseña sea temporal |
| `Data/Interceptors/AuditoriaInterceptor.cs` | Auditoría automática |
| `Models/DTOs/Auth`, `Models/DTOs/Usuarios`, `Models/DTOs/Comun` | Contratos de la API |
| `Models/Constants/AccionAuditoria.cs` | Valores de `Auditoria.Accion` |
| `Configuration/JwtSettings.cs`, `LoginSettings.cs`, `AdminInicialSettings.cs` | Opciones validadas al arrancar |
| `Exceptions/UnauthorizedException.cs`, `CuentaBloqueadaException.cs` | 401 y 423; `AppException` admite un `Codigo` estable |
| `Extensions/SecurityExtensions.cs`, `ApplicationExtensions.cs`, `BearerSecuritySchemeTransformer.cs` | Registro de JWT, políticas, rate limiting, servicios y OpenAPI |
| `Extensions/DotEnvExtensions.cs` | Lee también `JWT_KEY` (obligatoria) y `ADMIN_PASSWORD` (opcional) |
| `appsettings.json` | Secciones `Jwt` (sin la clave) y `Login` |
| `Data/Configurations/RefreshTokenConfiguration.cs` | Índices `UQ_RefreshToken_TokenHash` e `IX_RefreshToken_Usuario` |
| `../bd/cambios/002_indice_refreshtoken.sql` | Crea esos índices en la base compartida |

Paquetes nuevos: `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.15`, `BCrypt.Net-Next 4.2.0`, `FluentValidation.DependencyInjectionExtensions 12.2.0` (sin vulnerabilidades conocidas: `dotnet list package --vulnerable`).

## Orden del pipeline
1. Forwarded headers (solo con proxy) → 2. `ExceptionHandlingMiddleware` → 3. `SecurityHeadersMiddleware` → 4. `UseStatusCodePages` → (HSTS/HTTPS en prod) → 5. CORS → 6. **RateLimiter** → 7. **Authentication** → 8. **Authorization** → 9. **`PasswordPendienteMiddleware`** → 10. Controllers / `/health`.

## Configuración

| Clave | Desarrollo (`backend/.env`) | Producción (variable de entorno) |
|---|---|---|
| `Jwt:Key` (≥ 32 caracteres, secreta) | `JWT_KEY` | `Jwt__Key` |
| `AdminInicial:Password` (solo primera instalación) | `ADMIN_PASSWORD` | `AdminInicial__Password` (quitarla después) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutos`, `Jwt:RefreshTokenDias` | `appsettings.json` | — |
| `Login:MaxIntentosFallidos`, `Login:MinutosBloqueo`, `Login:PeticionesPorMinuto` | `appsettings.json` | — |

Sin `Jwt:Key` válida la API no arranca (`ValidateOnStart`).

## Verificación realizada (2026-11-22, contra la base en MonsterASP)

Script de pruebas de punta a punta: **39/39 OK**.

| Prueba | Resultado |
|---|---|
| `dotnet build` (analizadores, warnings = errores) | ✅ 0 advertencias (se corrigieron 3 CA1859) |
| Sin token → `/api/auth/me`, `/api/usuarios` | ✅ 401 ProblemDetails en español |
| Login: vacío → 400, correo inexistente → 401, contraseña errónea → 401 (mismo mensaje) | ✅ |
| Admin inicial: el marcador se reemplazó por BCrypt al arrancar; login con `ADMIN_PASSWORD` → `debeCambiarPassword=true` | ✅ |
| Con contraseña temporal: `/api/usuarios` → 403 `DEBE_CAMBIAR_PASSWORD`; `/me` → 200 | ✅ |
| Cambio de contraseña: débil → 400, actual errónea → 422, correcto → 200 y revoca las sesiones anteriores | ✅ |
| Refresh: rotación → 200; reutilizar el token usado → 401 y revoca también el rotado | ✅ |
| Usuarios: listar, roles, validación de `tamanoPagina` (400), crear Gestor (201 + temporal), correo duplicado en mayúsculas (409), rol inválido (400), editar (200), 404 | ✅ |
| Reglas: quitarse el rol de Admin, desactivarse o restablecerse a sí mismo → 422 | ✅ |
| Gestor: login temporal → cambia contraseña → `/api/usuarios` 403, `/me` 200 | ✅ |
| Restablecer y desactivar Gestor → su login da 401 | ✅ |
| Logout → 204; refresh tras logout → 401; logout repetido → 204 | ✅ |
| 11 logins seguidos desde la misma IP → el 11.º da 429 con `Retry-After: 60` | ✅ |
| Tabla `Auditoria`: Crear/Editar/Login/LoginFallido/Logout/CambioPassword/ResetPassword/TokenReutilizado; hashes como `***` | ✅ |
| OpenAPI con esquema Bearer | ✅ |

**Datos que quedaron en la base compartida:** el admin `admin@vmtfabric.com` con contraseña definitiva (guardada en el `backend/.env` del desarrollador que ejecutó las pruebas, clave `ADMIN_PASSWORD`) y el usuario **inactivo** `gestor.prueba@vmtfabric.test` (Gestor) para pruebas.

## Limitaciones conocidas
- Un access token sigue siendo válido hasta 15 min después de desactivar al usuario o cambiarle el rol (los refresh tokens sí se revocan al instante). Aceptado por la duración corta; se puede revisar en la fase 13.
- Un refresh token revocado por logout o cambio de contraseña que se vuelva a presentar se trata como reutilización (cierra todas las sesiones del usuario y se audita `TokenReutilizado`). Es más estricto de lo necesario, pero seguro.
- Sin `RecuperacionPassword` por correo: la tabla queda reservada (DT-12).
- Con la ruta inexistente y sin token, la respuesta es 401 (la política de respaldo se evalúa antes que el 404): no revela qué rutas existen.

## Pendientes
- Ejecutar `bd/cambios/002_indice_refreshtoken.sql` en la base compartida (Run T-SQL o Import SQL del panel). La API funciona sin él, pero los índices evitan recorrer la tabla en cada refresh.
- Cada integrante debe agregar `JWT_KEY` a su `.env` (ver `.env.example`).
