# VmtFabric · Documentación del backend

Registro de la construcción del backend (ASP.NET Core 10 + EF Core + SQL Server Express) por fases.

## Cómo está organizada esta carpeta

| Archivo | Contenido |
|---|---|
| [00-plan-de-fases.md](00-plan-de-fases.md) | Hoja de ruta: qué se construye en cada fase y qué requerimientos cubre. Se marca el estado de cada fase. |
| [01-decisiones-tecnicas.md](01-decisiones-tecnicas.md) | Decisiones de arquitectura (ADR breves): por qué se eligió cada cosa. |
| [02-brechas-bd-vs-requerimientos.md](02-brechas-bd-vs-requerimientos.md) | Diferencias entre `VmtFabric_Requerimientos.md` y `bd/VmtFabric_BD.sql`, y cómo se resuelven. |
| [03-guia-seguridad-y-codigo.md](03-guia-seguridad-y-codigo.md) | **Reglas obligatorias**: estructura de carpetas (Controllers/Models/Middleware...), principios de seguridad y de mantenimiento, checklist de cierre de fase. |
| [04-despliegue.md](04-despliegue.md) | Base de datos en MonsterASP.NET y API en Render: pasos, variables de entorno y limitaciones de los planes gratuitos. |
| [bitacora.md](bitacora.md) | Diario cronológico: qué se hizo, cuándo y qué quedó pendiente. |
| [fases/](fases/) | Un archivo por fase con el detalle: alcance, archivos creados, endpoints, cómo probar y pendientes. |

## Reglas para mantener el registro

1. Al iniciar una fase se crea `fases/fase-NN-nombre.md` a partir de la plantilla de la fase 01.
2. Al cerrar una fase: actualizar su estado en `00-plan-de-fases.md` y agregar una entrada en `bitacora.md`.
3. Toda decisión que cambie la arquitectura o se aparte del requerimiento va a `01-decisiones-tecnicas.md` o `02-brechas-...md`.
4. Antes de cerrar una fase se pasa la lista de verificación de `03-guia-seguridad-y-codigo.md`.

## Arranque rápido

```bash
# 1a. Base de datos compartida en MonsterASP: copiar backend/.env.example como backend/.env y completar DB_PASSWORD y JWT_KEY
# 1b. O base local (requiere el servicio SQL Server (SQLEXPRESS) iniciado):
sqlcmd -S .\SQLEXPRESS -E -Q "CREATE DATABASE VmtFabric"
sqlcmd -S .\SQLEXPRESS -E -d VmtFabric -b -i bd/VmtFabric_BD.sql

# 2. API
cd backend
dotnet run --launch-profile http
```

| URL | Para qué |
|---|---|
| http://localhost:5222/scalar | Documentación interactiva de la API (solo Development) |
| http://localhost:5222/openapi/v1.json | Documento OpenAPI |
| http://localhost:5222/health | Estado de la API y conexión a SQL Server |

Todos los endpoints exigen sesión salvo login/refresh/logout y `/health`. Para probar en Scalar: `POST /api/auth/login`, copiar `accessToken` y pegarlo en **Authentication → Bearer**. Contrato completo en [fases/fase-02-autenticacion.md](fases/fase-02-autenticacion.md).
