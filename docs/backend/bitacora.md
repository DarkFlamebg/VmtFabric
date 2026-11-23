# Bitácora del backend

Entradas en orden cronológico (la más reciente al final).

---

## 2026-11-19 · Fase 01 — Fundamentos ✅

**Hecho**
- Se analizó `VmtFabric_Requerimientos.md` contra `bd/VmtFabric_BD.sql`; se registraron 12 brechas en `02-brechas-bd-vs-requerimientos.md`. Criterio: la BD es la fuente de verdad.
- Se definió el plan de 13 fases (`00-plan-de-fases.md`) y las decisiones técnicas DT-01 a DT-08.
- Se reemplazó la plantilla de .NET 10 por la base del proyecto: EF Core mapeado a las 26 tablas, ProblemDetails, CORS, OpenAPI + Scalar, health check.
- Compila sin advertencias; la API arranca y valida el modelo EF.

**Problemas encontrados**
- El servicio `SQL Server (SQLEXPRESS)` estaba detenido y no se pudo iniciar sin permisos de administrador → `/health` devuelve 503. Falta la prueba contra la BD real.
- `bin/` y `obj/` del backend estaban en el commit inicial.

**Siguiente:** Fase 02 — autenticación JWT, usuarios y auditoría.

---

## 2026-11-19 · Fase 01 — Reestructuración y seguridad base ✅

**Motivo:** el equipo pidió usar las carpetas `Models`, `Controllers` y `Middleware` y reforzar los principios de seguridad y mantenimiento.

**Hecho**
- Estructura migrada a `Controllers / Models (Entities, DTOs, Constants) / Middleware / Services / Data / Exceptions / Configuration / Extensions` (DT-09). Una clase por archivo (26 entidades, 26 configuraciones).
- Manejo de errores pasado a `ExceptionHandlingMiddleware`, con `traceId` e `instance` en cada error.
- Nuevo `SecurityHeadersMiddleware`; cabecera `Server` eliminada; HSTS/HTTPS en producción.
- Cadena de conexión movida a `appsettings.Development.json`; la API no arranca sin ella (DT-11). Límite de 10 MB por petición. `AllowedHosts` restringido.
- Analizadores `latest-recommended` + advertencias como errores (DT-10). Se corrigieron 34 hallazgos (nombres de parámetros CA1725 y logs con `[LoggerMessage]` CA1848/CA1873).
- Nueva guía `03-guia-seguridad-y-codigo.md` con los principios S-01..S-14 y M-01..M-11.
- Verificado: build limpio, modelo EF válido, 404 como ProblemDetails, cabeceras presentes.

**Pendiente:** igual que la entrada anterior (BD real, `bin/obj` en git, brechas por confirmar).

---

## 2026-11-19 · Hosting en la nube: MonsterASP.NET (BD) + Render (API)

**Contexto:** se evaluaron Supabase y Render Postgres (PostgreSQL, incompatibles con el script T-SQL), Azure SQL (requiere tarjeta) y MonsterASP.NET (SQL Server 2025 gratis). Decisión DT-12.

**Hecho**
- `bd/VmtFabric_BD.sql`: eliminados `CREATE DATABASE` y `USE` (el nombre de la base lo asigna el proveedor); nueva guarda que impide ejecutarlo en bases del sistema; instrucciones de ejecución para nube y local.
- Backend: soporte opcional de proxy inverso (`ReverseProxy:Enabled`), redirección HTTPS al 443 excepto `/health`, HSTS de 1 año; `appsettings.Development.json` excluido de la publicación.
- `backend/Dockerfile` (multi-etapa, usuario no root, puerto 10000), `backend/.dockerignore` y `render.yaml` (Frankfurt, plan free, secretos con `sync: false`).
- Nueva guía `04-despliegue.md`.
- Verificado con `dotnet publish` + ejecución en modo Production simulando Render: proxy HTTPS ok, HTTP directo → 307, `/health` sin redirección, Host ajeno → 400, `/scalar` → 404, sin cadena no arranca, HSTS 1 año.

**No verificado**
- Construcción de la imagen Docker: Docker Desktop no estaba en ejecución.
- Conexión real a MonsterASP y despliegue en Render: faltan las cuentas.

**Riesgos anotados:** MonsterASP Free sin backups automáticos y sin filtro de IP; Render Free se duerme y tiene disco efímero (afecta a las fases 07 y 10).

