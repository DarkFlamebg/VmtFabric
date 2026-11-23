# Guía de la API para el frontend

Referencia para conectar el frontend Angular con el backend. Se actualiza en cada fase que agregue endpoints.
**Estado:** Fase 02 (autenticación y usuarios). Ejemplos tomados de respuestas reales del 2026-11-23 (tokens ocultos con `***`).

---

## 1. Datos de conexión

| Entorno | URL base de la API | Origen del frontend permitido (CORS) |
|---|---|---|
| Desarrollo | `http://localhost:5222/api` | `http://localhost:4200` |
| Producción | *(se define en la etapa 2: `https://<sitio>.runasp.net/api`)* | URL del frontend en Render |

- Levantar la API: `cd backend` → `dotnet run --launch-profile http` (requiere `backend/.env`, ver `.env.example`).
- Documentación interactiva (solo desarrollo): `http://localhost:5222/scalar`.
- Si el frontend corre en otro puerto u origen (p. ej. `127.0.0.1:4200`), hay que agregarlo en `Cors:AllowedOrigins` de `backend/appsettings.json` o el navegador bloqueará las llamadas.

Sugerencia para Angular (`src/environments/environment.ts`):
```ts
export const environment = {
  apiUrl: 'http://localhost:5222/api'
};
```

---

## 2. Convenciones

