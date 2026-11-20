/* =====================================================================
   VmtFabric - Gestión de talento de la fábrica de software
   Motor: SQL Server Express 2019+ (probado para 2025 Express, v17)
   Backend: ASP.NET Core + EF Core
   Basado en: diseño Figma Make + requerimientos del cliente
     - 2 roles: Administrador (todo) y Gestor (solo Talento)
     - Login sin registro: el Administrador crea los usuarios
     - Asignaciones con tiempo, valor a cobrar, costo, margen e ingresos
     - Avisos (contratos por vencer, evaluaciones pendientes)
     - Crear evaluaciones y asignar un evaluador

   El script no crea la base ni cambia de base: se ejecuta conectado a la
   base destino (en hosting/nube la base ya existe y su nombre lo asigna
   el proveedor, p. ej. MonsterASP "dbXXXX").
   Nota: el importador del panel de MonsterASP rechaza el archivo si
   contiene ciertos comandos de servidor, incluso dentro de comentarios.

   Ejecución (el archivo está en UTF-8 con BOM):
     Nube (MonsterASP): panel -> Import SQL, o
       sqlcmd -S dbXXXX.public.databaseasp.net -d dbXXXX -U dbXXXX -P "<password>" -N -b -i VmtFabric_BD.sql
     Local (SQL Server Express): crear antes una base vacía "VmtFabric"
       (SSMS: clic derecho en Databases -> New Database) y luego:
       sqlcmd -S .\SQLEXPRESS -E -d VmtFabric -b -i VmtFabric_BD.sql
   Connection string local para la API:
     Server=.\SQLEXPRESS;Database=VmtFabric;Trusted_Connection=True;TrustServerCertificate=True

   Notas SQL Server Express:
     - Límite de 10 GB por base de datos (los archivos de Documento van a
       disco/blob; aquí solo se guarda la ruta).
     - No incluye SQL Server Agent: sp_GenerarAvisos se ejecuta desde un
       BackgroundService de .NET (o el Programador de tareas con sqlcmd).
   ===================================================================== */

-- sqlcmd arranca con QUOTED_IDENTIFIER OFF; los índices filtrados y las
-- columnas calculadas PERSISTED exigen estas opciones en ON.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Seguridad: no ejecutar el esquema por error en una base del sistema
IF DB_NAME() IN ('master', 'model', 'msdb', 'tempdb')
BEGIN
    RAISERROR('Conéctese a la base destino (sqlcmd -d <base>) antes de ejecutar el script.', 16, 1);
    SET NOEXEC ON;
END
GO

-- El script crea el esquema desde cero: si ya existe, se detiene sin tocar nada
IF OBJECT_ID('dbo.Rol', 'U') IS NOT NULL
BEGIN
    RAISERROR('La base VmtFabric ya tiene el esquema creado. Elimínela antes de volver a ejecutar el script.', 16, 1);
    SET NOEXEC ON;
END
GO

/* =====================================================================
   1. SEGURIDAD
   ===================================================================== */
CREATE TABLE Rol (
    RolId           TINYINT       NOT NULL PRIMARY KEY,
    Nombre          NVARCHAR(30)  NOT NULL UNIQUE,         -- Administrador, Gestor
    Descripcion     NVARCHAR(200) NULL
);

CREATE TABLE Usuario (
    UsuarioId            INT IDENTITY(1,1) PRIMARY KEY,
    Nombre               NVARCHAR(120) NOT NULL,
    Correo               NVARCHAR(150) NOT NULL,
    PasswordHash         NVARCHAR(255) NOT NULL,           -- hash (BCrypt / ASP.NET Identity), nunca texto plano
    RolId                TINYINT       NOT NULL REFERENCES Rol(RolId),
    Activo               BIT           NOT NULL DEFAULT 1,
    DebeCambiarPassword  BIT           NOT NULL DEFAULT 1, -- contraseña temporal creada por el admin
    UltimoAcceso         DATETIME2     NULL,
    IntentosFallidos     TINYINT       NOT NULL DEFAULT 0,
    BloqueadoHasta       DATETIME2     NULL,
    CreadoPor            INT           NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    FechaModificacion    DATETIME2     NULL,
    CONSTRAINT UQ_Usuario_Correo UNIQUE (Correo)
);

