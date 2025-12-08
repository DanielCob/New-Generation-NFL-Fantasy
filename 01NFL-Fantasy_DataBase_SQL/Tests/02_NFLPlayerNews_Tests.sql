-- ============================================================================
-- UNIT TESTS: sp_AddNFLPlayerNews
-- ============================================================================

-- ===========================================
-- TEST 1: Crear noticia exitosamente (no lesión)
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews crea noticia regular exitosamente]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    EXEC tSQLt.FakeTable 'ref.PlayerDesignation';
    EXEC tSQLt.FakeTable 'audit.UserActionLog';

    -- Insertar datos de prueba
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    -- ACT
    DECLARE @NewsID BIGINT;
    DECLARE @Message NVARCHAR(MAX);

    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 1,
            @NFLPlayerID = 100,
            @NewsText = 'Mahomes tuvo una excelente práctica hoy',
            @IsInjury = 0,
            @InjurySummary = NULL,
            @Designation = NULL,
            @SourceIp = '192.168.1.1',
            @UserAgent = 'Test Agent';
    END TRY
    BEGIN CATCH
        EXEC tSQLt.Fail 'No debería lanzar excepción para noticia regular válida';
    END CATCH

    -- ASSERT
    DECLARE @ActualCount INT;
    SELECT @ActualCount = COUNT(*)
    FROM ref.NFLPlayerNews
    WHERE NFLPlayerID = 100
      AND NewsText = 'Mahomes tuvo una excelente práctica hoy'
      AND IsInjury = 0
      AND InjurySummary IS NULL
      AND DesignationID IS NULL;

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ActualCount,
        @Message = 'Debe crear exactamente 1 noticia regular';
END
GO

-- ===========================================
-- TEST 2: Crear noticia de lesión con designación
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews crea noticia de lesion y actualiza designacion]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    EXEC tSQLt.FakeTable 'ref.PlayerDesignation';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerChangeLog';
    EXEC tSQLt.FakeTable 'audit.UserActionLog';

    -- Datos de prueba
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (
        NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID, 
        CurrentDesignationID, InjuryStatus, InjuryDescription, 
        CreatedByUserID, UpdatedByUserID
    )
    VALUES (100, 'Patrick', 'Mahomes', 1, 1, NULL, 'Healthy', NULL, 1, 1);

    INSERT INTO ref.PlayerDesignation (DesignationID, DesignationCode, DesignationName, Description)
    VALUES (1, 'Q', 'Questionable', 'Probabilidad ~50%');

    -- ACT
    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 1,
            @NFLPlayerID = 100,
            @NewsText = 'Mahomes se lesionó el tobillo en práctica',
            @IsInjury = 1,
            @InjurySummary = 'Tobillo derecho',
            @Designation = 'Q',
            @SourceIp = '192.168.1.1',
            @UserAgent = 'Test Agent';
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
        EXEC tSQLt.Fail @ErrorMsg;
    END CATCH

    -- ASSERT 1: Noticia creada correctamente
    DECLARE @NewsCount INT;
    SELECT @NewsCount = COUNT(*)
    FROM ref.NFLPlayerNews
    WHERE NFLPlayerID = 100
      AND IsInjury = 1
      AND InjurySummary = 'Tobillo derecho'
      AND DesignationID = 1;

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @NewsCount,
        @Message = 'Debe crear 1 noticia de lesión';

    -- ASSERT 2: CurrentDesignationID actualizado
    DECLARE @CurrentDesignation TINYINT;
    SELECT @CurrentDesignation = CurrentDesignationID
    FROM ref.NFLPlayer
    WHERE NFLPlayerID = 100;

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @CurrentDesignation,
        @Message = 'Debe actualizar CurrentDesignationID a Q (1)';

    -- ASSERT 3: ChangeLog registrado
    DECLARE @ChangeLogCount INT;
    SELECT @ChangeLogCount = COUNT(*)
    FROM ref.NFLPlayerChangeLog
    WHERE NFLPlayerID = 100
      AND FieldName = 'CurrentDesignationID';

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ChangeLogCount,
        @Message = 'Debe registrar cambio en ChangeLog';
