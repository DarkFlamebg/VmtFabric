# Guía de estructura, seguridad y mantenimiento

Reglas que se aplican en **todas** las fases. Antes de cerrar una fase se revisa la lista de verificación del final.

## 1. Estructura de carpetas (obligatoria)

```
backend/
├── Controllers/          Endpoints HTTP. Delgados: validan entrada, llaman a un servicio, devuelven DTO.
├── Models/
│   ├── Entities/         Una clase por tabla (mapeo 1:1 con bd/VmtFabric_BD.sql). Nunca se devuelven al cliente.
│   ├── DTOs/             Contratos de entrada/salida de la API, agrupados por módulo (DTOs/Colaboradores/...).
│   └── Constants/        Valores fijos de la BD (estados, tipos) como constantes.
├── Middleware/           Componentes del pipeline HTTP (errores, cabeceras de seguridad, ...).
├── Services/             Lógica de negocio y reglas RN-xx. Interfaz + implementación por módulo.
├── Data/                 DbContext, Configurations/ (una por entidad) y health check.
├── Exceptions/           Excepciones de aplicación (se traducen a HTTP en el middleware).
├── Configuration/        Clases de opciones tipadas (secciones de appsettings), validadas al arrancar.
├── Extensions/           Registro de servicios y pipeline, para que Program.cs quede corto.
└── Program.cs
```

Flujo de una petición: `Middleware → Controller → Service → Data (DbContext)`.
Un controller **no** usa el DbContext directamente y un servicio **no** conoce `HttpContext`.

## 2. Principios de seguridad

| # | Principio | Cómo se aplica |
|---|---|---|
| S-01 | Denegar por defecto | Desde la fase 02 todo endpoint exige autenticación (política de respaldo); lo público se marca con `[AllowAnonymous]` explícito. |
| S-02 | Mínimo privilegio | `[Authorize(Roles = ...)]` por endpoint; el Gestor no accede a Administración. Verificar también la propiedad del recurso cuando aplique. |
| S-03 | Nunca exponer entidades | Siempre DTOs de entrada/salida: evita over-posting (asignar campos como `RolId` o `Eliminado`) y fuga de datos (`PasswordHash`). |
| S-04 | Validar toda entrada | DataAnnotations/FluentValidation en DTOs; longitudes iguales a las de la BD; cédula con algoritmo ecuatoriano. |
| S-05 | Consultas parametrizadas | Solo LINQ o `FromSql($"...")` interpolado (parametriza). Prohibido `FromSqlRaw` con concatenación. |
| S-06 | Errores sin detalles internos | El middleware responde ProblemDetails; un 500 nunca incluye mensaje, SQL ni stack trace. Se devuelve `traceId` para buscar en el log. |
| S-07 | Secretos fuera del repositorio | `appsettings.json` no lleva cadenas ni claves. Desarrollo: `appsettings.Development.json` / `dotnet user-secrets`. Producción: variables de entorno (`ConnectionStrings__VmtFabric`, `Jwt__Key`). La API no arranca si faltan. |
| S-08 | Contraseñas y tokens con hash | BCrypt para contraseñas; refresh/recuperación se guardan como hash SHA-256, nunca en claro. |
| S-09 | Cabeceras HTTP seguras | `SecurityHeadersMiddleware`: nosniff, `X-Frame-Options: DENY`, CSP restrictiva, `Referrer-Policy`, `Permissions-Policy`, `Cache-Control: no-store` en `/api`. Sin cabecera `Server`. HSTS + redirección HTTPS fuera de desarrollo. |
| S-10 | CORS cerrado | Solo los orígenes de `Cors:AllowedOrigins` (validado al arrancar); nunca `*`. |
| S-11 | Límites | Cuerpo máximo 10 MB (Kestrel); paginación con tamaño máximo; rate limiting en login (fase 02); tipos y tamaño de archivo controlados (fase 07). |
| S-12 | Datos personales en logs | `EnableSensitiveDataLogging` solo en Development. No registrar contraseñas, tokens ni documentos. |
| S-13 | Auditoría | Crear/editar/eliminar/login/exportar quedan en `Auditoria` con usuario y fecha (RNF-07). |
| S-14 | Documentación privada | OpenAPI/Scalar solo en Development. |

## 3. Principios de mantenimiento

| # | Principio | Cómo se aplica |
|---|---|---|
| M-01 | Una clase por archivo | El nombre del archivo es el de la clase. |
| M-02 | Responsabilidad única | Controller = HTTP, Service = negocio, Data = persistencia, Middleware = asuntos transversales. |
| M-03 | Depender de interfaces | Servicios registrados como `IXxxService` → `XxxService` (inyección de dependencias); facilita pruebas. |
| M-04 | Sin números mágicos | Ids y estados vía `Models/Constants` o constantes de la entidad (`Rol.Administrador`, `EstadoAsignacion.Activa`). |
| M-05 | Cero advertencias | `TreatWarningsAsErrors` + analizadores `latest-recommended`: el proyecto no compila con advertencias. No se silencian reglas sin justificarlo en `01-decisiones-tecnicas.md`. |
| M-06 | Logs de alto rendimiento | `[LoggerMessage]` (source generator) en lugar de `logger.LogX(...)` directo (CA1848). |
| M-07 | Async de punta a punta | Métodos de I/O `async` con `CancellationToken` propagado hasta EF. |
| M-08 | Consultas eficientes | `AsNoTracking()` en lecturas, proyección a DTO con `Select`, paginación en SQL, nada de cargar tablas completas en memoria. |
| M-09 | Configuración tipada | `IOptions<T>` con validación al arrancar (`ValidateOnStart`) en vez de leer `IConfiguration["..."]` disperso. |
| M-10 | Nombres en español del dominio | Entidades, DTOs y endpoints usan el vocabulario del negocio (Colaborador, Asignación); código técnico sigue las convenciones de .NET. |
| M-11 | Registro de cambios | Toda fase actualiza `docs/backend` (plan, bitácora, archivo de fase). |

## 4. Lista de verificación al cerrar una fase

- [ ] `dotnet build` sin errores ni advertencias.
- [ ] Endpoints nuevos con `[Authorize]`/roles correctos y probados sin token (401) y con rol incorrecto (403).
- [ ] Entrada validada; respuestas solo con DTOs.
- [ ] Reglas de negocio en servicios, lanzando `BusinessRuleException` cuando no se cumplen.
- [ ] Sin secretos nuevos en archivos versionados.
- [ ] Peticiones de ejemplo agregadas a `backend/backend.http`.
- [ ] Documentación de la fase actualizada.
