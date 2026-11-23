/* =====================================================================
   002 - Índice para buscar refresh tokens por hash (Fase 02)

   /api/auth/refresh y /api/auth/logout buscan el token por TokenHash.
   Sin índice, cada búsqueda recorre toda la tabla. Se puede ejecutar
   más de una vez: si el índice ya existe, no hace nada.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_RefreshToken_TokenHash' AND object_id = OBJECT_ID('dbo.RefreshToken'))
    CREATE UNIQUE INDEX UQ_RefreshToken_TokenHash ON dbo.RefreshToken (TokenHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_RefreshToken_Usuario' AND object_id = OBJECT_ID('dbo.RefreshToken'))
    CREATE INDEX IX_RefreshToken_Usuario ON dbo.RefreshToken (UsuarioId, Revocado);
GO