---

## 2026-11-19 · Revisión del hosting: todo en MonsterASP, en etapas

**Decisión:** la API también irá a MonsterASP (misma red que la BD, se duerme tras 3 h, disco persistente, .NET nativo); el frontend irá a Render Static Site; la API en Render queda como plan B. Horizonte: hasta enero de 2027.

**Etapas:** 1) ahora solo la BD, compartida por el equipo vía acceso remoto + user-secrets; 2) la API cuando esté terminada; 3) el frontend.

**Base creada (etapa 1):** `db81318` en MonsterASP Free (SQL Server 2025, EU, collation `SQL_Latin1_General_CP1_CI_AS`), acceso remoto habilitado; test de acceso remoto del panel: OK. Configuración local por `backend/.env` (lector solo en Development, `.env.example` versionado, `.env` en `.gitignore`/`.dockerignore`). Pendiente: ejecutar el script y conectar desde los PCs (ver incidencia abajo).

**Incidencia de red (2026-11-19):** el puerto 1433 respondía al inicio; minutos después la IP del servidor (46.4.165.210) dejó de responder desde el PC (también el puerto 80), mientras el test del panel daba OK y no había IP bloqueadas en *Automated Security Protection* ni intentos de login registrados. Causa no determinada (red local/ISP o firewall del proveedor).

**Importación del script en MonsterASP (2026-11-19):**
1. *Import SQL* rechazó el archivo por contener el texto `CREATE DATABASE` (solo estaba en un comentario): el importador filtra por texto. Se reescribieron los comentarios de la cabecera.
2. La segunda importación creó las 26 tablas y las 8 vistas y se detuvo en `sp_CompletarEvaluacion`: **error real del script** (8124, referencias externas `e.EscalaMin/EscalaMax` mezcladas con columnas internas dentro de `SUM`). Fórmula reescrita de forma equivalente: `(Σ(p·w) − min·Σw) / ((max − min)·Σw) · 100`, con `NULLIF` para evitar división por cero.
3. Estado verificado con un SELECT en *Run T-SQL*: 26 tablas, 8 vistas, 0 procedimientos, 0 datos iniciales.
4. Creado `bd/cambios/001_completar_instalacion.sql` (procedimientos + datos iniciales, con guarda para no ejecutarse dos veces). El script principal también quedó corregido para instalaciones nuevas.
- No se pudo probar el script en local: SQL Server LocalDB 2025 no arranca en el equipo.
5. `001_completar_instalacion.sql` importado. **Verificado:** 26 tablas, 8 vistas, procedimientos `sp_CompletarEvaluacion` y `sp_GenerarAvisos`, 2 roles, 9 cargos, 19 tecnologías, 5 criterios plantilla, 1 usuario (admin con hash pendiente de la Fase 02). La base está lista.
6. Conexión API local → BD: **bloqueada por red**. Desde el PC del desarrollador, 46.4.165.210 no responde en 1433 ni en 80 (sí respondió al inicio de la sesión); el test remoto del panel da OK y el IDS no muestra bloqueos.

**Hecho:** `04-despliegue.md` reescrito por etapas, con reglas para la base compartida (scripts incrementales en `bd/cambios/`, copias semanales, manejo de la cadena de conexión). DT-12 revisada. Plan de fases ajustado (Fase 02 sin correo, Fase 07 en disco, Fase 10 bajo demanda).
7. Nueva prueba API local → BD: `/health` → **503** tras 56 s (`SqlException` error 40/64, servidor no accesible). Diagnóstico de red: el puerto 1433 de salida **funciona** (portquiz.net:1433 OK) y el sitio de MonsterASP responde (443 OK), pero el servidor de bases 46.4.165.210 no responde en ningún puerto (tampoco ping) solo desde la IP pública del desarrollador. Conclusión: **bloqueo de la IP en el lado del proveedor**, no de la red local. Acciones: ticket a soporte de MonsterASP y/o prueba con datos móviles; alternativa evaluada: Azure SQL Database (oferta gratuita).
8. Abierto ticket de soporte en MonsterASP (código `D74Y-Y87W-X84C`, prioridad media, vinculado a db81318) pidiendo revisar el firewall y desbloquear la IP del desarrollador. Estado: esperando respuesta (llega por correo y en *Tickets* del panel).

