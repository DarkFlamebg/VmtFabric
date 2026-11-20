# Decisiones técnicas

Formato: contexto → decisión → consecuencias. Las decisiones se numeran y no se borran; si una cambia, se marca como reemplazada.

---

## DT-01 · La base de datos manda (database-first sin migraciones)
**Contexto:** el esquema ya existe en `bd/VmtFabric_BD.sql`, con índices filtrados, columnas calculadas, vistas y procedimientos.
**Decisión:** EF Core mapea las tablas existentes con `IEntityTypeConfiguration<>` escritas a mano. No se usan migraciones de EF.
**Consecuencias:** cualquier cambio de esquema se hace primero en el script SQL y luego en la entidad + configuración. Las vistas y procedimientos se consumen desde EF (`FromSql`, entidades sin clave) cuando cada fase los necesite.

## DT-02 · ~~Un solo proyecto con carpetas Domain/Infrastructure/Features~~ — reemplazada por DT-09

## DT-03 · Controllers con atributos de autorización
**Decisión:** API con `[ApiController]` en lugar de minimal APIs, porque el requerimiento pide `[Authorize(Roles=…)]` (RNF-02) y facilita agrupar endpoints por módulo.

## DT-04 · Errores como ProblemDetails desde un middleware
**Decisión:** `Middleware/ExceptionHandlingMiddleware` traduce:
| Excepción | HTTP |
|---|---|
| `NotFoundException` | 404 |
| `ConflictException`, violación UNIQUE (SQL 2601/2627), FK/CHECK (SQL 547), concurrencia | 409 |
| `BusinessRuleException` (reglas RN-xx) | 422 |
| `ForbiddenException` | 403 |
| `BadHttpRequestException` | su código (400/413...) |
| cualquier otra | 500 (se registra en log, no se expone el detalle) |

Toda respuesta de error lleva `traceId` e `instance`. Los servicios lanzan estas excepciones; los controllers no tienen try/catch. Los 404/405 sin cuerpo también salen como ProblemDetails (`UseStatusCodePages`).

## DT-05 · Documentación con OpenAPI nativo + Scalar
**Decisión:** `Microsoft.AspNetCore.OpenApi` (nativo de .NET 10) genera `/openapi/v1.json`; Scalar da la UI en `/scalar`. Solo en Development. Cumple RNF-08 (el requerimiento dice "Swagger/OpenAPI"; Scalar es la UI recomendada en .NET 9+, Swashbuckle ya no viene en las plantillas).

## DT-06 · Fechas
- Columnas `DATE` → `DateOnly`; `DATETIME2` → `DateTime` en **UTC** (la BD usa `SYSUTCDATETIME()`).
- Los campos de fecha de creación tienen `HasDefaultValueSql("SYSUTCDATETIME()")` para que SQL Server los asigne.

## DT-07 · Estados como constantes, no enums
Los estados que en la BD son `VARCHAR` con CHECK (`Asignacion.Estado`, `Evaluacion.Estado`, etc.) se modelan como `string` + clases de constantes en `Models/Constants` (`EstadoAsignacion.Activa`), para que coincidan exactamente con los valores del CHECK. Los catálogos con id fijo (`Rol`, `EstadoColaborador`, `TipoDocumento`, `ModalidadTarifa`) exponen sus ids como constantes en la propia entidad.

## DT-08 · Validación del modelo al arrancar (Development)
Al iniciar en Development se construye el modelo EF y se registra cuántas entidades se mapearon; un error de configuración detiene el arranque de inmediato en lugar de fallar en la primera petición.

## DT-09 · Estructura por capas: Controllers / Models / Middleware (+ Services, Data)
**Contexto:** pedido explícito del equipo (2026-11-19): el backend debe usar las carpetas `Models`, `Controllers` y `Middleware`.
**Decisión:** un único proyecto con la estructura descrita en [03-guia-seguridad-y-codigo.md](03-guia-seguridad-y-codigo.md#1-estructura-de-carpetas-obligatoria). Se agregan `Services` (lógica de negocio), `Data` (EF Core), `Exceptions`, `Configuration` y `Extensions` para no mezclar responsabilidades. Una clase por archivo.
**Consecuencias:** la convención es la estándar de ASP.NET, fácil de reconocer por cualquier desarrollador .NET.

## DT-10 · Calidad de código forzada por el compilador
**Decisión:** `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true` y `TreatWarningsAsErrors=true` en el `.csproj`.
**Consecuencias:** cualquier advertencia rompe la compilación. Las reglas se corrigen, no se silencian; si alguna se desactiva, se justifica aquí.

## DT-11 · Secretos y configuración por entorno
**Decisión:** `appsettings.json` (versionado) no contiene cadenas de conexión ni claves: `ConnectionStrings:VmtFabric` está vacía y la API se niega a arrancar si sigue vacía. Desarrollo usa `appsettings.Development.json` (conexión local con autenticación de Windows, sin contraseña) o, para apuntar a la base en la nube, `dotnet user-secrets`. Producción usa variables de entorno. `AllowedHosts` restringido a `localhost` por defecto; en Render se sobrescribe con `*.onrender.com`. `appsettings.Development.json` no se incluye en el paquete publicado ni en la imagen Docker.

## DT-12 · Hosting gratuito: BD y API en MonsterASP.NET, frontend en Render
**Contexto:** se necesita la base en la nube sin costo, compartida por el equipo y sin reescribir el script T-SQL, funcionando al menos hasta enero de 2027. Se descartaron Supabase y Render Postgres (PostgreSQL: obligaban a convertir el script y el backend; además Render Postgres Free caduca a los 30 días) y Azure SQL (requiere tarjeta).
**Decisión (revisada el 2026-11-19):**
- **Base de datos:** MonsterASP.NET plan Free — SQL Server 2025, 1 GB, centro de datos EU, acceso remoto habilitado para el equipo. **Se sube primero (etapa 1)**; el equipo desarrolla con la API en local.
- **API:** también en MonsterASP (etapa 2), cuando el backend esté terminado: misma red que la BD (conexión interna), .NET nativo sin Docker, se duerme tras 3 h (Render: 15 min) y disco persistente de 5 GB.
- **Frontend:** Render Static Site (etapa 3); MonsterASP Free solo permite un sitio web.
- **Plan B:** API en Render con Docker; `Dockerfile` y `render.yaml` se conservan.
- El script SQL deja de hacer `CREATE DATABASE`/`USE` y se ejecuta conectado a la base destino (el nombre lo asigna el proveedor). Incluye una guarda para no ejecutarse en `master`.
- La API soporta proxy inverso de forma opcional (`ReverseProxy:Enabled`, solo para el plan B): cabeceras reenviadas, redirección HTTPS al 443 (excepto `/health`) y HSTS de 1 año.

**Consecuencias:** sin costo y sin cambios en EF Core. Efectos en las fases: la **02** restablece contraseñas por el Administrador (MonsterASP Free no envía correos); la **07** puede guardar documentos en el disco del sitio; la **10** genera avisos bajo demanda (no hay tareas programadas). Los cambios de esquema posteriores van en scripts incrementales (`bd/cambios/NNN_*.sql`) porque la base es compartida. Detalle en [04-despliegue.md](04-despliegue.md). Antes de una puesta en producción real hay que revisar esta decisión.
