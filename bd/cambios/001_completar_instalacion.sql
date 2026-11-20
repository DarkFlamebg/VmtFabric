/* =====================================================================
   001 - Completar la instalación inicial (procedimientos y datos iniciales)

   Ejecutar UNA sola vez sobre la base que ya tiene las tablas.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Guarda: si los datos iniciales ya existen, no se hace nada
IF EXISTS (SELECT 1 FROM dbo.Rol)
BEGIN
    RAISERROR('Los datos iniciales ya existen: este script ya se ejecutó.', 16, 1);
    SET NOEXEC ON;
END
GO

/* =====================================================================
   12. PROCEDIMIENTOS
   ===================================================================== */
-- Calcula el puntaje (0-100) al completar una evaluación
CREATE OR ALTER PROCEDURE sp_CompletarEvaluacion
    @EvaluacionId INT,
    @ComentarioGeneral NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM EvaluacionCriterio WHERE EvaluacionId = @EvaluacionId AND Puntaje IS NULL)
        THROW 50001, 'Todos los criterios deben tener puntaje.', 1;

    UPDATE e SET
        PuntajeFinal = x.Puntaje,
        ComentarioGeneral = @ComentarioGeneral,
        Estado = 'Completada',
        FechaCompletada = SYSUTCDATETIME()
    FROM Evaluacion e
    -- Puntaje 0-100 = Σ((p - min)/(max - min)·w) / Σw  =  (Σ(p·w) - min·Σw) / ((max - min)·Σw)
    -- Las columnas de "e" quedan fuera de los SUM: SQL Server no permite mezclar
    -- referencias externas con columnas internas dentro de un agregado (error 8124).
    CROSS APPLY (
        SELECT SUM(c.Puntaje * c.Peso) AS SumaPonderada, SUM(c.Peso) AS SumaPesos
        FROM EvaluacionCriterio c WHERE c.EvaluacionId = e.EvaluacionId
    ) s
    CROSS APPLY (
        SELECT CAST((s.SumaPonderada - e.EscalaMin * s.SumaPesos) * 100.0
                    / ((e.EscalaMax - e.EscalaMin) * NULLIF(s.SumaPesos, 0)) AS DECIMAL(5,2)) AS Puntaje
    ) x
    WHERE e.EvaluacionId = @EvaluacionId;
END;
GO

-- Genera avisos (ejecutar a diario desde un BackgroundService/Hangfire de .NET;
-- SQL Server Express no trae SQL Agent)
CREATE OR ALTER PROCEDURE sp_GenerarAvisos
AS
BEGIN
    SET NOCOUNT ON;
    -- Asignaciones por vencer -> todos los usuarios activos
    INSERT INTO Aviso (UsuarioId, Tipo, Titulo, Mensaje, EntidadTipo, EntidadId)
    SELECT u.UsuarioId, 'AsignacionPorVencer',
           N'Asignación por vencer',
           CONCAT(v.Colaborador, N' · ', v.Cliente, N' · vence en ', v.DiasRestantes, N' días'),
           'Asignacion', v.AsignacionId
    FROM vw_AsignacionesPorVencer v
    CROSS JOIN Usuario u
    WHERE u.Activo = 1
      AND NOT EXISTS (SELECT 1 FROM Aviso x WHERE x.UsuarioId = u.UsuarioId
                      AND x.Tipo = 'AsignacionPorVencer' AND x.EntidadTipo = 'Asignacion'
                      AND x.EntidadId = v.AsignacionId);

    -- Evaluaciones pendientes a 3 días de su fecha límite -> evaluador
    INSERT INTO Aviso (UsuarioId, Tipo, Titulo, Mensaje, EntidadTipo, EntidadId)
    SELECT e.EvaluadorUsuarioId, 'EvaluacionPendiente',
           N'Evaluación pendiente', CONCAT(e.Nombre, N' vence el ', CONVERT(VARCHAR(10), e.FechaLimite, 103)),
           'Evaluacion', e.EvaluacionId
    FROM Evaluacion e
    WHERE e.Estado IN ('Pendiente','EnCurso')
      AND e.FechaLimite IS NOT NULL
      AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), e.FechaLimite) <= 3
      AND NOT EXISTS (SELECT 1 FROM Aviso x WHERE x.UsuarioId = e.EvaluadorUsuarioId
                      AND x.Tipo = 'EvaluacionPendiente' AND x.EntidadTipo = 'Evaluacion'
                      AND x.EntidadId = e.EvaluacionId);
END;
GO

/* =====================================================================
   13. DATOS INICIALES
   ===================================================================== */
INSERT INTO Rol (RolId, Nombre, Descripcion) VALUES
 (1, N'Administrador', N'Acceso completo a todos los módulos'),
 (2, N'Gestor',        N'Solo módulo Talento: Colaboradores, Clientes, Plan de Carrera, Evaluaciones');

INSERT INTO EstadoColaborador (EstadoColaboradorId, Nombre) VALUES
 (1, N'Asignado'), (2, N'Disponible'), (3, N'Inactivo');

INSERT INTO Seniority (SeniorityId, Nombre, Orden) VALUES
 (1, N'Junior', 1), (2, N'Semi Senior', 2), (3, N'Senior', 3), (4, N'Lead', 4);

INSERT INTO TipoDocumento (TipoDocumentoId, Nombre) VALUES
 (1, N'CV'), (2, N'Cédula'), (3, N'Contrato'), (4, N'Otro');

INSERT INTO ModalidadTarifa (ModalidadTarifaId, Nombre) VALUES
 (1, N'Mensual'), (2, N'Por hora');

INSERT INTO Cargo (Nombre) VALUES
 (N'Full Stack Developer'), (N'Frontend Developer'), (N'Backend Developer'), (N'UX/UI Designer'),
 (N'QA Automation'), (N'DevOps Engineer'), (N'Data Engineer'), (N'Mobile Developer'), (N'Tech Lead');

INSERT INTO Tecnologia (Nombre, Categoria) VALUES
 (N'Angular','Frontend'), (N'React','Frontend'), (N'TypeScript','Frontend'), (N'.NET','Backend'),
 (N'Node.js','Backend'), (N'Java','Backend'), (N'Spring Boot','Backend'), (N'Python','Backend'),
 (N'SQL Server','Datos'), (N'PostgreSQL','Datos'), (N'AWS','Cloud'), (N'Azure','Cloud'),
 (N'Docker','DevOps'), (N'Kubernetes','DevOps'), (N'Cypress','QA'), (N'Selenium','QA'),
 (N'Figma','Diseño'), (N'React Native','Mobile'), (N'Swift','Mobile');

INSERT INTO CriterioPlantilla (Descripcion) VALUES
 (N'Calidad del trabajo'), (N'Colaboración y comunicación'), (N'Cumplimiento de objetivos'),
 (N'Conocimiento técnico'), (N'Proactividad');

-- Usuario administrador inicial: reemplazar el hash desde la API (PasswordHasher) en el primer despliegue
INSERT INTO Usuario (Nombre, Correo, PasswordHash, RolId, DebeCambiarPassword)
VALUES (N'Administrador', N'admin@vmtfabric.com', N'<REEMPLAZAR_CON_HASH>', 1, 1);
GO

SET NOEXEC OFF;
GO