| Tema | Regla |
|---|---|
| Formato | JSON con nombres en **camelCase** (`usuarioId`, `debeCambiarPassword`). Enviar `Content-Type: application/json`. |
| Fechas | ISO 8601 en **UTC** con `Z` (`"2026-11-23T20:33:29.598Z"`). Para mostrar: `new Date(valor)` ya la convierte a la hora local. |
| Autenticación | Cabecera `Authorization: Bearer <accessToken>` en **todas** las llamadas, salvo login, refresh y logout. |
| Paginación | Query `pagina` (desde 1) y `tamanoPagina` (1–100). Respuesta `{ items, total, pagina, tamanoPagina }`. |
| Errores | Siempre [ProblemDetails](#5-errores): `{ title, status, detail, instance, traceId, codigo?, errors? }`. Mostrar `detail` al usuario. |
| Ids de rol | `1` = Administrador, `2` = Gestor (o pedirlos a `GET /usuarios/roles`). |

---

## 3. Flujo de sesión

```
          ┌──────────── POST /auth/login ────────────┐
          │                                          ▼
   [Login] ──401/423/429──> mostrar detail      TokenResponse
                                                     │
                     usuario.debeCambiarPassword ?───┤
                         sí │                        │ no
                            ▼                        ▼
             [Cambiar contraseña]               [App]  ── cada petición con Bearer ──┐
              POST /auth/cambiar-password             │                             │
                  → TokenResponse nuevo ──────────────┘          401 → POST /auth/refresh (1 vez)
                                                                     ├─ 200: guardar tokens nuevos y repetir la petición
                                                                     └─ 401: borrar sesión → [Login]
```

Reglas para implementarlo:
1. **Guardar tokens:** `accessToken` en memoria (signal/servicio); `refreshToken` en `localStorage` para sobrevivir a la recarga. Al iniciar la app, si hay `refreshToken`, llamar a `/auth/refresh` para recuperar la sesión.
2. **Refresh una sola vez:** si `/auth/refresh` responde 401, la sesión terminó (venció, se cerró o el Administrador la revocó). No reintentar.
3. **Cada refresh devuelve un `refreshToken` nuevo**; el anterior deja de servir. Si dos pestañas o peticiones simultáneas hacen refresh con el mismo token, la API lo trata como robo y **cierra todas las sesiones**: el frontend debe compartir una única llamada de refresh en curso entre las peticiones que reciban 401.
4. **403 con `codigo: "DEBE_CAMBIAR_PASSWORD"`** en cualquier endpoint → llevar a la pantalla de cambio de contraseña.
5. **Cerrar sesión:** `POST /auth/logout` con el `refreshToken` y borrar ambos tokens localmente, aunque la llamada falle.
6. El `accessToken` dura **15 min** (`accessTokenExpira`); el `refreshToken`, **7 días** (`refreshTokenExpira`).
7. **Permisos en la interfaz:** usar `usuario.rol` (`"Administrador"` o `"Gestor"`) para mostrar u ocultar menús. Es solo visual: la API valida los permisos igualmente.

---

## 4. Endpoints

### 4.1 Autenticación (`/api/auth`)

#### `POST /auth/login` — Iniciar sesión
Acceso: anónimo · Límite: 10 peticiones/min por IP (compartido con refresh y logout).
```json
// Petición
{ "correo": "admin@vmtfabric.com", "password": "********" }
```
```json
// 200 OK → TokenResponse
{
  "accessToken": "***",
  "accessTokenExpira": "2026-11-23T20:47:52.8377781Z",
  "refreshToken": "***",
  "refreshTokenExpira": "2026-11-30T20:32:52.9067836Z",
  "usuario": {
    "usuarioId": 1,
    "nombre": "Administrador",
    "correo": "admin@vmtfabric.com",
    "rol": "Administrador",
    "debeCambiarPassword": false
  }
}
```
| Código | Cuándo | `detail` |
|---|---|---|
| 400 | Correo o contraseña vacíos | ver `errors` |
| 401 | Correo inexistente, usuario inactivo o contraseña incorrecta (mismo mensaje en los tres casos) | "Correo o contraseña incorrectos." |
| 423 | 5 intentos fallidos: cuenta bloqueada 15 min · `codigo: "CUENTA_BLOQUEADA"` | "Cuenta bloqueada por intentos fallidos. Intente de nuevo en N minuto(s)…" |
| 429 | Más de 10 intentos por minuto · cabecera `Retry-After` (segundos) | "Demasiados intentos. Espere un minuto…" |

#### `POST /auth/refresh` — Renovar tokens
Acceso: anónimo (no enviar Bearer).
```json
// Petición
{ "refreshToken": "***" }
```
200 → `TokenResponse` (igual que el login, con tokens nuevos). 401 → la sesión terminó: volver al login.

#### `POST /auth/logout` — Cerrar sesión
Acceso: anónimo. Petición: `{ "refreshToken": "***" }`. Respuesta: **204** siempre (aunque el token ya no exista).

#### `GET /auth/me` — Usuario de la sesión
Acceso: con sesión (también con contraseña temporal).
```json
// 200 OK → UsuarioActual
{ "usuarioId": 1, "nombre": "Administrador", "correo": "admin@vmtfabric.com", "rol": "Administrador", "debeCambiarPassword": false }
```

#### `POST /auth/cambiar-password` — Cambiar la propia contraseña
Acceso: con sesión (también con contraseña temporal).
```json
// Petición
{ "passwordActual": "********", "passwordNueva": "********" }
```
200 → `TokenResponse` nuevo (**reemplazar ambos tokens**: los anteriores quedan revocados y se cierran las otras sesiones del usuario).

| Código | Cuándo |
|---|---|
| 400 | La nueva no cumple la política o es igual a la actual (ver `errors.passwordNueva`) |
| 422 | "La contraseña actual no es correcta." |

**Política de contraseñas** (para validar también en el formulario): 8 a 64 caracteres, al menos una letra y un número, distinta de la actual.

---

### 4.2 Usuarios (`/api/usuarios`) — solo Administrador
Un Gestor recibe **403** en todos estos endpoints.

#### `GET /usuarios` — Listar
Query (todos opcionales): `buscar` (texto en nombre o correo), `rolId` (1/2), `activo` (true/false), `pagina` (def. 1), `tamanoPagina` (def. 20, máx. 100).
```
GET /api/usuarios?buscar=gestor&activo=true&pagina=1&tamanoPagina=20
```
```json
// 200 OK → PaginaResultado<Usuario>
{
  "items": [
    {
      "usuarioId": 1,
      "nombre": "Administrador",
      "correo": "admin@vmtfabric.com",
      "rolId": 1,
      "rol": "Administrador",
      "activo": true,
      "debeCambiarPassword": false,
      "bloqueado": false,
      "ultimoAcceso": "2026-11-23T20:33:29.5983324Z",
      "fechaCreacion": "2026-11-19T21:34:52.8200189Z"
    }
  ],
  "total": 2,
  "pagina": 1,
  "tamanoPagina": 20
}
```
`ultimoAcceso` puede ser `null` (nunca entró). `bloqueado` = bloqueado ahora por intentos fallidos.

#### `GET /usuarios/{id}` — Ver uno
200 → `Usuario` · 404 "El usuario no existe."

#### `POST /usuarios` — Crear
```json
// Petición (la contraseña NO se envía: la genera el servidor)
{ "nombre": "Ana Pérez", "correo": "ana.perez@vmtfabric.com", "rolId": 2 }
```
```json
// 201 Created → PasswordTemporalResponse
{
  "usuario": { "usuarioId": 3, "nombre": "Ana Pérez", "correo": "ana.perez@vmtfabric.com", "rolId": 2, "rol": "Gestor",
               "activo": true, "debeCambiarPassword": true, "bloqueado": false, "ultimoAcceso": null,
               "fechaCreacion": "2026-11-23T20:40:00Z" },
  "passwordTemporal": "***"
}
```
> **La contraseña temporal se muestra una sola vez.** La pantalla debe mostrarla al Administrador (con botón de copiar) y avisar que se entrega por un canal privado; no se puede volver a consultar. Si se pierde: restablecerla.

| Código | Cuándo |
|---|---|
| 400 | Nombre vacío o > 120, correo inválido o > 150, rol distinto de 1/2 |
| 409 | "Ya existe un usuario con ese correo." (no distingue mayúsculas) |

#### `PUT /usuarios/{id}` — Editar nombre, correo y rol
Petición igual a crear. 200 → `Usuario`. Si cambia el rol, se cierran las sesiones de ese usuario (debe volver a entrar).

| Código | Cuándo |
|---|---|
| 409 | Correo de otro usuario |
| 422 | Quitarse a sí mismo el rol de Administrador · dejar el sistema sin Administradores activos |

#### `PATCH /usuarios/{id}/estado` — Activar o desactivar
```json
{ "activo": false }
```
200 → `Usuario`. Desactivar cierra sus sesiones. 422 si es uno mismo o el último Administrador activo. (Los usuarios no se eliminan.)

#### `POST /usuarios/{id}/restablecer-password` — Contraseña temporal nueva
Sin cuerpo. 200 → `PasswordTemporalResponse` (misma regla: mostrarla una sola vez). También desbloquea la cuenta y cierra sus sesiones.
422 si es uno mismo ("Para su propio usuario use \"Cambiar contraseña\".") o el usuario está inactivo.

#### `GET /usuarios/roles` — Roles para los combos
```json
[
  { "rolId": 1, "nombre": "Administrador", "descripcion": "Acceso completo a todos los módulos" },
  { "rolId": 2, "nombre": "Gestor", "descripcion": "Solo módulo Talento: Colaboradores, Clientes, Plan de Carrera, Evaluaciones" }
]
```

---

### 4.3 Otros

| Método | Ruta | Uso |
|---|---|---|
| GET | `http://localhost:5222/health` | `Healthy` (200) o `Unhealthy` (503): API y base de datos. Sin `/api`. |

---

## 5. Errores

Todas las respuestas de error tienen este formato:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "No autorizado",
  "status": 401,
  "detail": "Debe iniciar sesión.",
  "instance": "GET /api/auth/me",
  "traceId": "00-6ffdc9ded70b351a99df96574b5c2d96-1d489f96f7873cd2-00"
}
```
Errores de validación (400): `errors` trae los mensajes **por campo**, con la clave igual al nombre del campo del JSON:
```json
{
  "title": "Uno o más datos no son válidos.",
  "status": 400,
  "errors": {
    "nombre": ["El nombre es obligatorio."],
    "correo": ["El correo no tiene un formato válido."],
    "rolId": ["El rol no es válido."]
  }
}
```

| Status | Significado | Qué hacer en el frontend |
|---|---|---|
| 400 | Datos inválidos | Mostrar `errors[campo]` bajo cada campo del formulario |
| 401 | Sin sesión o token vencido | Refresh una vez; si falla, ir al login (en `/auth/login`: mostrar `detail`) |
| 403 | Sin permiso para esa acción | Mensaje "No tiene permisos". Si `codigo` = `DEBE_CAMBIAR_PASSWORD` → pantalla de cambio de contraseña |
| 404 | No existe | Mostrar `detail` o volver al listado |
| 409 | Conflicto (duplicado, dato en uso) | Mostrar `detail` |
| 422 | Regla de negocio | Mostrar `detail` |
| 423 | Cuenta bloqueada (`codigo` = `CUENTA_BLOQUEADA`) | Mostrar `detail` |
| 429 | Demasiadas peticiones | Esperar `Retry-After` segundos |
| 500 | Error interno | Mensaje genérico + `traceId` (sirve para buscar el error en el log del backend) |

Códigos estables (`codigo`) disponibles hoy: `DEBE_CAMBIAR_PASSWORD`, `CUENTA_BLOQUEADA`.

---

## 6. Modelos TypeScript

Interfaces listas para copiar (p. ej. en `src/app/core/models/`):

```ts
export type RolNombre = 'Administrador' | 'Gestor';