## 2026-11-22 · Respuesta del soporte y causa real del bloqueo

- Soporte (ticket `D74Y-Y87W-X84C`): la IP del desarrollador fue **bloqueada 24 h por varios intentos con contraseña incorrecta** y la desbloquearon.
- Prueba: puerto 1433 abierto → un intento de la API (`/health`) → 503 y la IP **quedó bloqueada otra vez** de inmediato.
- *Access log* del panel: solo hay inicios de sesión **locales** (el propio panel); ningún inicio de sesión remoto correcto desde el PC.
- **Conclusión:** la contraseña de `backend/.env` no es la del usuario `db81318`. El bloqueo no es de red. Solución: restablecer la contraseña del usuario de la base en el panel, copiarla exacta al `.env` y pedir de nuevo el desbloqueo.
- Contraseña del usuario `db81318` restablecida por el desarrollador y `.env` corregido (faltaba `DB_USER`). Bloqueo del IDS quitado desde el panel (*Users → Automated Security Protection → Remove*).
- **Verificado:** `/health` → **200 Healthy** (2 s). Consulta con `sqlcmd` desde el PC: base `db81318`, 26 tablas, 8 vistas, 2 procedimientos, 2 roles, 9 cargos, 19 tecnologías, 1 usuario. **La API local ya está conectada a la base en la nube.**
- Lección: un `.env` con la contraseña incorrecta bloquea la IP en pocos arranques de la API. Antes de arrancar, confirmar la contraseña; si falla una vez, no reintentar.

---

## 2026-11-22 · Fase 02 — Autenticación, usuarios y auditoría ✅

**Hecho**
- Login JWT (15 min) + refresh token rotativo con hash SHA-256 y detección de reutilización; logout; cambio de contraseña; `GET /api/auth/me`.
- Bloqueo tras 5 intentos (15 min), rate limiting de 10/min por IP en `/api/auth`, BCrypt factor 12, mensajes que no revelan si el correo existe.
- Contraseñas temporales generadas por el servidor (sin correo); `PasswordPendienteMiddleware` obliga a cambiarlas.
- CRUD de usuarios solo para Administrador, con reglas (siempre un Admin activo, no auto-desactivarse, etc.).
- Política de respaldo: todo exige sesión salvo lo marcado `[AllowAnonymous]`. Políticas `Administrador` y `Talento`.
- Auditoría automática con interceptor de EF + eventos de seguridad con IP.
- FluentValidation con filtro global; errores del framework (401/403/404/405) en español.
- Nuevas carpetas `Security/`, `Validators/`, `Filters/`, `Data/Interceptors/` (DT-14). Decisiones DT-13 a DT-15.
- `bd/cambios/002_indice_refreshtoken.sql` y el script principal con los índices de `RefreshToken`.
- `.env` admite `JWT_KEY` (obligatoria) y `ADMIN_PASSWORD`; `render.yaml` con `Jwt__Key`.

**Verificado contra la base en MonsterASP:** 39/39 pruebas de punta a punta, 429 al 11.º login por minuto, filas de `Auditoria` correctas, hashes BCrypt. Detalle en `fases/fase-02-autenticacion.md`.

**Datos de prueba en la base compartida:** admin con contraseña definitiva (en el `.env` de quien ejecutó las pruebas) y `gestor.prueba@vmtfabric.test` inactivo.

**Pendiente:** ejecutar `002_indice_refreshtoken.sql` en la base compartida; que cada integrante agregue `JWT_KEY` a su `.env`.

**Siguiente:** Fase 03 — Catálogos.

---

## 2026-11-23 · Prueba de login como frontend y guía de la API

- Prueba simulando el navegador (`Origin: http://localhost:4200`): preflight CORS 204 con el origen permitido; login 401/200, `me`, usuarios, roles, refresh, logout y validación 400 correctos.
- **Corregido:** las fechas leídas de la BD salían sin zona (`Kind=Unspecified`, JSON sin `Z`) y el navegador las habría tomado como hora local → convención EF `UtcDateTimeConverter` para todo `DateTime`. Las claves de `errors` en los 400 salían en PascalCase (`RolId`) → ahora camelCase (`rolId`), igual que el JSON.
- Nuevo `05-api-para-frontend.md`: URL y CORS, convenciones, flujo de sesión, todos los endpoints con ejemplos reales, errores y modelos TypeScript.