-- "¿Olvidaste tu contraseña?"
CREATE TABLE RecuperacionPassword (
    RecuperacionId  INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId       INT           NOT NULL REFERENCES Usuario(UsuarioId),
    TokenHash       NVARCHAR(255) NOT NULL,
    Expira          DATETIME2     NOT NULL,
    Usado           BIT           NOT NULL DEFAULT 0,
    FechaCreacion   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Sesiones JWT (refresh tokens)
CREATE TABLE RefreshToken (
    RefreshTokenId  INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId       INT           NOT NULL REFERENCES Usuario(UsuarioId),
    TokenHash       NVARCHAR(255) NOT NULL,
    Expira          DATETIME2     NOT NULL,
    Revocado        BIT           NOT NULL DEFAULT 0,
    FechaCreacion   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

/* =====================================================================
   2. CATÁLOGOS
   ===================================================================== */
CREATE TABLE EstadoColaborador (
    EstadoColaboradorId TINYINT      NOT NULL PRIMARY KEY,
    Nombre              NVARCHAR(30) NOT NULL UNIQUE           -- Asignado, Disponible, Inactivo
);

CREATE TABLE Seniority (
    SeniorityId  TINYINT      NOT NULL PRIMARY KEY,
    Nombre       NVARCHAR(30) NOT NULL UNIQUE,                  -- Junior, Semi Senior, Senior, Lead
    Orden        TINYINT      NOT NULL                          -- para "siguiente nivel" en plan de carrera
);

CREATE TABLE Cargo (
    CargoId  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre   NVARCHAR(80) NOT NULL UNIQUE,                      -- Full Stack Developer, QA Automation...
    Activo   BIT          NOT NULL DEFAULT 1
);

-- Catálogo de Tecnologías (CRUD solo Administrador)
CREATE TABLE Tecnologia (
    TecnologiaId   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         NVARCHAR(60) NOT NULL UNIQUE,
    Categoria      NVARCHAR(40) NULL,                           -- Frontend, Backend, Cloud, QA, Datos...
    Activo         BIT          NOT NULL DEFAULT 1,
    FechaCreacion  DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE TipoDocumento (
    TipoDocumentoId TINYINT      NOT NULL PRIMARY KEY,
    Nombre          NVARCHAR(40) NOT NULL UNIQUE                -- CV, Cédula, Contrato, Otro
);

CREATE TABLE ModalidadTarifa (
    ModalidadTarifaId TINYINT      NOT NULL PRIMARY KEY,
    Nombre            NVARCHAR(20) NOT NULL UNIQUE              -- Mensual, Por hora
);

/* =====================================================================
   3. TALENTO: COLABORADORES
   ===================================================================== */
CREATE TABLE Colaborador (
    ColaboradorId        INT IDENTITY(1,1) PRIMARY KEY,
    Cedula               CHAR(10)      NOT NULL,
    Nombres              NVARCHAR(80)  NOT NULL,
    Apellidos            NVARCHAR(80)  NOT NULL,
    FechaNacimiento      DATE          NOT NULL,                -- la EDAD se calcula (ver vista)
    Correo               NVARCHAR(150) NOT NULL,
    Celular              VARCHAR(20)   NULL,
    Ubicacion            NVARCHAR(100) NULL,
    EstadoColaboradorId  TINYINT       NOT NULL REFERENCES EstadoColaborador(EstadoColaboradorId),
    CargoId              INT           NOT NULL REFERENCES Cargo(CargoId),
    SeniorityId          TINYINT       NOT NULL REFERENCES Seniority(SeniorityId),
    CostoMensualRef      DECIMAL(12,2) NULL,                    -- precarga el "costo del colaborador" en la asignación
    Eliminado            BIT           NOT NULL DEFAULT 0,      -- borrado lógico
    CreadoPor            INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    ModificadoPor        INT           NULL REFERENCES Usuario(UsuarioId),
    FechaModificacion    DATETIME2     NULL,
    CONSTRAINT UQ_Colaborador_Cedula UNIQUE (Cedula),
    CONSTRAINT UQ_Colaborador_Correo UNIQUE (Correo),
    CONSTRAINT CK_Colaborador_Cedula CHECK (Cedula NOT LIKE '%[^0-9]%')
);
CREATE INDEX IX_Colaborador_Filtros ON Colaborador (EstadoColaboradorId, CargoId, SeniorityId) WHERE Eliminado = 0;

-- Pestaña "Tecnologías" del colaborador
CREATE TABLE ColaboradorTecnologia (
    ColaboradorId  INT     NOT NULL REFERENCES Colaborador(ColaboradorId),
    TecnologiaId   INT     NOT NULL REFERENCES Tecnologia(TecnologiaId),
    Nivel          TINYINT NOT NULL CHECK (Nivel BETWEEN 1 AND 5),
    AniosExperiencia DECIMAL(4,1) NULL,
    PRIMARY KEY (ColaboradorId, TecnologiaId)
);

/* =====================================================================
   4. CLIENTES Y PROYECTOS
   ===================================================================== */
CREATE TABLE Cliente (
    ClienteId          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre             NVARCHAR(120) NOT NULL,
    Ruc                CHAR(13)      NULL,
    Industria          NVARCHAR(60)  NULL,
    ContactoNombre     NVARCHAR(120) NULL,
    ContactoCorreo     NVARCHAR(150) NULL,
    ContactoTelefono   VARCHAR(20)   NULL,
    InicioRelacion     DATE          NULL,
    Activo             BIT           NOT NULL DEFAULT 1,
    CreadoPor          INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Cliente_Nombre UNIQUE (Nombre)
);
CREATE UNIQUE INDEX UQ_Cliente_Ruc ON Cliente (Ruc) WHERE Ruc IS NOT NULL;

CREATE TABLE Proyecto (
    ProyectoId     INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId      INT           NOT NULL REFERENCES Cliente(ClienteId),
    Nombre         NVARCHAR(120) NOT NULL,                       -- "Portal B2B"
    Descripcion    NVARCHAR(500) NULL,
    Activo         BIT           NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Proyecto UNIQUE (ClienteId, Nombre)
);

/* =====================================================================
   5. ASIGNACIONES (tiempo + valor a cobrar + costo + margen)
   ===================================================================== */
CREATE TABLE Asignacion (
    AsignacionId        INT IDENTITY(1,1) PRIMARY KEY,
    ColaboradorId       INT           NOT NULL REFERENCES Colaborador(ColaboradorId),
    ClienteId           INT           NOT NULL REFERENCES Cliente(ClienteId),
    ProyectoId          INT           NOT NULL REFERENCES Proyecto(ProyectoId),
    CargoId             INT           NOT NULL REFERENCES Cargo(CargoId),          -- cargo con el que se asignó
    SeniorityId         TINYINT       NOT NULL REFERENCES Seniority(SeniorityId),
    LiderCliente        NVARCHAR(120) NULL,
    FechaInicio         DATE          NOT NULL,
    FechaFin            DATE          NOT NULL,                                    -- tiempo que estará con el cliente
    ModalidadTarifaId   TINYINT       NOT NULL REFERENCES ModalidadTarifa(ModalidadTarifaId),
    ValorCobrar         DECIMAL(12,2) NOT NULL CHECK (ValorCobrar > 0),           -- tarifa mensual u hora
    HorasMes            SMALLINT      NULL,                                        -- requerido si la modalidad es Por hora
    CostoColaborador    DECIMAL(12,2) NOT NULL CHECK (CostoColaborador >= 0),     -- mismo período que la tarifa
    MargenPct AS (CAST(CASE WHEN ValorCobrar = 0 THEN 0
                       ELSE (ValorCobrar - CostoColaborador) * 100.0 / ValorCobrar END AS DECIMAL(6,2))) PERSISTED,
    AvisarDiasAntes     SMALLINT      NOT NULL DEFAULT 30,                         -- "Avisarme 30 días antes..."
    Estado              VARCHAR(12)   NOT NULL DEFAULT 'Activa'
                        CHECK (Estado IN ('Activa','Finalizada','Cancelada')),
    FechaCierre         DATE          NULL,
    MotivoCierre        NVARCHAR(200) NULL,                                        -- fin de contrato, rotación...
    FuncionesInicio     NVARCHAR(1000) NULL,
    FuncionesActuales   NVARCHAR(1000) NULL,
    CreadoPor           INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Asignacion_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CK_Asignacion_Horas  CHECK (ModalidadTarifaId <> 2 OR HorasMes IS NOT NULL)
);
-- Regla: un colaborador solo puede tener UNA asignación activa
CREATE UNIQUE INDEX UQ_Asignacion_ActivaPorColaborador ON Asignacion (ColaboradorId) WHERE Estado = 'Activa';
CREATE INDEX IX_Asignacion_Vencimiento ON Asignacion (FechaFin) INCLUDE (ColaboradorId, ClienteId) WHERE Estado = 'Activa';
CREATE INDEX IX_Asignacion_Cliente ON Asignacion (ClienteId, Estado);

CREATE TABLE AsignacionTecnologia (
    AsignacionId  INT NOT NULL REFERENCES Asignacion(AsignacionId),
    TecnologiaId  INT NOT NULL REFERENCES Tecnologia(TecnologiaId),
    PRIMARY KEY (AsignacionId, TecnologiaId)
);

-- Ingresos por asignación (proyectado -> facturado -> cobrado); alimenta "Ingresos del mes"
CREATE TABLE Ingreso (
    IngresoId       INT IDENTITY(1,1) PRIMARY KEY,
    AsignacionId    INT           NOT NULL REFERENCES Asignacion(AsignacionId),
    Periodo         DATE          NOT NULL,                     -- primer día del mes (2026-11-01)
    Monto           DECIMAL(12,2) NOT NULL CHECK (Monto >= 0),
    Costo           DECIMAL(12,2) NOT NULL DEFAULT 0,
    Estado          VARCHAR(12)   NOT NULL DEFAULT 'Proyectado'
                    CHECK (Estado IN ('Proyectado','Facturado','Cobrado','Anulado')),
    NumeroFactura   VARCHAR(30)   NULL,
    FechaCobro      DATE          NULL,
    Observacion     NVARCHAR(300) NULL,
    RegistradoPor   INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaRegistro   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Ingreso_Periodo UNIQUE (AsignacionId, Periodo),
    CONSTRAINT CK_Ingreso_Periodo CHECK (DAY(Periodo) = 1)
);

/* =====================================================================
   6. DOCUMENTOS (colaborador o cliente)
   ===================================================================== */
CREATE TABLE Documento (
    DocumentoId      INT IDENTITY(1,1) PRIMARY KEY,
    ColaboradorId    INT           NULL REFERENCES Colaborador(ColaboradorId),
    ClienteId        INT           NULL REFERENCES Cliente(ClienteId),
    TipoDocumentoId  TINYINT       NOT NULL REFERENCES TipoDocumento(TipoDocumentoId),
    NombreArchivo    NVARCHAR(200) NOT NULL,
    RutaAlmacen      NVARCHAR(400) NOT NULL,                    -- ruta en disco / blob key
    MimeType         VARCHAR(100)  NOT NULL,
    TamanoBytes      BIGINT        NOT NULL,
    Eliminado        BIT           NOT NULL DEFAULT 0,
    SubidoPor        INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCarga       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Documento_Propietario CHECK (
        (ColaboradorId IS NOT NULL AND ClienteId IS NULL) OR
        (ColaboradorId IS NULL AND ClienteId IS NOT NULL))
);

/* =====================================================================
   7. PLAN DE CARRERA
   ===================================================================== */
CREATE TABLE PlanCarrera (
    PlanCarreraId       INT IDENTITY(1,1) PRIMARY KEY,
    ColaboradorId       INT          NOT NULL REFERENCES Colaborador(ColaboradorId),
    SeniorityActualId   TINYINT      NOT NULL REFERENCES Seniority(SeniorityId),
    SeniorityObjetivoId TINYINT      NOT NULL REFERENCES Seniority(SeniorityId),  -- siguiente nivel
    CargoObjetivoId     INT          NULL REFERENCES Cargo(CargoId),
    FechaInicio         DATE         NOT NULL,
    FechaObjetivo       DATE         NULL,
    Estado              VARCHAR(12)  NOT NULL DEFAULT 'Vigente'
                        CHECK (Estado IN ('Vigente','Cumplido','Cancelado')),
    CreadoPor           INT          NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion       DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX UQ_PlanCarrera_Vigente ON PlanCarrera (ColaboradorId) WHERE Estado = 'Vigente';

CREATE TABLE PlanObjetivo (
    PlanObjetivoId   INT IDENTITY(1,1) PRIMARY KEY,
    PlanCarreraId    INT           NOT NULL REFERENCES PlanCarrera(PlanCarreraId) ON DELETE CASCADE,
    Descripcion      NVARCHAR(200) NOT NULL,                    -- "Certificación AWS Solutions Architect"
    Tipo             VARCHAR(15)   NOT NULL DEFAULT 'Objetivo'
                     CHECK (Tipo IN ('Objetivo','Curso','Certificacion','Promocion')),
    Peso             TINYINT       NOT NULL DEFAULT 1,          -- para calcular el % de progreso
    FechaLimite      DATE          NULL,
    Completado       BIT           NOT NULL DEFAULT 0,
    FechaCompletado  DATE          NULL
);

/* =====================================================================
   8. EVALUACIONES (crear, asignar evaluador, responder, resultados)
   ===================================================================== */
CREATE TABLE PeriodoEvaluacion (
    PeriodoEvaluacionId INT IDENTITY(1,1) PRIMARY KEY,
    Nombre              NVARCHAR(60) NOT NULL UNIQUE,           -- "Julio – septiembre 2026"
    FechaInicio         DATE         NOT NULL,
    FechaFin            DATE         NOT NULL,
    CHECK (FechaFin >= FechaInicio)
);

CREATE TABLE Evaluacion (
    EvaluacionId          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre                NVARCHAR(120) NOT NULL,
    PeriodoEvaluacionId   INT           NOT NULL REFERENCES PeriodoEvaluacion(PeriodoEvaluacionId),
    ColaboradorId         INT           NOT NULL REFERENCES Colaborador(ColaboradorId),   -- evaluado
    EvaluadorUsuarioId    INT           NOT NULL REFERENCES Usuario(UsuarioId),           -- EVALUADOR asignado
    EscalaMin             TINYINT       NOT NULL DEFAULT 1,
    EscalaMax             TINYINT       NOT NULL DEFAULT 5,
    FechaLimite           DATE          NULL,
    Estado                VARCHAR(12)   NOT NULL DEFAULT 'Pendiente'
                          CHECK (Estado IN ('Pendiente','EnCurso','Completada','Cancelada')),
    PuntajeFinal          DECIMAL(5,2)  NULL,                    -- 0-100, calculado al completar
    ComentarioGeneral     NVARCHAR(1000) NULL,
    FechaInicioRespuesta  DATETIME2     NULL,
    FechaCompletada       DATETIME2     NULL,
    CreadoPor             INT           NOT NULL REFERENCES Usuario(UsuarioId),
    FechaCreacion         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Evaluacion_Escala CHECK (EscalaMax > EscalaMin)
);
CREATE INDEX IX_Evaluacion_Evaluador ON Evaluacion (EvaluadorUsuarioId, Estado);
CREATE INDEX IX_Evaluacion_Colaborador ON Evaluacion (ColaboradorId);

-- Criterios de cada evaluación y la respuesta del evaluador
CREATE TABLE EvaluacionCriterio (
    EvaluacionCriterioId INT IDENTITY(1,1) PRIMARY KEY,
    EvaluacionId         INT           NOT NULL REFERENCES Evaluacion(EvaluacionId) ON DELETE CASCADE,
    Orden                TINYINT       NOT NULL,
    Descripcion          NVARCHAR(200) NOT NULL,                -- "Calidad del trabajo"
    Peso                 DECIMAL(5,2)  NOT NULL DEFAULT 1,
    Puntaje              TINYINT       NULL,                    -- respuesta del evaluador (1-5)
    Comentario           NVARCHAR(500) NULL,
    CONSTRAINT UQ_EvaluacionCriterio_Orden UNIQUE (EvaluacionId, Orden)
);

-- Plantillas reutilizables de criterios (opcional para "Añadir criterio" rápido)
CREATE TABLE CriterioPlantilla (
    CriterioPlantillaId INT IDENTITY(1,1) PRIMARY KEY,
    Descripcion         NVARCHAR(200) NOT NULL UNIQUE,
    Activo              BIT           NOT NULL DEFAULT 1
);

/* =====================================================================
   9. AVISOS / NOTIFICACIONES (campana con contador + panel de alertas)
   ===================================================================== */
CREATE TABLE Aviso (
    AvisoId          INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId        INT           NOT NULL REFERENCES Usuario(UsuarioId),   -- destinatario
    Tipo             VARCHAR(30)   NOT NULL
                     CHECK (Tipo IN ('AsignacionPorVencer','EvaluacionAsignada','EvaluacionPendiente',
                                     'PlanCarrera','Sistema')),
    Titulo           NVARCHAR(150) NOT NULL,
    Mensaje          NVARCHAR(500) NOT NULL,
    EntidadTipo      VARCHAR(30)   NULL,                       -- 'Asignacion', 'Evaluacion'...
    EntidadId        INT           NULL,                       -- para navegar al detalle
    Leido            BIT           NOT NULL DEFAULT 0,
    FechaLectura     DATETIME2     NULL,
    FechaCreacion    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Aviso_NoLeidos ON Aviso (UsuarioId, Leido, FechaCreacion DESC);
-- Evita duplicar el mismo aviso para la misma entidad
CREATE UNIQUE INDEX UQ_Aviso_Entidad ON Aviso (UsuarioId, Tipo, EntidadTipo, EntidadId) WHERE EntidadId IS NOT NULL;

/* =====================================================================
   10. AUDITORÍA
   ===================================================================== */
CREATE TABLE Auditoria (
    AuditoriaId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId     INT           NULL REFERENCES Usuario(UsuarioId),
    Accion        VARCHAR(20)   NOT NULL,                      -- Crear, Editar, Eliminar, Login, Exportar
    Entidad       VARCHAR(50)   NOT NULL,
    EntidadId     INT           NULL,
    Detalle       NVARCHAR(MAX) NULL,                          -- JSON con cambios
    Fecha         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* =====================================================================
   11. VISTAS (Dashboard, Clientes, Análisis, Reportes)
   ===================================================================== */
CREATE OR ALTER VIEW vw_Colaborador AS
SELECT  c.ColaboradorId, c.Cedula, c.Nombres, c.Apellidos,
        CONCAT(c.Nombres, ' ', c.Apellidos) AS NombreCompleto,
        DATEDIFF(YEAR, c.FechaNacimiento, GETDATE())
          - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, c.FechaNacimiento, GETDATE()), c.FechaNacimiento) > GETDATE()
                 THEN 1 ELSE 0 END AS Edad,
        c.Correo, c.Celular, c.Ubicacion,
        e.Nombre AS Estado, ca.Nombre AS Cargo, s.Nombre AS Seniority,
        a.AsignacionId, cl.Nombre AS Cliente, p.Nombre AS Proyecto, a.FechaFin,
        DATEDIFF(DAY, CAST(GETDATE() AS DATE), a.FechaFin) AS DiasParaVencer
FROM Colaborador c
JOIN EstadoColaborador e ON e.EstadoColaboradorId = c.EstadoColaboradorId
JOIN Cargo ca            ON ca.CargoId = c.CargoId
JOIN Seniority s         ON s.SeniorityId = c.SeniorityId
LEFT JOIN Asignacion a   ON a.ColaboradorId = c.ColaboradorId AND a.Estado = 'Activa'
LEFT JOIN Cliente cl     ON cl.ClienteId = a.ClienteId
LEFT JOIN Proyecto p     ON p.ProyectoId = a.ProyectoId
WHERE c.Eliminado = 0;
GO

-- Resumen financiero de cada asignación (modal "Nueva asignación")
CREATE OR ALTER VIEW vw_AsignacionResumen AS
SELECT  a.AsignacionId, a.ColaboradorId, a.ClienteId, a.Estado,
        a.FechaInicio, a.FechaFin,
        DATEDIFF(MONTH, a.FechaInicio, DATEADD(DAY, 1, a.FechaFin)) AS MesesDuracion,
        CASE WHEN a.ModalidadTarifaId = 1 THEN a.ValorCobrar
             ELSE a.ValorCobrar * a.HorasMes END AS IngresoMensual,
        CASE WHEN a.ModalidadTarifaId = 1 THEN a.CostoColaborador
             ELSE a.CostoColaborador * a.HorasMes END AS CostoMensual,
        a.MargenPct
FROM Asignacion a;
GO

CREATE OR ALTER VIEW vw_AsignacionEstimado AS
SELECT  r.*,
        r.IngresoMensual * r.MesesDuracion                    AS IngresoTotalEstimado,
        (r.IngresoMensual - r.CostoMensual) * r.MesesDuracion AS MargenTotalEstimado
FROM vw_AsignacionResumen r;
GO

-- Avisos: asignaciones que entran en su ventana de aviso
CREATE OR ALTER VIEW vw_AsignacionesPorVencer AS
SELECT  a.AsignacionId, a.ColaboradorId,
        CONCAT(c.Nombres, ' ', c.Apellidos) AS Colaborador,
        cl.Nombre AS Cliente, a.FechaFin,
        DATEDIFF(DAY, CAST(GETDATE() AS DATE), a.FechaFin) AS DiasRestantes
FROM Asignacion a
JOIN Colaborador c ON c.ColaboradorId = a.ColaboradorId
JOIN Cliente cl    ON cl.ClienteId = a.ClienteId
WHERE a.Estado = 'Activa'
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), a.FechaFin) BETWEEN 0 AND a.AvisarDiasAntes;
GO

-- KPIs del Dashboard
CREATE OR ALTER VIEW vw_DashboardKpi AS
SELECT
  (SELECT COUNT(*) FROM Colaborador WHERE Eliminado = 0)                                AS TotalColaboradores,
  (SELECT COUNT(*) FROM Colaborador WHERE Eliminado = 0 AND EstadoColaboradorId = 1)    AS Asignados,
  (SELECT COUNT(*) FROM Colaborador WHERE Eliminado = 0 AND EstadoColaboradorId = 2)    AS Disponibles,
  (SELECT ISNULL(SUM(Monto), 0) FROM Ingreso
    WHERE Estado <> 'Anulado'
      AND Periodo = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))               AS IngresosMes,
  (SELECT COUNT(*) FROM vw_AsignacionesPorVencer)                                       AS ContratosPorVencer,
  (SELECT COUNT(*) FROM Evaluacion WHERE Estado IN ('Pendiente','EnCurso'))             AS EvaluacionesPendientes;
GO

-- Clientes: colaboradores, tiempo e ingresos generados
CREATE OR ALTER VIEW vw_ClienteResumen AS
SELECT  cl.ClienteId, cl.Nombre, cl.Industria,
        COUNT(DISTINCT CASE WHEN a.Estado = 'Activa' THEN a.ColaboradorId END) AS ColaboradoresActivos,
        COUNT(DISTINCT a.ColaboradorId)                                         AS ColaboradoresHistorico,
        ISNULL((SELECT SUM(i.Monto) FROM Ingreso i
                JOIN Asignacion a2 ON a2.AsignacionId = i.AsignacionId
                WHERE a2.ClienteId = cl.ClienteId AND i.Estado <> 'Anulado'), 0) AS IngresosGenerados,
        MIN(CASE WHEN a.Estado = 'Activa' THEN a.FechaFin END)                  AS ProximoVencimiento
FROM Cliente cl
LEFT JOIN Asignacion a ON a.ClienteId = cl.ClienteId
GROUP BY cl.ClienteId, cl.Nombre, cl.Industria;
GO

-- Plan de carrera: % de progreso
CREATE OR ALTER VIEW vw_PlanCarreraProgreso AS
SELECT  pc.PlanCarreraId, pc.ColaboradorId,
        sa.Nombre AS NivelActual, so.Nombre AS SiguienteNivel,
        CAST(ISNULL(SUM(CASE WHEN o.Completado = 1 THEN o.Peso END) * 100.0
                    / NULLIF(SUM(o.Peso), 0), 0) AS DECIMAL(5,2)) AS ProgresoPct,
        COUNT(o.PlanObjetivoId) AS TotalObjetivos,
        SUM(CASE WHEN o.Completado = 1 THEN 1 ELSE 0 END) AS ObjetivosCompletados
FROM PlanCarrera pc
JOIN Seniority sa ON sa.SeniorityId = pc.SeniorityActualId
JOIN Seniority so ON so.SeniorityId = pc.SeniorityObjetivoId
LEFT JOIN PlanObjetivo o ON o.PlanCarreraId = pc.PlanCarreraId
WHERE pc.Estado = 'Vigente'
GROUP BY pc.PlanCarreraId, pc.ColaboradorId, sa.Nombre, so.Nombre;
GO

-- Análisis: ingresos por cliente y mes
CREATE OR ALTER VIEW vw_IngresosClienteMes AS
SELECT  cl.ClienteId, cl.Nombre AS Cliente, i.Periodo,
        SUM(i.Monto) AS Ingresos, SUM(i.Costo) AS Costos, SUM(i.Monto - i.Costo) AS Margen
FROM Ingreso i
JOIN Asignacion a ON a.AsignacionId = i.AsignacionId
JOIN Cliente cl   ON cl.ClienteId = a.ClienteId
WHERE i.Estado <> 'Anulado'
GROUP BY cl.ClienteId, cl.Nombre, i.Periodo;
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

SET NOEXEC OFF;  -- restaura la sesión si el script se detuvo por esquema existente
GO
