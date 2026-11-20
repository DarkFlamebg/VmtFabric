# Fase 01 · Fundamentos

**Estado:** ✅ Terminada — 2026-11-19 (reestructurada el mismo día a Controllers/Models/Middleware, ver DT-09)
**Objetivo:** dejar el proyecto listo para construir módulos: acceso a datos, errores, seguridad base, documentación y configuración.

## Alcance
- [x] Limpiar la plantilla (`/weatherforecast` eliminado).
- [x] `RootNamespace`/`AssemblyName` = `VmtFabric.Api`.
- [x] Paquetes: `Microsoft.EntityFrameworkCore.SqlServer 10.0.15`, `Scalar.AspNetCore 2.21.0`, `Microsoft.AspNetCore.OpenApi 10.0.15`.
- [x] Estructura de carpetas `Controllers / Models / Middleware / Services / Data / Exceptions / Configuration / Extensions`.
- [x] 26 entidades (una por archivo) y 26 configuraciones EF, alineadas con las 26 tablas del script.
- [x] `VmtFabricDbContext` con reintentos ante fallos transitorios.
- [x] `ExceptionHandlingMiddleware`: errores como ProblemDetails con `traceId`, sin filtrar detalles internos.
- [x] `SecurityHeadersMiddleware`: cabeceras OWASP; cabecera `Server` eliminada; HSTS + HTTPS fuera de desarrollo.
- [x] CORS con orígenes validados al arrancar (`Cors:AllowedOrigins`, por defecto `http://localhost:4200`).
- [x] Cadena de conexión fuera de `appsettings.json`; la API no arranca sin ella.
- [x] Límite de 10 MB por petición (Kestrel).
- [x] OpenAPI (`/openapi/v1.json`) + Scalar (`/scalar`) solo en Development.
- [x] Health check `/health` (no expone detalles de la conexión).
- [x] Analizadores de código + advertencias como errores.
- [x] `.gitignore` del backend (`bin/`, `obj/`).

## Archivos

| Ruta (en `backend/`) | Qué hace |
|---|---|
| `backend.csproj` | Paquetes, nombre del ensamblado, analizadores |
| `Program.cs` | Composición: 3 llamadas de registro + pipeline |
| `appsettings.json` | CORS, límites, logging (sin secretos) |
| `appsettings.Development.json` | Cadena de conexión local |
| `Controllers/` | Vacía (`.gitkeep`); primeros controllers en fase 02 |
| `Models/Entities/*.cs` | 26 entidades, una por tabla |
| `Models/Constants/*.cs` | `EstadoAsignacion`, `EstadoEvaluacion`, `EstadoIngreso`, `EstadoPlanCarrera`, `TipoAviso` |
| `Models/DTOs/` | Vacía (`.gitkeep`) |
| `Middleware/ExceptionHandlingMiddleware.cs` | Excepción → ProblemDetails |
| `Middleware/SecurityHeadersMiddleware.cs` | Cabeceras de seguridad y `no-store` en `/api` |
| `Services/` | Vacía (`.gitkeep`) |
| `Data/VmtFabricDbContext.cs` | DbContext |
| `Data/Configurations/*Configuration.cs` | Mapeo tabla por tabla (longitudes, tipos, índices, FKs) |
| `Data/DatabaseHealthCheck.cs` | Chequeo de SQL Server |
| `Exceptions/*.cs` | `AppException` (base), `NotFound`, `Conflict`, `BusinessRule`, `Forbidden` |
| `Configuration/CorsSettings.cs` | Opciones tipadas de CORS con validación |
| `Extensions/ServiceCollectionExtensions.cs` | `AddPersistence`, `AddApiServices`, `AddFrontendCors` |
| `Extensions/WebApplicationExtensions.cs` | `UseApiPipeline` (orden de middleware), `ValidateEfModel` |
| `backend.http` | Peticiones de prueba |

## Orden del pipeline
1. `ExceptionHandlingMiddleware` → 2. `SecurityHeadersMiddleware` → 3. `UseStatusCodePages` → (HSTS/HTTPS en prod) → 4. CORS → *(fase 02: Authentication, Authorization)* → 5. Controllers / `/health`.

## Detalles de mapeo a tener en cuenta
- `Asignacion.MargenPct` es columna calculada `PERSISTED`: solo lectura en la entidad.
- Índices únicos filtrados replicados en el modelo: una asignación `Activa` por colaborador (RN-01), un plan `Vigente` por colaborador, RUC único cuando no es nulo.
- Todas las FK usan `NoAction` (igual que el script), salvo `PlanObjetivo → PlanCarrera` y `EvaluacionCriterio → Evaluacion` que son `CASCADE`.
- `Cedula` y `Ruc` son `CHAR` (longitud fija, no Unicode).

## Verificación realizada
| Prueba | Resultado |
|---|---|
| `dotnet build` (analizadores, warnings = errores) | ✅ 0 advertencias, 0 errores |
| Arranque en Development | ✅ log: "Modelo EF validado: 26 entidades mapeadas" |
| `GET /openapi/v1.json`, `GET /scalar` | ✅ 200 |
| `GET /api/no-existe` | ✅ 404 `application/problem+json` con `instance` y `traceId` |
| Cabeceras en todas las respuestas | ✅ nosniff, `X-Frame-Options: DENY`, CSP, Referrer/Permissions-Policy, COOP; `no-store` en `/api`; sin `Server` |
| `GET /health` | ⚠️ 503 — esperado: el servicio `SQL Server (SQLEXPRESS)` estaba detenido y no se pudo iniciar sin permisos de administrador |

## Pendientes que pasan a la siguiente fase
- **Probar contra la BD real**: iniciar el servicio `MSSQL$SQLEXPRESS` (como administrador) y ejecutar el script; `/health` debe responder 200 `Healthy`.
- `backend/bin` y `backend/obj` estaban versionados en git en el primer commit; ejecutar una vez: `git rm -r --cached backend/bin backend/obj`.
- Confirmar con el cliente las brechas ❓ de [02-brechas-bd-vs-requerimientos.md](../02-brechas-bd-vs-requerimientos.md) (sobre todo roles y estados, que afectan a las fases 02 y 04).
