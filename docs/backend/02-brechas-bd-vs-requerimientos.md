# Brechas entre requerimientos y base de datos

`VmtFabric_Requerimientos.md` (documento original, roles "a validar") y `bd/VmtFabric_BD.sql` (posterior, basado en el diseño de Figma y en lo pedido por el cliente) no coinciden en varios puntos.
**Criterio adoptado: la BD es la fuente de verdad**, porque es más reciente y su cabecera recoge decisiones del cliente. Cada brecha queda abierta hasta que se confirme.

| # | Tema | Requerimiento | Base de datos | Cómo lo resuelve el backend | Estado |
|---|---|---|---|---|---|
| B-01 | Roles | Admin, Gestor, Consulta (§1.2) | Administrador (todo) y Gestor (solo Talento) | 2 roles. "Consulta" no existe. | ❓ Confirmar |
| B-02 | Estados del colaborador | Activo, Asignado, En banca, Desvinculado (RN-05, RF-13) | Asignado, Disponible, Inactivo | Se usan los de la BD: *Disponible* ≈ "En banca", *Inactivo* ≈ "Desvinculado". No hay "Activo" separado. | ❓ Confirmar |
| B-03 | Evaluaciones | Prueba de opción múltiple: temas, preguntas, respuestas correctas (RF-38, RF-39) | Evaluación por criterios con puntaje 1–5, peso y evaluador; puntaje final 0–100 | Se implementa el modelo de criterios. RF-39 (preguntas) queda fuera. | ❓ Confirmar |
| B-04 | Facturación | Tabla `Facturacion` por cliente y periodo (RF-30) | Tabla `Ingreso` por asignación y periodo, con estado Proyectado/Facturado/Cobrado | La pestaña Financiero agrega los ingresos de las asignaciones del cliente (`vw_IngresosClienteMes`). | ✅ Resuelto |
| B-05 | Plan de carrera | Sincronizado desde VmtSkills, solo lectura (RF-36) | Gestionado localmente: `PlanCarrera` + `PlanObjetivo` con pesos | Se gestiona localmente; la sincronización con VmtSkills queda como punto de extensión. | ❓ Confirmar |
| B-06 | Calificación de la asignación | `Asignacion.Calificacion` 0–100 (RF-20, RF-26) | No existe en `Asignacion` | Se mostrará el `PuntajeFinal` de la última evaluación completada del colaborador. | ❓ Confirmar |
| B-07 | Próximo objetivo | `Colaborador.ProximoObjetivo` (RF-25) | No existe | Se toma el siguiente objetivo no completado del plan vigente. | ✅ Resuelto |
| B-08 | Tarifa mensual del cliente | `Cliente.TarifaMensual` (RF-28, RF-29) | Tarifa por asignación (`ValorCobrar`, `ModalidadTarifa`) | El cliente no tiene tarifa propia; se muestra la suma/promedio de sus asignaciones activas. | ❓ Confirmar |
| B-09 | Nombre del colaborador | Un campo `Nombre` | `Nombres` + `Apellidos` | La API expone ambos y `nombreCompleto`. | ✅ Resuelto |
| B-10 | Cargo y nivel | Solo en la asignación | También en `Colaborador` (cargo/seniority actual) y en `Asignacion` (con el que se asignó) | Se mantienen ambos. | ✅ Resuelto |
| B-11 | Proyecto | No existe | `Proyecto` obligatorio en cada asignación | El formulario de asignación pedirá proyecto; se podrá crear al vuelo. | ✅ Resuelto |
| B-12 | Usuario admin inicial | — | Insertado con `PasswordHash = '<REEMPLAZAR_CON_HASH>'` | La fase 02 lo reemplaza al arrancar con una contraseña configurada. | ⏳ Fase 02 |

Cuando el cliente confirme un punto ❓, actualizar la fila y registrar el cambio en `bitacora.md`.