export interface UsuarioActual {
  usuarioId: number;
  nombre: string;
  correo: string;
  rol: RolNombre;
  debeCambiarPassword: boolean;
}

export interface TokenResponse {
  accessToken: string;
  accessTokenExpira: string;   // ISO UTC
  refreshToken: string;
  refreshTokenExpira: string;  // ISO UTC
  usuario: UsuarioActual;
}

export interface LoginRequest { correo: string; password: string; }
export interface RefreshRequest { refreshToken: string; }
export interface CambiarPasswordRequest { passwordActual: string; passwordNueva: string; }

export interface Usuario {
  usuarioId: number;
  nombre: string;
  correo: string;
  rolId: number;
  rol: RolNombre;
  activo: boolean;
  debeCambiarPassword: boolean;
  bloqueado: boolean;
  ultimoAcceso: string | null; // ISO UTC
  fechaCreacion: string;       // ISO UTC
}

export interface UsuarioFiltro {
  buscar?: string;
  rolId?: number;
  activo?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}

export interface CrearUsuarioRequest { nombre: string; correo: string; rolId: number; }
export type ActualizarUsuarioRequest = CrearUsuarioRequest;
export interface CambiarEstadoUsuarioRequest { activo: boolean; }

export interface PasswordTemporalResponse { usuario: Usuario; passwordTemporal: string; }
export interface Rol { rolId: number; nombre: RolNombre; descripcion: string | null; }