END
GO

-- ===========================================
-- TEST 3: Rechazar noticia si usuario no es ADMIN
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews rechaza si usuario no es ADMIN]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (2, 'USER');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    -- ACT & ASSERT
    DECLARE @ErrorOccurred BIT = 0;
    DECLARE @ErrorNumber INT;

    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 2,
            @NFLPlayerID = 100,
            @NewsText = 'Intento de noticia no autorizado',
            @IsInjury = 0;
    END TRY
    BEGIN CATCH
        SET @ErrorOccurred = 1;
        SET @ErrorNumber = ERROR_NUMBER();
    END CATCH

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ErrorOccurred,
        @Message = 'Debe lanzar error cuando usuario no es ADMIN';

    EXEC tSQLt.AssertEquals 
        @Expected = 50701,
        @Actual = @ErrorNumber,
        @Message = 'Debe lanzar error 50701 específico de permisos';
END
GO

-- ===========================================
-- TEST 4: Rechazar noticia con texto muy corto
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews rechaza texto menor a 10 caracteres]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    -- ACT & ASSERT
    DECLARE @ErrorOccurred BIT = 0;
    DECLARE @ErrorNumber INT;

    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 1,
            @NFLPlayerID = 100,
            @NewsText = 'Corto',  -- Solo 5 caracteres
            @IsInjury = 0;
    END TRY
    BEGIN CATCH
        SET @ErrorOccurred = 1;
        SET @ErrorNumber = ERROR_NUMBER();
    END CATCH

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ErrorOccurred,
        @Message = 'Debe rechazar texto menor a 10 caracteres';

    EXEC tSQLt.AssertEquals 
        @Expected = 50703,
        @Actual = @ErrorNumber,
        @Message = 'Debe lanzar error 50703 de longitud de texto';
END
GO

-- ===========================================
-- TEST 5: Rechazar lesión sin InjurySummary
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews rechaza lesion sin resumen]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    -- ACT & ASSERT
    DECLARE @ErrorOccurred BIT = 0;
    DECLARE @ErrorNumber INT;

    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 1,
            @NFLPlayerID = 100,
            @NewsText = 'Mahomes se lesionó en práctica',
            @IsInjury = 1,
            @InjurySummary = NULL,  -- Falta el resumen
            @Designation = 'Q';
    END TRY
    BEGIN CATCH
        SET @ErrorOccurred = 1;
        SET @ErrorNumber = ERROR_NUMBER();
    END CATCH

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ErrorOccurred,
        @Message = 'Debe rechazar lesión sin InjurySummary';

    EXEC tSQLt.AssertEquals 
        @Expected = 50704,
        @Actual = @ErrorNumber,
        @Message = 'Debe lanzar error 50704 de resumen requerido';
END
GO

-- ===========================================
-- TEST 6: Rechazar designación inválida
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_AddNFLPlayerNews rechaza designacion invalida]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.PlayerDesignation';
    
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    -- No insertar la designación 'INVALID'

    -- ACT & ASSERT
    DECLARE @ErrorOccurred BIT = 0;
    DECLARE @ErrorNumber INT;

    BEGIN TRY
        EXEC app.sp_AddNFLPlayerNews
            @ActorUserID = 1,
            @NFLPlayerID = 100,
            @NewsText = 'Mahomes se lesionó en práctica',
            @IsInjury = 1,
            @InjurySummary = 'Tobillo',
            @Designation = 'INVALID';  -- Designación que no existe
    END TRY
    BEGIN CATCH
        SET @ErrorOccurred = 1;
        SET @ErrorNumber = ERROR_NUMBER();
    END CATCH

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ErrorOccurred,
        @Message = 'Debe rechazar designación inválida';

    EXEC tSQLt.AssertEquals 
        @Expected = 50707,
        @Actual = @ErrorNumber,
        @Message = 'Debe lanzar error 50707 de designación inválida';
END
GO

-- ============================================================================
-- UNIT TESTS: sp_DeleteNFLPlayerNews
-- ============================================================================

