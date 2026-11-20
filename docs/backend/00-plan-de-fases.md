# Plan de fases del backend

Leyenda de estado: ✅ terminada · 🔄 en curso · ⏳ pendiente

| # | Fase | Estado | Requerimientos |
|---|---|---|---|
| 01 | Fundamentos | ✅ | RNF-05 (base), RNF-08 |
| 02 | Autenticación, usuarios y auditoría | ⏳ | RF-01, RF-02, RF-51, RF-52, RNF-01, RNF-02, RNF-07 |
| 03 | Catálogos | ⏳ | RF-53, RF-54, RF-55 |
| 04 | Colaboradores | ⏳ | RF-12 a RF-18, RF-23, RF-25, RN-04, RN-05, RN-09, RN-10 |
| 05 | Clientes y proyectos | ⏳ | RF-27, RF-28, RF-29, RF-31, RF-32 |
| 06 | Asignaciones e ingresos | ⏳ | RF-20, RF-21, RF-26, RF-30, RN-01, RN-02, RN-03, RN-07 |
| 07 | Documentos (archivos) | ⏳ | RF-19, RF-33, RNF-04 |
| 08 | Plan de carrera | ⏳ | RF-22, RF-34, RF-35, RF-36 |
| 09 | Evaluaciones | ⏳ | RF-37, RF-38, RF-39 |
| 10 | Avisos y tareas programadas | ⏳ | RF-04, RN-08 |
| 11 | Dashboard y análisis | ⏳ | RF-06 a RF-11, RF-40 a RF-44 |
| 12 | Reportes y exportación | ⏳ | RF-24, RF-45 a RF-50 |
| 13 | Endurecimiento y despliegue | ⏳ (infraestructura de despliegue ya preparada, ver [04-despliegue.md](04-despliegue.md)) | Tests, rate limiting, logs, configuración de producción |

> **Impacto del hosting en MonsterASP Free (DT-12):**
> - Fase 02: sin envío de correos → "olvidé mi contraseña" lo resuelve el Administrador con una contraseña temporal (`DebeCambiarPassword`). La tabla `RecuperacionPassword` queda reservada para cuando haya correo.
> - Fase 07: los documentos se guardan en el disco del sitio (5 GB), detrás de `IFileStorage`.
> - Fase 10: los avisos se generan bajo demanda; no hay tareas programadas.

## Detalle por fase

### 01 · Fundamentos ✅
Estructura `Controllers / Models / Middleware / Services / Data`, EF Core mapeado a las 26 tablas del script (sin migraciones), middleware de errores (ProblemDetails) y de cabeceras de seguridad, CORS para Angular, secretos fuera del repositorio, analizadores de código, OpenAPI + Scalar, health check de SQL Server. → [fases/fase-01-fundamentos.md](fases/fase-01-fundamentos.md)

### 02 · Autenticación, usuarios y auditoría
- `POST /api/auth/login`, `/refresh`, `/logout`, `/cambiar-password`; el Administrador restablece contraseñas de otros usuarios (sin correo, ver DT-12).
- JWT de acceso corto + refresh token rotativo guardado como hash (`RefreshToken`).
- Bloqueo por intentos fallidos (`IntentosFallidos`, `BloqueadoHasta`), `DebeCambiarPassword`.
- Hash de contraseñas con BCrypt; reemplazo del `<REEMPLAZAR_CON_HASH>` del admin inicial al arrancar.
- `GET /api/auth/me` (nombre, correo, rol para el header).
- CRUD de usuarios (solo Administrador), activar/desactivar, asignar rol.
- Políticas de autorización por rol; usuario actual (`ICurrentUser`) para llenar `CreadoPor`/`ModificadoPor`.
- Interceptor de auditoría que escribe en `Auditoria`.
- Validación con FluentValidation.

### 03 · Catálogos
Tecnologías (CRUD solo Admin, ranking y tarjeta con colaboradores y nivel promedio), cargos, seniority, estados, tipos de documento, modalidades de tarifa, plantillas de criterios. Endpoint combinado para llenar combos del frontend.

### 04 · Colaboradores
Listado paginado/ordenado del lado del servidor con búsqueda (nombre, cédula, correo) y filtro por estado con contadores; crear, editar, borrado lógico; validación de cédula ecuatoriana; ficha (cabecera, contacto, edad calculada); tecnologías del colaborador (nivel 1–5).

### 05 · Clientes y proyectos
Listado con resumen (`vw_ClienteResumen`), CRUD de clientes y proyectos, pestañas General, Activos e Historial.

### 06 · Asignaciones e ingresos
Nueva asignación con cierre automático de la activa (RN-01/02), advertencia y confirmación explícita por traslape (RN-03), actualización de la asignación actual, historial con filtros, tecnologías usadas, cálculo financiero (`vw_AsignacionResumen`/`vw_AsignacionEstimado`), registro de ingresos mensuales y pestaña Financiero del cliente.

### 07 · Documentos
Subida/descarga/eliminación lógica de archivos de colaboradores y clientes, almacenamiento en disco detrás de una interfaz (`IFileStorage`) para poder migrar a blob, límite de tamaño y tipos permitidos (PDF, imágenes).

### 08 · Plan de carrera
Plan vigente por colaborador, objetivos con peso, % de avance (`vw_PlanCarreraProgreso`), promedio de la fábrica. Integración con VmtSkills queda como punto de extensión (ver brechas).

### 09 · Evaluaciones
Periodos, crear evaluación con criterios y evaluador, responder criterios, completar (`sp_CompletarEvaluacion`), listado y detalle.

### 10 · Avisos
`BackgroundService` diario que ejecuta `sp_GenerarAvisos`; endpoints de campana (contador, listado, marcar leído).

### 11 · Dashboard y análisis
KPIs (`vw_DashboardKpi`), colaboradores por cliente, por estado, por nivel, top 10 tecnologías, contratos por vencer, evaluación promedio, cobertura de tecnologías.

### 12 · Reportes
Exportación a Excel/CSV de los 6 reportes y ficha del colaborador en PDF.

### 13 · Endurecimiento
Pruebas de integración, rate limiting en login, logging estructurado, secretos fuera de `appsettings`, configuración de producción.