export interface PaginaResultado<T> { items: T[]; total: number; pagina: number; tamanoPagina: number; }

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  codigo?: 'DEBE_CAMBIAR_PASSWORD' | 'CUENTA_BLOQUEADA' | string;
  errors?: Record<string, string[]>;
}
```

---

## 7. Notas para cuando se arme el frontend

- **Desactivar el SSR/prerender** (`app.routes.server.ts` → `RenderMode.Client`): el login usa tokens del navegador y el despliegue en Render Static Site necesita un sitio estático.
- `app.config.ts`: `provideHttpClient(withFetch(), withInterceptors([authInterceptor]))`.
- **Interceptor:** agrega el Bearer salvo a `/auth/login`, `/auth/refresh` y `/auth/logout`; ante 401 comparte una única llamada de refresh en curso y repite la petición; ante 403 `DEBE_CAMBIAR_PASSWORD` redirige.
- **Guards:** `authGuard` (hay sesión) y `adminGuard` (`usuario.rol === 'Administrador'`) para las rutas de Administración.
- No guardar el `accessToken` en `localStorage` ni imprimir tokens en la consola.

## 8. Cómo probar sin frontend
- **Scalar** (`/scalar`): ejecutar `POST /api/auth/login`, copiar `accessToken` y pegarlo en *Authentication → Bearer*.
- **`backend/backend.http`** (VS Code con REST Client o Visual Studio): peticiones de ejemplo listas.
- Usuario de desarrollo: `admin@vmtfabric.com`; la contraseña está en `ADMIN_PASSWORD` del `backend/.env` del responsable (pedirla por el gestor de contraseñas). Para pruebas propias, que el Administrador cree un usuario con `POST /usuarios`.

## Historial
| Fecha | Cambio |
|---|---|
| 2026-11-23 | Versión inicial: autenticación y usuarios (fase 02). Fechas en UTC con `Z` y claves de `errors` en camelCase. |