-- ===========================================
-- TEST 7: Eliminar noticia regular exitosamente
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_DeleteNFLPlayerNews elimina noticia regular]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    EXEC tSQLt.FakeTable 'audit.UserActionLog';

    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.NFLPlayer (NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID)
    VALUES (100, 'Patrick', 'Mahomes', 1, 1);

    INSERT INTO ref.NFLPlayerNews (
        NewsID, NFLPlayerID, NewsText, IsInjury, 
        InjurySummary, DesignationID, CreatedByUserID, 
        IsDeleted, DeletedByUserID, DeletedAt
    )
    VALUES (1, 100, 'Noticia regular', 0, NULL, NULL, 1, 0, NULL, NULL);

    -- ACT
    BEGIN TRY
        EXEC app.sp_DeleteNFLPlayerNews
            @ActorUserID = 1,
            @NewsID = 1,
            @SourceIp = '192.168.1.1',
            @UserAgent = 'Test Agent';
    END TRY
    BEGIN CATCH
        EXEC tSQLt.Fail 'No debería lanzar excepción';
    END CATCH

    -- ASSERT
    DECLARE @IsDeleted BIT;
    SELECT @IsDeleted = IsDeleted
    FROM ref.NFLPlayerNews
    WHERE NewsID = 1;

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @IsDeleted,
        @Message = 'Debe marcar la noticia como eliminada';
END
GO

-- ===========================================
-- TEST 8: Eliminar noticia de lesión revierte designación
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_DeleteNFLPlayerNews revierte designacion correctamente]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    EXEC tSQLt.FakeTable 'ref.PlayerDesignation';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerChangeLog';
    EXEC tSQLt.FakeTable 'audit.UserActionLog';

    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.PlayerDesignation (DesignationID, DesignationCode, DesignationName, Description)
    VALUES 
        (1, 'Q', 'Questionable', 'Probabilidad ~50%'),
        (2, 'D', 'Doubtful', 'Muy poco probable');

    INSERT INTO ref.NFLPlayer (
        NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID, 
        CurrentDesignationID, InjuryStatus, InjuryDescription,
        CreatedByUserID, UpdatedByUserID
    )
    VALUES (100, 'Patrick', 'Mahomes', 1, 1, 2, 'Doubtful', 'Muy poco probable', 1, 1);

    -- Noticia antigua (designación Q)
    INSERT INTO ref.NFLPlayerNews (
        NewsID, NFLPlayerID, NewsText, IsInjury, 
        InjurySummary, DesignationID, CreatedByUserID, CreatedAt,
        IsDeleted
    )
    VALUES (1, 100, 'Lesión tobillo', 1, 'Tobillo', 1, 1, '2024-01-01', 0);

    -- Noticia más reciente (designación D) - esta la eliminaremos
    INSERT INTO ref.NFLPlayerNews (
        NewsID, NFLPlayerID, NewsText, IsInjury, 
        InjurySummary, DesignationID, CreatedByUserID, CreatedAt,
        IsDeleted
    )
    VALUES (2, 100, 'Lesión empeoró', 1, 'Tobillo peor', 2, 1, '2024-01-02', 0);

    -- ACT
    EXEC app.sp_DeleteNFLPlayerNews
        @ActorUserID = 1,
        @NewsID = 2;

    -- ASSERT 1: CurrentDesignationID debe revertir a Q (1)
    DECLARE @CurrentDesignation TINYINT;
    SELECT @CurrentDesignation = CurrentDesignationID
    FROM ref.NFLPlayer
    WHERE NFLPlayerID = 100;

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @CurrentDesignation,
        @Message = 'Debe revertir designación a la anterior (Q)';

    -- ASSERT 2: ChangeLog debe registrar el cambio
    DECLARE @ChangeLogCount INT;
    SELECT @ChangeLogCount = COUNT(*)
    FROM ref.NFLPlayerChangeLog
    WHERE NFLPlayerID = 100
      AND FieldName = 'CurrentDesignationID'
      AND OldValue = '2'
      AND NewValue = '1';

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ChangeLogCount,
        @Message = 'Debe registrar cambio de designación en ChangeLog';
END
GO

-- ===========================================
-- TEST 9: Eliminar última noticia de lesión revierte a NULL
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_DeleteNFLPlayerNews revierte a NULL si no hay previas]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayer';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    EXEC tSQLt.FakeTable 'ref.PlayerDesignation';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerChangeLog';
    EXEC tSQLt.FakeTable 'audit.UserActionLog';

    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (1, 'ADMIN');

    INSERT INTO ref.PlayerDesignation (DesignationID, DesignationCode, DesignationName, Description)
    VALUES (1, 'Q', 'Questionable', 'Probabilidad ~50%');

    INSERT INTO ref.NFLPlayer (
        NFLPlayerID, FirstName, LastName, IsActive, NFLTeamID, 
        CurrentDesignationID, InjuryStatus, InjuryDescription,
        CreatedByUserID, UpdatedByUserID
    )
    VALUES (100, 'Patrick', 'Mahomes', 1, 1, 1, 'Questionable', 'Probabilidad ~50%', 1, 1);

    -- Solo UNA noticia de lesión
    INSERT INTO ref.NFLPlayerNews (
        NewsID, NFLPlayerID, NewsText, IsInjury, 
        InjurySummary, DesignationID, CreatedByUserID, CreatedAt,
        IsDeleted
    )
    VALUES (1, 100, 'Lesión tobillo', 1, 'Tobillo', 1, 1, '2024-01-01', 0);

    -- ACT
    EXEC app.sp_DeleteNFLPlayerNews
        @ActorUserID = 1,
        @NewsID = 1;

    -- ASSERT 1: CurrentDesignationID debe ser NULL
    DECLARE @CurrentDesignation TINYINT;
    SELECT @CurrentDesignation = CurrentDesignationID
    FROM ref.NFLPlayer
    WHERE NFLPlayerID = 100;

    EXEC tSQLt.AssertEquals 
        @Expected = NULL,
        @Actual = @CurrentDesignation,
        @Message = 'Debe revertir designación a NULL si no hay noticias previas';

    -- ASSERT 2: InjuryStatus debe ser 'Healthy'
    DECLARE @InjuryStatus NVARCHAR(50);
    SELECT @InjuryStatus = InjuryStatus
    FROM ref.NFLPlayer
    WHERE NFLPlayerID = 100;

    EXEC tSQLt.AssertEquals 
        @Expected = 'Healthy',
        @Actual = @InjuryStatus,
        @Message = 'Debe establecer InjuryStatus como Healthy';
END
GO

-- ===========================================
-- TEST 10: Rechazar eliminación si usuario no es ADMIN
-- ===========================================
CREATE OR ALTER PROCEDURE PlayerNewsTests.[test sp_DeleteNFLPlayerNews rechaza si usuario no es ADMIN]
AS
BEGIN
    -- ARRANGE
    EXEC tSQLt.FakeTable 'auth.UserAccount';
    EXEC tSQLt.FakeTable 'ref.NFLPlayerNews';
    
    INSERT INTO auth.UserAccount (UserID, SystemRoleCode)
    VALUES (2, 'USER');

    INSERT INTO ref.NFLPlayerNews (
        NewsID, NFLPlayerID, NewsText, IsInjury, CreatedByUserID, IsDeleted
    )
    VALUES (1, 100, 'Noticia', 0, 1, 0);

    -- ACT & ASSERT
    DECLARE @ErrorOccurred BIT = 0;
    DECLARE @ErrorNumber INT;

    BEGIN TRY
        EXEC app.sp_DeleteNFLPlayerNews
            @ActorUserID = 2,
            @NewsID = 1;
    END TRY
    BEGIN CATCH
        SET @ErrorOccurred = 1;
        SET @ErrorNumber = ERROR_NUMBER();
    END CATCH

    EXEC tSQLt.AssertEquals 
        @Expected = 1,
        @Actual = @ErrorOccurred,
        @Message = 'Debe rechazar si usuario no es ADMIN';

    EXEC tSQLt.AssertEquals 
        @Expected = 50711,
        @Actual = @ErrorNumber,
        @Message = 'Debe lanzar error 50711 de permisos';
END
GO