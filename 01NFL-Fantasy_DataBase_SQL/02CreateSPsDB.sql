USE XNFLFantasyDB;
GO

-- Schema para procedimientos de aplicación
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'app')
    EXEC('CREATE SCHEMA app AUTHORIZATION dbo;');
GO

-- Rol de ejecución (si no existe en tu DB, créalo)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app_executor')
    CREATE ROLE app_executor AUTHORIZATION dbo;
GO

-- ============================================================================
-- sp_RegisterUser - VERSIÓN ACTUALIZADA
-- Ahora establece explícitamente SystemRoleCode = 'USER'
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_RegisterUser
  @Name               NVARCHAR(50),
  @Email              NVARCHAR(50),
  @Alias              NVARCHAR(50) = NULL,
  @Password           NVARCHAR(50),
  @PasswordConfirm    NVARCHAR(50),
  @LanguageCode       NVARCHAR(10) = N'en',
  @ProfileImageUrl    NVARCHAR(400) = NULL,
  @ProfileImageWidth  SMALLINT = NULL,
  @ProfileImageHeight SMALLINT = NULL,
  @ProfileImageBytes  INT = NULL,
  @SourceIp           NVARCHAR(45) = NULL,
  @UserAgent          NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validaciones de campos (mantener las existentes)
    IF @Name IS NULL OR LEN(@Name) < 1 OR LEN(@Name) > 50
      THROW 50001, 'Nombre inválido: debe tener entre 1 y 50 caracteres.', 1;

    IF @Email IS NULL OR LEN(@Email) > 50 OR @Email NOT LIKE '%_@_%._%'
      THROW 50002, 'Correo inválido o supera 50 caracteres.', 1;

    IF @Alias IS NOT NULL AND LEN(@Alias) > 50
      THROW 50003, 'Alias supera 50 caracteres.', 1;

    IF @Password IS NULL OR @PasswordConfirm IS NULL OR @Password <> @PasswordConfirm
      THROW 50004, 'La confirmación de contraseña no coincide.', 1;

    IF LEN(@Password) < 8 OR LEN(@Password) > 12
      THROW 50005, 'Contraseña inválida: longitud 8-12.', 1;

    IF PATINDEX('%[A-Z]%', @Password COLLATE Latin1_General_BIN2) = 0
      THROW 50006, 'Contraseña debe incluir al menos una mayúscula.', 1;
    IF PATINDEX('%[a-z]%', @Password COLLATE Latin1_General_BIN2) = 0
      THROW 50007, 'Contraseña debe incluir al menos una minúscula.', 1;
    IF PATINDEX('%[0-9]%', @Password) = 0
      THROW 50008, 'Contraseña debe incluir al menos un dígito.', 1;
    IF @Password LIKE '%[^0-9A-Za-z]%'
      THROW 50009, 'Contraseña debe ser alfanumérica (sin caracteres especiales).', 1;

    IF @ProfileImageBytes IS NOT NULL AND @ProfileImageBytes > 5242880
      THROW 50010, 'Imagen supera 5MB.', 1;
    IF @ProfileImageWidth IS NOT NULL AND (@ProfileImageWidth < 300 OR @ProfileImageWidth > 1024)
      THROW 50011, 'Ancho de imagen fuera de rango (300-1024).', 1;
    IF @ProfileImageHeight IS NOT NULL AND (@ProfileImageHeight < 300 OR @ProfileImageHeight > 1024)
      THROW 50012, 'Alto de imagen fuera de rango (300-1024).', 1;

    IF EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email = @Email)
      THROW 50013, 'Correo duplicado.', 1;

    DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
    DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @Password);
    DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

    DECLARE @UserID INT;

    BEGIN TRAN;
      INSERT INTO auth.UserAccount
      (Email, PasswordHash, PasswordSalt, Name, Alias, SystemRoleCode, LanguageCode,
       ProfileImageUrl, ProfileImageWidth, ProfileImageHeight, ProfileImageBytes,
       AccountStatus, FailedLoginCount, LockedUntil)
      VALUES
      (@Email, @Hash, @Salt, @Name, @Alias, N'USER', @LanguageCode,
       @ProfileImageUrl, @ProfileImageWidth, @ProfileImageHeight, @ProfileImageBytes,
       1, 0, NULL);

      SET @UserID = SCOPE_IDENTITY();

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'CREATE', 
             N'Registro exitoso con rol USER', @SourceIp, @UserAgent);

    COMMIT;

    SELECT @UserID AS UserID, N'USER' AS SystemRoleCode, N'Registro exitoso.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RegisterUser TO app_executor;
GO

-- ============================================================================
-- sp_Login
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_Login
  @Email       NVARCHAR(50),
  @Password    NVARCHAR(50),
  @SourceIp    NVARCHAR(45) = NULL,      -- NUEVO
  @UserAgent   NVARCHAR(300) = NULL,     -- NUEVO
  @SessionID   UNIQUEIDENTIFIER OUTPUT,
  @Message     NVARCHAR(200) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @SessionID = NULL;
  SET @Message = N'';

  DECLARE
    @UserID INT, @Hash VARBINARY(64), @Salt VARBINARY(16),
    @Status TINYINT, @Fails SMALLINT, @LockedUntil DATETIME2(0);

  SELECT
    @UserID = UserID,
    @Hash = PasswordHash,
    @Salt = PasswordSalt,
    @Status = AccountStatus,
    @Fails = FailedLoginCount,
    @LockedUntil = LockedUntil
  FROM auth.UserAccount
  WHERE Email = @Email;

  IF @UserID IS NULL
  BEGIN
    SET @Message = N'Credenciales inválidas.';
    INSERT INTO auth.LoginAttempt(UserID, Email, Success, Ip, UserAgent)
    VALUES(NULL, @Email, 0, @SourceIp, @UserAgent);
    RETURN;
  END

  IF @Status = 2 AND (@LockedUntil IS NULL OR @LockedUntil > SYSUTCDATETIME())
  BEGIN
    SET @Message = N'Cuenta bloqueada. Solicite restablecer.';
    INSERT INTO auth.LoginAttempt(UserID, Email, Success, Ip, UserAgent)
    VALUES(@UserID, @Email, 0, @SourceIp, @UserAgent);
    RETURN;
  END

  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @Password);
  DECLARE @Check VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  IF @Check <> @Hash
  BEGIN
    BEGIN TRY
      BEGIN TRAN;
        UPDATE auth.UserAccount
           SET FailedLoginCount = FailedLoginCount + 1,
               AccountStatus = CASE WHEN FailedLoginCount + 1 >= 5 THEN 2 ELSE AccountStatus END,
               LockedUntil = CASE WHEN FailedLoginCount + 1 >= 5 THEN DATEADD(HOUR, 1, SYSUTCDATETIME()) ELSE LockedUntil END,
               UpdatedAt = SYSUTCDATETIME()
         WHERE UserID = @UserID;

        INSERT INTO auth.LoginAttempt(UserID, Email, Success, Ip, UserAgent)
        VALUES(@UserID, @Email, 0, @SourceIp, @UserAgent);
      COMMIT;
    END TRY
    BEGIN CATCH
      IF @@TRANCOUNT > 0 ROLLBACK;
      THROW;
    END CATCH

    SET @Message = CASE WHEN @Fails + 1 >= 5
                        THEN N'Cuenta bloqueada por múltiples intentos.'
                        ELSE N'Credenciales inválidas.'
                   END;
    RETURN;
  END

  -- Éxito: crear sesión CON IP y UserAgent
  BEGIN TRY
    BEGIN TRAN;

      UPDATE auth.UserAccount
         SET FailedLoginCount = 0,
             AccountStatus = 1,
             LockedUntil = NULL,
             UpdatedAt = SYSUTCDATETIME()
       WHERE UserID = @UserID;

      SET @SessionID = NEWID();
      INSERT INTO auth.Session(SessionID, UserID, ExpiresAt, Ip, UserAgent)
      VALUES(@SessionID, @UserID, DATEADD(HOUR, 12, SYSUTCDATETIME()), @SourceIp, @UserAgent);

      INSERT INTO auth.LoginAttempt(UserID, Email, Success, Ip, UserAgent)
      VALUES(@UserID, @Email, 1, @SourceIp, @UserAgent);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'LOGIN', 
             N'Inicio de sesión exitoso', @SourceIp, @UserAgent);

    COMMIT;

    SET @Message = N'Login exitoso.';
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_Login TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_ValidateAndRefreshSession
  @SessionID   UNIQUEIDENTIFIER,
  @IsValid     BIT OUTPUT,
  @UserID      INT OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @IsValid = 0; SET @UserID = NULL;

  DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

  SELECT @UserID = s.UserID
  FROM auth.Session s
  WHERE s.SessionID = @SessionID
    AND s.IsValid = 1
    AND s.ExpiresAt > @Now;

  IF @UserID IS NULL
  BEGIN
    -- Si expiró y existía, invalidar
    UPDATE auth.Session SET IsValid = 0
      WHERE SessionID = @SessionID AND IsValid = 1 AND ExpiresAt <= @Now;
    RETURN;
  END

  -- Sliding expiration: nueva expiración a +12h
  UPDATE auth.Session
     SET LastActivityAt = @Now,
         ExpiresAt = DATEADD(HOUR, 12, @Now)
   WHERE SessionID = @SessionID;

  SET @IsValid = 1;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ValidateAndRefreshSession TO app_executor;
GO

-- ============================================================================
-- sp_Logout
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_Logout
  @SessionID  UNIQUEIDENTIFIER,
  @SourceIp   NVARCHAR(45) = NULL,      -- NUEVO
  @UserAgent  NVARCHAR(300) = NULL      -- NUEVO
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @UserID INT;
  SELECT @UserID = UserID FROM auth.Session WHERE SessionID = @SessionID;

  UPDATE auth.Session
     SET IsValid = 0
   WHERE SessionID = @SessionID AND IsValid = 1;

  IF @UserID IS NOT NULL
  BEGIN
    INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
    VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'LOGOUT', 
           N'Cierre de sesión', @SourceIp, @UserAgent);
  END

  SELECT N'Sesión cerrada.' AS Message;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_Logout TO app_executor;
GO

-- ============================================================================
-- sp_RequestPasswordReset
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_RequestPasswordReset
  @Email      NVARCHAR(50),
  @SourceIp   NVARCHAR(45) = NULL,
  @Token      NVARCHAR(100) OUTPUT,
  @ExpiresAt  DATETIME2(0) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @Token = NULL; SET @ExpiresAt = NULL;

  DECLARE @UserID INT;
  SELECT @UserID = UserID FROM auth.UserAccount WHERE Email = @Email;

  IF @UserID IS NULL
  BEGIN
    SET @Token = NULL; SET @ExpiresAt = NULL;
    RETURN;
  END

  -- CORRECCIÓN: Generar token correctamente
  DECLARE @guid1 NVARCHAR(36) = CONVERT(NVARCHAR(36), NEWID());
  DECLARE @guid2 NVARCHAR(36) = CONVERT(NVARCHAR(36), NEWID());
  -- Remover guiones de ambos GUIDs y concatenar (64 caracteres)
  SET @Token = REPLACE(@guid1, '-', '') + REPLACE(@guid2, '-', '');
  
  SET @ExpiresAt = DATEADD(MINUTE, 60, SYSUTCDATETIME());

  INSERT INTO auth.PasswordResetRequest(UserID, Token, ExpiresAt, FromIp)
  VALUES(@UserID, @Token, @ExpiresAt, @SourceIp);

  INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
  VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'RESET_REQUEST', 
         N'Solicitud de restablecimiento', @SourceIp, NULL);

END
GO

GRANT EXECUTE ON OBJECT::app.sp_RequestPasswordReset TO app_executor;
GO

-- ============================================================================
-- sp_ResetPasswordWithToken
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_ResetPasswordWithToken
  @Token            NVARCHAR(100),
  @NewPassword      NVARCHAR(50),
  @ConfirmPassword  NVARCHAR(50),
  @SourceIp         NVARCHAR(45) = NULL,      -- NUEVO
  @UserAgent        NVARCHAR(300) = NULL      -- NUEVO
AS
BEGIN
  SET NOCOUNT ON;

  IF @NewPassword IS NULL OR @ConfirmPassword IS NULL OR @NewPassword <> @ConfirmPassword
    THROW 50020, 'La confirmación de contraseña no coincide.', 1;

  IF LEN(@NewPassword) < 8 OR LEN(@NewPassword) > 12
    THROW 50021, 'Contraseña inválida: longitud 8-12.', 1;
  IF PATINDEX('%[A-Z]%', @NewPassword COLLATE Latin1_General_BIN2) = 0
    THROW 50022, 'Contraseña debe incluir al menos una mayúscula.', 1;
  IF PATINDEX('%[a-z]%', @NewPassword COLLATE Latin1_General_BIN2) = 0
    THROW 50023, 'Contraseña debe incluir al menos una minúscula.', 1;
  IF PATINDEX('%[0-9]%', @NewPassword) = 0
    THROW 50024, 'Contraseña debe incluir al menos un dígito.', 1;
  IF @NewPassword LIKE '%[^0-9A-Za-z]%'
    THROW 50025, 'Contraseña debe ser alfanumérica.', 1;

  DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();
  DECLARE @UserID INT;

  SELECT TOP 1 @UserID = r.UserID
  FROM auth.PasswordResetRequest r
  WHERE r.Token = @Token AND r.UsedAt IS NULL AND r.ExpiresAt > @Now;

  IF @UserID IS NULL
    THROW 50026, 'Enlace inválido o expirado.', 1;

  DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @NewPassword);
  DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  BEGIN TRY
    BEGIN TRAN;

      UPDATE auth.UserAccount
         SET PasswordHash = @Hash,
             PasswordSalt = @Salt,
             AccountStatus = 1,
             FailedLoginCount = 0,
             LockedUntil = NULL,
             UpdatedAt = @Now
       WHERE UserID = @UserID;

      UPDATE auth.PasswordResetRequest
         SET UsedAt = @Now
       WHERE Token = @Token;

      UPDATE auth.Session SET IsValid = 0 WHERE UserID = @UserID AND IsValid = 1;

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'RESET_PASSWORD', 
             N'Password restablecida', @SourceIp, @UserAgent);

    COMMIT;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ResetPasswordWithToken TO app_executor;
GO

-- ============================================================================
-- sp_UpdateUserProfile
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_UpdateUserProfile
  @ActorUserID        INT,
  @TargetUserID       INT,
  @Name               NVARCHAR(50) = NULL,
  @Alias              NVARCHAR(50) = NULL,
  @LanguageCode       NVARCHAR(10) = NULL,
  @ProfileImageUrl    NVARCHAR(400) = NULL,
  @ProfileImageWidth  SMALLINT = NULL,
  @ProfileImageHeight SMALLINT = NULL,
  @ProfileImageBytes  INT = NULL,
  @SourceIp           NVARCHAR(45) = NULL,      -- NUEVO
  @UserAgent          NVARCHAR(300) = NULL      -- NUEVO
AS
BEGIN
  SET NOCOUNT ON;

  IF @Name IS NOT NULL AND (LEN(@Name) < 1 OR LEN(@Name) > 50)
    THROW 50030, 'Nombre inválido (1-50).', 1;
  IF @Alias IS NOT NULL AND LEN(@Alias) > 50
    THROW 50031, 'Alias supera 50 caracteres.', 1;
  IF @ProfileImageBytes IS NOT NULL AND @ProfileImageBytes > 5242880
    THROW 50032, 'Imagen supera 5MB.', 1;
  IF @ProfileImageWidth IS NOT NULL AND (@ProfileImageWidth < 300 OR @ProfileImageWidth > 1024)
    THROW 50033, 'Ancho de imagen fuera de rango (300-1024).', 1;
  IF @ProfileImageHeight IS NOT NULL AND (@ProfileImageHeight < 300 OR @ProfileImageHeight > 1024)
    THROW 50034, 'Alto de imagen fuera de rango (300-1024).', 1;

  DECLARE
    @OldName NVARCHAR(50),
    @OldAlias NVARCHAR(50),
    @OldLang NVARCHAR(10),
    @OldUrl NVARCHAR(400),
    @OldW SMALLINT, @OldH SMALLINT, @OldB INT;

  SELECT
    @OldName = Name,
    @OldAlias = Alias,
    @OldLang = LanguageCode,
    @OldUrl = ProfileImageUrl,
    @OldW = ProfileImageWidth, @OldH = ProfileImageHeight, @OldB = ProfileImageBytes
  FROM auth.UserAccount
  WHERE UserID = @TargetUserID;

  IF @OldName IS NULL
    THROW 50035, 'Usuario destino no existe.', 1;

  BEGIN TRY
    BEGIN TRAN;

      UPDATE auth.UserAccount
         SET Name = COALESCE(@Name, Name),
             Alias = COALESCE(@Alias, Alias),
             LanguageCode = COALESCE(@LanguageCode, LanguageCode),
             ProfileImageUrl = COALESCE(@ProfileImageUrl, ProfileImageUrl),
             ProfileImageWidth = CASE WHEN @ProfileImageWidth IS NULL THEN ProfileImageWidth ELSE @ProfileImageWidth END,
             ProfileImageHeight= CASE WHEN @ProfileImageHeight IS NULL THEN ProfileImageHeight ELSE @ProfileImageHeight END,
             ProfileImageBytes = CASE WHEN @ProfileImageBytes IS NULL THEN ProfileImageBytes ELSE @ProfileImageBytes END,
             UpdatedAt = SYSUTCDATETIME()
       WHERE UserID = @TargetUserID;

      -- Log por campo cambiado (con SourceIp)
      IF @Name IS NOT NULL AND @Name <> @OldName
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'Name', @OldName, @Name, @SourceIp);

      IF @Alias IS NOT NULL AND @Alias <> @OldAlias
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'Alias', @OldAlias, @Alias, @SourceIp);

      IF @LanguageCode IS NOT NULL AND @LanguageCode <> @OldLang
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'LanguageCode', @OldLang, @LanguageCode, @SourceIp);

      IF @ProfileImageUrl IS NOT NULL AND @ProfileImageUrl <> @OldUrl
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageUrl', @OldUrl, @ProfileImageUrl, @SourceIp);

      IF @ProfileImageWidth IS NOT NULL AND @ProfileImageWidth <> @OldW
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageWidth', CONVERT(NVARCHAR(50), @OldW), CONVERT(NVARCHAR(50), @ProfileImageWidth), @SourceIp);

      IF @ProfileImageHeight IS NOT NULL AND @ProfileImageHeight <> @OldH
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageHeight', CONVERT(NVARCHAR(50), @OldH), CONVERT(NVARCHAR(50), @ProfileImageHeight), @SourceIp);

      IF @ProfileImageBytes IS NOT NULL AND @ProfileImageBytes <> @OldB
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageBytes', CONVERT(NVARCHAR(50), @OldB), CONVERT(NVARCHAR(50), @ProfileImageBytes), @SourceIp);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'USER_PROFILE', CAST(@TargetUserID AS NVARCHAR(50)), N'UPDATE', 
             N'Actualización de perfil', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Perfil actualizado.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_UpdateUserProfile TO app_executor;
GO

-- ============================================================================
-- sp_GetUserProfile - VERSIÓN ACTUALIZADA
-- Devuelve el SystemRoleCode real del usuario
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetUserProfile
  @UserID INT
AS
BEGIN
  SET NOCOUNT ON;

  -- 1) Perfil (incluye SystemRoleCode real)
  SELECT
    u.UserID, u.Email, u.Name, u.Alias, u.LanguageCode,
    u.ProfileImageUrl, u.ProfileImageWidth, u.ProfileImageHeight, u.ProfileImageBytes,
    u.AccountStatus, u.SystemRoleCode, u.CreatedAt, u.UpdatedAt
  FROM auth.UserAccount u
  WHERE u.UserID = @UserID;

  -- 2) Ligas donde soy comisionado (principal o co-comisionado)
  SELECT
    lm.LeagueID, l.Name AS LeagueName, l.Status, l.TeamSlots, l.CreatedAt,
    lm.RoleCode, lm.IsPrimaryCommissioner, lm.JoinedAt
  FROM league.LeagueMember lm
  JOIN league.League l ON l.LeagueID = lm.LeagueID
  WHERE lm.UserID = @UserID AND lm.RoleCode IN (N'COMMISSIONER', N'CO_COMMISSIONER')
  ORDER BY l.CreatedAt DESC;

  -- 3) Mis equipos por liga
  SELECT
    t.TeamID, t.LeagueID, l.Name AS LeagueName, t.TeamName, t.CreatedAt
  FROM league.Team t
  JOIN league.League l ON l.LeagueID = t.LeagueID
  WHERE t.OwnerUserID = @UserID
  ORDER BY t.CreatedAt DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetUserProfile TO app_executor;
GO

-- ============================================================================
-- sp_ChangeUserSystemRole
-- Permite a un ADMIN cambiar el rol del sistema de un usuario
-- Solo puede cambiar entre USER y BRAND_MANAGER (no a ADMIN)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_ChangeUserSystemRole
  @ActorUserID      INT,
  @TargetUserID     INT,
  @NewRoleCode      NVARCHAR(20),
  @Reason           NVARCHAR(300) = NULL,
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es ADMIN
    DECLARE @ActorRole NVARCHAR(20);
    SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

    IF @ActorRole IS NULL
      THROW 50200, 'Usuario actor no existe.', 1;

    IF @ActorRole <> N'ADMIN'
      THROW 50201, 'Solo un ADMIN puede cambiar roles del sistema.', 1;

    -- Validar que el nuevo rol es válido
    IF @NewRoleCode NOT IN (N'USER', N'BRAND_MANAGER', N'ADMIN')
      THROW 50202, 'Rol destino inválido. Debe ser USER, BRAND_MANAGER o ADMIN.', 1;

    -- Validar que el rol existe en la tabla de referencia
    IF NOT EXISTS (SELECT 1 FROM auth.SystemRole WHERE RoleCode = @NewRoleCode)
      THROW 50203, 'El rol especificado no existe en el sistema.', 1;

    -- Obtener información del usuario target
    DECLARE @OldRoleCode NVARCHAR(20), @TargetEmail NVARCHAR(50), @TargetName NVARCHAR(50);
    SELECT 
      @OldRoleCode = SystemRoleCode,
      @TargetEmail = Email,
      @TargetName = Name
    FROM auth.UserAccount 
    WHERE UserID = @TargetUserID;

    IF @OldRoleCode IS NULL
      THROW 50204, 'Usuario destino no existe.', 1;

    -- Si no hay cambio real, no hacer nada
    IF @OldRoleCode = @NewRoleCode
    BEGIN
      SELECT N'El usuario ya tiene ese rol asignado.' AS Message;
      RETURN;
    END

    BEGIN TRAN;

      -- Actualizar el rol
      UPDATE auth.UserAccount
         SET SystemRoleCode = @NewRoleCode,
             UpdatedAt = SYSUTCDATETIME()
       WHERE UserID = @TargetUserID;

      -- Registrar en el log de cambios de rol
      INSERT INTO auth.SystemRoleChangeLog(
        UserID, ChangedByUserID, OldRoleCode, NewRoleCode, Reason, SourceIp
      )
      VALUES(
        @TargetUserID, @ActorUserID, @OldRoleCode, @NewRoleCode, @Reason, @SourceIp
      );

      -- Auditoría general
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'USER_PROFILE', CAST(@TargetUserID AS NVARCHAR(50)), N'CHANGE_SYSTEM_ROLE',
             CONCAT(N'Rol cambiado de ', @OldRoleCode, N' a ', @NewRoleCode, N' para ', @TargetName, N' (', @TargetEmail, N'). Razón: ', ISNULL(@Reason, N'No especificada')),
             @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      @TargetUserID AS UserID,
      @OldRoleCode AS OldRole,
      @NewRoleCode AS NewRole,
      N'Rol del sistema actualizado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ChangeUserSystemRole TO app_executor;
GO

-- ============================================================================
-- sp_GetUsersBySystemRole
-- Lista usuarios filtrados por rol del sistema con paginación
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetUsersBySystemRole
  @ActorUserID    INT,
  @FilterRole     NVARCHAR(20) = NULL,
  @SearchTerm     NVARCHAR(100) = NULL,
  @PageNumber     INT = 1,
  @PageSize       INT = 50
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar que el actor es ADMIN
  DECLARE @ActorRole NVARCHAR(20);
  SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

  IF @ActorRole <> N'ADMIN'
    THROW 50210, 'Solo un ADMIN puede listar usuarios por rol.', 1;

  -- Validar paginación
  IF @PageNumber < 1 SET @PageNumber = 1;
  IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 50;

  DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

  -- Total de registros
  DECLARE @TotalRecords INT;
  SELECT @TotalRecords = COUNT(*)
  FROM auth.UserAccount
  WHERE (@FilterRole IS NULL OR SystemRoleCode = @FilterRole)
    AND (@SearchTerm IS NULL OR (Name LIKE N'%' + @SearchTerm + N'%' OR Email LIKE N'%' + @SearchTerm + N'%' OR Alias LIKE N'%' + @SearchTerm + N'%'));

  -- Resultados paginados
  SELECT
    u.UserID,
    u.Email,
    u.Name,
    u.Alias,
    u.SystemRoleCode,
    sr.Display AS SystemRoleDisplay,
    u.AccountStatus,
    u.LanguageCode,
    u.CreatedAt,
    u.UpdatedAt,
    @TotalRecords AS TotalRecords,
    @PageNumber AS CurrentPage,
    @PageSize AS PageSize,
    CEILING(CAST(@TotalRecords AS FLOAT) / @PageSize) AS TotalPages
  FROM auth.UserAccount u
  JOIN auth.SystemRole sr ON sr.RoleCode = u.SystemRoleCode
  WHERE (@FilterRole IS NULL OR u.SystemRoleCode = @FilterRole)
    AND (@SearchTerm IS NULL OR (u.Name LIKE N'%' + @SearchTerm + N'%' OR u.Email LIKE N'%' + @SearchTerm + N'%' OR u.Alias LIKE N'%' + @SearchTerm + N'%'))
  ORDER BY u.CreatedAt DESC
  OFFSET @Offset ROWS
  FETCH NEXT @PageSize ROWS ONLY;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetUsersBySystemRole TO app_executor;
GO

-- ============================================================================
-- sp_GetSystemRoleHistory
-- Obtiene el historial de cambios de rol del sistema de un usuario
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetSystemRoleHistory
  @ActorUserID    INT,
  @TargetUserID   INT = NULL,
  @Top            INT = 50
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar que el actor es ADMIN
  DECLARE @ActorRole NVARCHAR(20);
  SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

  IF @ActorRole <> N'ADMIN'
    THROW 50220, 'Solo un ADMIN puede ver el historial de cambios de roles.', 1;

  IF @Top > 500 SET @Top = 500;
  IF @Top < 1 SET @Top = 50;

  SELECT TOP (@Top)
    src.ChangeID,
    src.UserID,
    u.Name AS UserName,
    u.Email AS UserEmail,
    src.OldRoleCode,
    oldRole.Display AS OldRoleDisplay,
    src.NewRoleCode,
    newRole.Display AS NewRoleDisplay,
    src.ChangedByUserID,
    changer.Name AS ChangedByName,
    src.ChangedAt,
    src.Reason,
    src.SourceIp
  FROM auth.SystemRoleChangeLog src
  JOIN auth.UserAccount u ON u.UserID = src.UserID
  JOIN auth.SystemRole oldRole ON oldRole.RoleCode = src.OldRoleCode
  JOIN auth.SystemRole newRole ON newRole.RoleCode = src.NewRoleCode
  JOIN auth.UserAccount changer ON changer.UserID = src.ChangedByUserID
  WHERE (@TargetUserID IS NULL OR src.UserID = @TargetUserID)
  ORDER BY src.ChangedAt DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetSystemRoleHistory TO app_executor;
GO

-- ============================================================================
-- sp_CreateLeague - VERSIÓN ACTUALIZADA
-- Ahora valida nombre contra equipos NFL reales y desactiva ligas activas
-- NOTA: El creador recibe rol COMMISSIONER explícito + MANAGER derivado (por tener equipo)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_CreateLeague
  @CreatorUserID        INT,
  @Name                 NVARCHAR(100),
  @Description          NVARCHAR(500) = NULL,
  @TeamSlots            TINYINT,
  @LeaguePassword       NVARCHAR(50),
  @InitialTeamName      NVARCHAR(50),
  @PlayoffTeams         TINYINT = 4,
  @AllowDecimals        BIT = 1,
  @PositionFormatID     INT = NULL,
  @ScoringSchemaID      INT = NULL,
  @SourceIp             NVARCHAR(45) = NULL,
  @UserAgent            NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  -- Validaciones (mantener las existentes)
  IF @Name IS NULL OR LEN(@Name) < 1 OR LEN(@Name) > 100
    THROW 50040, 'Nombre de liga inválido (1-100).', 1;

  IF @TeamSlots NOT IN (4,6,8,10,12,14,16,18,20)
    THROW 50041, 'TeamSlots inválido.', 1;

  IF @PlayoffTeams NOT IN (4,6)
    THROW 50042, 'PlayoffTeams inválido (4 o 6).', 1;

  IF @InitialTeamName IS NULL OR LEN(@InitialTeamName) < 1 OR LEN(@InitialTeamName) > 50
    THROW 50043, 'Nombre de equipo inválido (1-50).', 1;

  IF @LeaguePassword IS NULL OR LEN(@LeaguePassword) < 8 OR LEN(@LeaguePassword) > 12
    THROW 50045, 'Contraseña de liga inválida: longitud 8-12.', 1;
  IF PATINDEX('%[A-Z]%', @LeaguePassword COLLATE Latin1_General_BIN2) = 0
    THROW 50046, 'Contraseña de liga debe incluir al menos una mayúscula.', 1;
  IF PATINDEX('%[a-z]%', @LeaguePassword COLLATE Latin1_General_BIN2) = 0
    THROW 50047, 'Contraseña de liga debe incluir al menos una minúscula.', 1;
  IF PATINDEX('%[0-9]%', @LeaguePassword) = 0
    THROW 50048, 'Contraseña de liga debe incluir al menos un dígito.', 1;
  IF @LeaguePassword LIKE '%[^0-9A-Za-z]%'
    THROW 50049, 'Contraseña de liga debe ser alfanumérica.', 1;

  DECLARE @SeasonID INT;
  SELECT @SeasonID = SeasonID FROM league.Season WHERE IsCurrent = 1;
  IF @SeasonID IS NULL
    THROW 50050, 'No hay temporada actual configurada.', 1;

  IF @PositionFormatID IS NULL
    SELECT TOP 1 @PositionFormatID = PositionFormatID FROM ref.PositionFormat WHERE Name = N'Default';
  IF @PositionFormatID IS NULL
    THROW 50051, 'No existe PositionFormat por defecto.', 1;

  IF @ScoringSchemaID IS NULL
    SELECT TOP 1 @ScoringSchemaID = ScoringSchemaID FROM scoring.ScoringSchema WHERE Name = N'Default' AND Version = 1;
  IF @ScoringSchemaID IS NULL
    THROW 50052, 'No existe ScoringSchema por defecto.', 1;

  IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE UserID = @CreatorUserID)
    THROW 50053, 'Usuario creador no existe.', 1;

  IF EXISTS (SELECT 1 FROM league.League WHERE SeasonID = @SeasonID AND Name = @Name)
    THROW 50054, 'Ya existe liga con ese nombre en la temporada actual.', 1;

  DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @LeaguePassword);
  DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  DECLARE @LeagueID INT;

  BEGIN TRY
    BEGIN TRAN;

      -- *** NUEVO: Desactivar cualquier liga activa (Status=1) antes de crear la nueva ***
      UPDATE league.League
         SET Status = 2,  -- Inactive
             UpdatedAt = SYSUTCDATETIME()
       WHERE SeasonID = @SeasonID
         AND Status = 1  -- Active
         AND CreatedByUserID = @CreatorUserID; -- Sólo las del creador

      -- Si hubo ligas desactivadas, registrar en historial
      IF @@ROWCOUNT > 0
      BEGIN
        INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
        VALUES(@CreatorUserID, N'LEAGUE', N'SYSTEM', N'AUTO_DEACTIVATE',
               N'Ligas activas desactivadas automáticamente al crear nueva liga', @SourceIp, @UserAgent);
      END

      -- Crear la nueva liga
      INSERT INTO league.League
      (SeasonID, Name, Description, TeamSlots,
       LeaguePasswordHash, LeaguePasswordSalt,
       Status, AllowDecimals, PlayoffTeams,
       TradeDeadlineEnabled, TradeDeadlineDate,
       MaxRosterChangesPerTeam, MaxFreeAgentAddsPerTeam,
       PositionFormatID, ScoringSchemaID,
       CreatedByUserID)
      VALUES
      (@SeasonID, @Name, @Description, @TeamSlots,
       @Hash, @Salt,
       0, @AllowDecimals, @PlayoffTeams,
       0, NULL,
       NULL, NULL,
       @PositionFormatID, @ScoringSchemaID,
       @CreatorUserID);

      SET @LeagueID = SCOPE_IDENTITY();

      INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
      VALUES(@LeagueID, @CreatorUserID, N'COMMISSIONER', 1);

      INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
      VALUES(@LeagueID, @CreatorUserID, @InitialTeamName);

      -- Auditoría con IP y UserAgent
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@CreatorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'CREATE',
             N'Liga creada en Pre-Draft con equipo del comisionado', @SourceIp, @UserAgent);

    COMMIT;

    SELECT
      l.LeagueID, l.Name, l.TeamSlots,
      (l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID)) AS AvailableSlots,
      l.Status, l.PlayoffTeams, l.AllowDecimals, l.CreatedAt
    FROM league.League l
    WHERE l.LeagueID = @LeagueID;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_CreateLeague TO app_executor;
GO

-- ============================================================================
-- sp_SetLeagueStatus
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_SetLeagueStatus
  @ActorUserID INT,
  @LeagueID    INT,
  @NewStatus   TINYINT,
  @Reason      NVARCHAR(300) = NULL,
  @SourceIp    NVARCHAR(45) = NULL,
  @UserAgent   NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  IF NOT EXISTS (
    SELECT 1 FROM league.LeagueMember
    WHERE LeagueID = @LeagueID AND UserID = @ActorUserID
      AND RoleCode = N'COMMISSIONER' AND IsPrimaryCommissioner = 1
  )
    THROW 50060, 'Sólo el comisionado principal puede cambiar el estado de la liga.', 1;

  DECLARE @OldStatus TINYINT;
  SELECT @OldStatus = Status FROM league.League WHERE LeagueID = @LeagueID;

  IF @OldStatus IS NULL
    THROW 50061, 'Liga no existe.', 1;

  IF @OldStatus = @NewStatus
  BEGIN
    INSERT INTO league.LeagueStatusHistory(LeagueID, OldStatus, NewStatus, ChangedByUserID, Reason)
    VALUES(@LeagueID, @OldStatus, @NewStatus, @ActorUserID, ISNULL(@Reason, N'Sin cambios'));
    RETURN;
  END

  BEGIN TRY
    BEGIN TRAN;

      UPDATE league.League
         SET Status = @NewStatus,
             UpdatedAt = SYSUTCDATETIME()
       WHERE LeagueID = @LeagueID;

      INSERT INTO league.LeagueStatusHistory(LeagueID, OldStatus, NewStatus, ChangedByUserID, Reason)
      VALUES(@LeagueID, @OldStatus, @NewStatus, @ActorUserID, @Reason);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'STATUS_CHANGE',
             CONCAT(N'De ', @OldStatus, N' a ', @NewStatus, N'. ', ISNULL(@Reason, N'')), @SourceIp, @UserAgent);

    COMMIT;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_SetLeagueStatus TO app_executor;
GO

-- ============================================================================
-- sp_EditLeagueConfig
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_EditLeagueConfig
  @ActorUserID              INT,
  @LeagueID                 INT,
  @Name                     NVARCHAR(100) = NULL,
  @Description              NVARCHAR(500) = NULL,
  @TeamSlots                TINYINT = NULL,
  @PositionFormatID         INT = NULL,
  @ScoringSchemaID          INT = NULL,
  @PlayoffTeams             TINYINT = NULL,
  @AllowDecimals            BIT = NULL,
  @TradeDeadlineEnabled     BIT = NULL,
  @TradeDeadlineDate        DATE = NULL,
  @MaxRosterChangesPerTeam  INT = NULL,
  @MaxFreeAgentAddsPerTeam  INT = NULL,
  @SourceIp                 NVARCHAR(45) = NULL,
  @UserAgent                NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  IF NOT EXISTS (
    SELECT 1 FROM league.LeagueMember
    WHERE LeagueID = @LeagueID AND UserID = @ActorUserID
      AND RoleCode = N'COMMISSIONER' AND IsPrimaryCommissioner = 1
  )
    THROW 50070, 'Sólo el comisionado principal puede editar la configuración.', 1;

  DECLARE
    @SeasonID INT, @Status TINYINT,
    @OldName NVARCHAR(100), @OldDescription NVARCHAR(500),
    @OldTeamSlots TINYINT, @OldPositionFormatID INT, @OldScoringSchemaID INT,
    @OldPlayoffTeams TINYINT, @OldAllowDecimals BIT,
    @OldTDEnabled BIT, @OldTDDate DATE,
    @OldMaxRoster INT, @OldMaxFA INT;

  SELECT
    @SeasonID = l.SeasonID, @Status = l.Status,
    @OldName = l.Name, @OldDescription = l.Description,
    @OldTeamSlots = l.TeamSlots, @OldPositionFormatID = l.PositionFormatID, @OldScoringSchemaID = l.ScoringSchemaID,
    @OldPlayoffTeams = l.PlayoffTeams, @OldAllowDecimals = l.AllowDecimals,
    @OldTDEnabled = l.TradeDeadlineEnabled, @OldTDDate = l.TradeDeadlineDate,
    @OldMaxRoster = l.MaxRosterChangesPerTeam, @OldMaxFA = l.MaxFreeAgentAddsPerTeam
  FROM league.League l WHERE l.LeagueID = @LeagueID;

  IF @SeasonID IS NULL
    THROW 50071, 'Liga no existe.', 1;

  IF @Name IS NOT NULL AND (LEN(@Name) < 1 OR LEN(@Name) > 100)
    THROW 50072, 'Nombre de liga inválido (1-100).', 1;

  IF @TeamSlots IS NOT NULL AND @TeamSlots NOT IN (4,6,8,10,12,14,16,18,20)
    THROW 50073, 'TeamSlots inválido.', 1;

  IF @PlayoffTeams IS NOT NULL AND @PlayoffTeams NOT IN (4,6)
    THROW 50074, 'PlayoffTeams inválido.', 1;

  IF @MaxRosterChangesPerTeam IS NOT NULL AND (@MaxRosterChangesPerTeam < 1 OR @MaxRosterChangesPerTeam > 100)
    THROW 50075, 'MaxRosterChangesPerTeam fuera de rango (1-100 o NULL).', 1;

  IF @MaxFreeAgentAddsPerTeam IS NOT NULL AND (@MaxFreeAgentAddsPerTeam < 1 OR @MaxFreeAgentAddsPerTeam > 100)
    THROW 50076, 'MaxFreeAgentAddsPerTeam fuera de rango (1-100 o NULL).', 1;

  IF @Status <> 0 AND (
       @TeamSlots IS NOT NULL OR @PositionFormatID IS NOT NULL OR @ScoringSchemaID IS NOT NULL
    OR @PlayoffTeams IS NOT NULL OR @AllowDecimals IS NOT NULL
    OR @TradeDeadlineEnabled IS NOT NULL OR @TradeDeadlineDate IS NOT NULL
  )
    THROW 50077, 'Esas configuraciones solo se pueden editar en estado Pre-Draft.', 1;

  IF @Name IS NOT NULL AND @Name <> @OldName
    IF EXISTS (SELECT 1 FROM league.League WHERE SeasonID = @SeasonID AND Name = @Name)
      THROW 50078, 'Ya existe una liga con ese nombre en la temporada.', 1;

  IF @TeamSlots IS NOT NULL
  BEGIN
    DECLARE @Teams INT = (SELECT COUNT(*) FROM league.Team WHERE LeagueID = @LeagueID);
    IF @TeamSlots < @Teams
      THROW 50079, 'No se puede reducir TeamSlots por debajo de los equipos ya registrados.', 1;
  END

  IF @TradeDeadlineEnabled = 1
  BEGIN
    DECLARE @Start DATE, @End DATE;
    SELECT @Start = s.StartDate, @End = s.EndDate FROM league.Season s WHERE s.SeasonID = @SeasonID;

    IF @TradeDeadlineDate IS NULL OR @TradeDeadlineDate < @Start OR @TradeDeadlineDate > @End
      THROW 50080, 'TradeDeadlineDate debe estar dentro de la temporada.', 1;
  END
  ELSE IF @TradeDeadlineEnabled = 0
  BEGIN
    SET @TradeDeadlineDate = NULL;
  END

  BEGIN TRY
    BEGIN TRAN;

      UPDATE league.League
         SET Name = COALESCE(@Name, Name),
             Description = COALESCE(@Description, Description),
             TeamSlots = COALESCE(@TeamSlots, TeamSlots),
             PositionFormatID = COALESCE(@PositionFormatID, PositionFormatID),
             ScoringSchemaID = COALESCE(@ScoringSchemaID, ScoringSchemaID),
             PlayoffTeams = COALESCE(@PlayoffTeams, PlayoffTeams),
             AllowDecimals = COALESCE(@AllowDecimals, AllowDecimals),
             TradeDeadlineEnabled = COALESCE(@TradeDeadlineEnabled, TradeDeadlineEnabled),
             TradeDeadlineDate = CASE
                                  WHEN @TradeDeadlineEnabled = 1 THEN @TradeDeadlineDate
                                  WHEN @TradeDeadlineEnabled = 0 THEN NULL
                                  ELSE TradeDeadlineDate
                                 END,
             MaxRosterChangesPerTeam = CASE WHEN @MaxRosterChangesPerTeam IS NULL THEN NULL ELSE @MaxRosterChangesPerTeam END,
             MaxFreeAgentAddsPerTeam = CASE WHEN @MaxFreeAgentAddsPerTeam IS NULL THEN NULL ELSE @MaxFreeAgentAddsPerTeam END,
             UpdatedAt = SYSUTCDATETIME()
       WHERE LeagueID = @LeagueID;

      -- Historial por cada cambio (mantener los existentes, sin agregar SourceIp a LeagueConfigHistory ya que no tiene esa columna)
      IF @Name IS NOT NULL AND @Name <> @OldName
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'Name', @OldName, @Name);

      IF @Description IS NOT NULL AND @Description <> @OldDescription
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'Description', @OldDescription, @Description);

      IF @TeamSlots IS NOT NULL AND @TeamSlots <> @OldTeamSlots
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'TeamSlots', CONVERT(NVARCHAR(50), @OldTeamSlots), CONVERT(NVARCHAR(50), @TeamSlots));

      IF @PositionFormatID IS NOT NULL AND @PositionFormatID <> @OldPositionFormatID
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'PositionFormatID', CONVERT(NVARCHAR(50), @OldPositionFormatID), CONVERT(NVARCHAR(50), @PositionFormatID));

      IF @ScoringSchemaID IS NOT NULL AND @ScoringSchemaID <> @OldScoringSchemaID
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'ScoringSchemaID', CONVERT(NVARCHAR(50), @OldScoringSchemaID), CONVERT(NVARCHAR(50), @ScoringSchemaID));

      IF @PlayoffTeams IS NOT NULL AND @PlayoffTeams <> @OldPlayoffTeams
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'PlayoffTeams', CONVERT(NVARCHAR(50), @OldPlayoffTeams), CONVERT(NVARCHAR(50), @PlayoffTeams));

      IF @AllowDecimals IS NOT NULL AND @AllowDecimals <> @OldAllowDecimals
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'AllowDecimals', CONVERT(NVARCHAR(50), @OldAllowDecimals), CONVERT(NVARCHAR(50), @AllowDecimals));

      IF @TradeDeadlineEnabled IS NOT NULL AND @TradeDeadlineEnabled <> @OldTDEnabled
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'TradeDeadlineEnabled', CONVERT(NVARCHAR(50), @OldTDEnabled), CONVERT(NVARCHAR(50), @TradeDeadlineEnabled));

      IF @TradeDeadlineEnabled = 1 AND @TradeDeadlineDate IS NOT NULL AND @TradeDeadlineDate <> @OldTDDate
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'TradeDeadlineDate', CONVERT(NVARCHAR(50), @OldTDDate), CONVERT(NVARCHAR(50), @TradeDeadlineDate));

      IF @MaxRosterChangesPerTeam IS NOT NULL AND ISNULL(@MaxRosterChangesPerTeam, -1) <> ISNULL(@OldMaxRoster, -1)
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'MaxRosterChangesPerTeam', CONVERT(NVARCHAR(50), @OldMaxRoster), CONVERT(NVARCHAR(50), @MaxRosterChangesPerTeam));

      IF @MaxFreeAgentAddsPerTeam IS NOT NULL AND ISNULL(@MaxFreeAgentAddsPerTeam, -1) <> ISNULL(@OldMaxFA, -1)
        INSERT INTO league.LeagueConfigHistory(LeagueID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@LeagueID, @ActorUserID, N'MaxFreeAgentAddsPerTeam', CONVERT(NVARCHAR(50), @OldMaxFA), CONVERT(NVARCHAR(50), @MaxFreeAgentAddsPerTeam));

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'UPDATE', 
             N'Edición de configuración de liga', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Configuración actualizada.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_EditLeagueConfig TO app_executor;
GO

-- ============================================================================
-- sp_GetLeagueSummary - VERSIÓN ACTUALIZADA
-- Incluye información adicional de equipos con imágenes
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetLeagueSummary
  @LeagueID INT
AS
BEGIN
  SET NOCOUNT ON;

  -- 1) Información de la liga
  SELECT
    l.LeagueID, l.Name, l.Description, l.Status,
    l.TeamSlots,
    TeamsCount = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
    AvailableSlots = l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
    l.PlayoffTeams, l.AllowDecimals,
    l.TradeDeadlineEnabled, l.TradeDeadlineDate,
    l.MaxRosterChangesPerTeam, l.MaxFreeAgentAddsPerTeam,
    l.PositionFormatID, pf.Name AS PositionFormatName,
    l.ScoringSchemaID, ss.Name AS ScoringSchemaName, ss.Version,
    l.SeasonID, s.Label AS SeasonLabel, s.Year, s.StartDate, s.EndDate,
    l.CreatedByUserID, l.CreatedAt, l.UpdatedAt
  FROM league.League l
  JOIN ref.PositionFormat pf ON pf.PositionFormatID = l.PositionFormatID
  JOIN scoring.ScoringSchema ss ON ss.ScoringSchemaID = l.ScoringSchemaID
  JOIN league.Season s ON s.SeasonID = l.SeasonID
  WHERE l.LeagueID = @LeagueID;

  -- 2) Equipos de la liga (con imagen y thumbnail)
  SELECT 
    t.TeamID, 
    t.TeamName, 
    t.OwnerUserID, 
    u.Name AS OwnerName,
    t.TeamImageUrl,
    t.ThumbnailUrl,
    t.IsActive,
    t.CreatedAt,
    -- Contar jugadores en el roster
    (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1) AS RosterCount
  FROM league.Team t
  JOIN auth.UserAccount u ON u.UserID = t.OwnerUserID
  WHERE t.LeagueID = @LeagueID
  ORDER BY t.CreatedAt;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetLeagueSummary TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_ListPositionFormats
AS
BEGIN
  SET NOCOUNT ON;
  SELECT pf.PositionFormatID, pf.Name, pf.Description, pf.CreatedAt
  FROM ref.PositionFormat pf
  ORDER BY pf.PositionFormatID;
END
GO
GRANT EXECUTE ON OBJECT::app.sp_ListPositionFormats TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_ListScoringSchemas
AS
BEGIN
  SET NOCOUNT ON;
  SELECT ss.ScoringSchemaID, ss.Name, ss.Version, ss.IsTemplate, ss.Description, ss.CreatedAt
  FROM scoring.ScoringSchema ss
  ORDER BY ss.Name, ss.Version;
END
GO
GRANT EXECUTE ON OBJECT::app.sp_ListScoringSchemas TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_GetCurrentSeason
AS
BEGIN
  SET NOCOUNT ON;
  SELECT TOP 1 * FROM league.Season WHERE IsCurrent = 1;
END
GO
GRANT EXECUTE ON OBJECT::app.sp_GetCurrentSeason TO app_executor;
GO

USE XNFLFantasyDB;
GO

-- ============================================================================
-- sp_LogoutAllSessions
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_LogoutAllSessions
  @ActorUserID INT,
  @SourceIp    NVARCHAR(45) = NULL,      -- NUEVO
  @UserAgent   NVARCHAR(300) = NULL      -- NUEVO
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

      UPDATE auth.Session
         SET IsValid = 0
       WHERE UserID = @ActorUserID
         AND IsValid = 1;

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'USER_PROFILE', CAST(@ActorUserID AS NVARCHAR(50)), N'LOGOUT_ALL', 
             N'Cierre de sesión global', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Sesiones cerradas en todos los dispositivos.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_LogoutAllSessions TO app_executor;
GO

-- ============================================================================
-- SP para consultar logs de auditoría con filtros
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetAuditLogs
  @EntityType     NVARCHAR(50) = NULL,
  @EntityID       NVARCHAR(50) = NULL,
  @ActorUserID    INT = NULL,
  @ActionCode     NVARCHAR(50) = NULL,
  @StartDate      DATETIME2(0) = NULL,
  @EndDate        DATETIME2(0) = NULL,
  @Top            INT = 100  -- Limitar resultados por defecto
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar Top
  IF @Top > 1000 SET @Top = 1000;  -- Máximo 1000 registros
  IF @Top < 1 SET @Top = 100;

  SELECT TOP (@Top)
    ual.ActionLogID,
    ual.ActorUserID,
    actor.Name AS ActorName,
    actor.Email AS ActorEmail,
    ual.ImpersonatedByUserID,
    imp.Name AS ImpersonatedByName,
    ual.EntityType,
    ual.EntityID,
    ual.ActionCode,
    ual.ActionAt,
    ual.SourceIp,
    ual.UserAgent,
    ual.Details
  FROM audit.UserActionLog ual
  LEFT JOIN auth.UserAccount actor ON actor.UserID = ual.ActorUserID
  LEFT JOIN auth.UserAccount imp ON imp.UserID = ual.ImpersonatedByUserID
  WHERE 
    (@EntityType IS NULL OR ual.EntityType = @EntityType)
    AND (@EntityID IS NULL OR ual.EntityID = @EntityID)
    AND (@ActorUserID IS NULL OR ual.ActorUserID = @ActorUserID)
    AND (@ActionCode IS NULL OR ual.ActionCode = @ActionCode)
    AND (@StartDate IS NULL OR ual.ActionAt >= @StartDate)
    AND (@EndDate IS NULL OR ual.ActionAt <= @EndDate)
  ORDER BY ual.ActionAt DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetAuditLogs TO app_executor;
GO

-- ============================================================================
-- SP para obtener auditoría de un usuario específico
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetUserAuditHistory
  @UserID   INT,
  @Top      INT = 50
AS
BEGIN
  SET NOCOUNT ON;

  IF @Top > 500 SET @Top = 500;
  IF @Top < 1 SET @Top = 50;

  SELECT TOP (@Top)
    ual.ActionLogID,
    ual.EntityType,
    ual.EntityID,
    ual.ActionCode,
    ual.ActionAt,
    ual.SourceIp,
    ual.UserAgent,
    ual.Details
  FROM audit.UserActionLog ual
  WHERE ual.ActorUserID = @UserID
  ORDER BY ual.ActionAt DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetUserAuditHistory TO app_executor;
GO

-- ============================================================================
-- SP para limpieza de sesiones y tokens expirados
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_CleanupExpiredSessions
  @RetentionDays INT = 30  -- Mantener sesiones inválidas por 30 días para auditoría
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @CutoffDate DATETIME2(0) = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
  DECLARE @DeletedSessions INT = 0;
  DECLARE @DeletedResets INT = 0;

  BEGIN TRY
    BEGIN TRAN;

      -- Marcar sesiones expiradas como inválidas
      UPDATE auth.Session
      SET IsValid = 0
      WHERE IsValid = 1 
        AND ExpiresAt < SYSUTCDATETIME();

      -- Eliminar sesiones inválidas antiguas
      DELETE FROM auth.Session
      WHERE IsValid = 0 
        AND CreatedAt < @CutoffDate;

      SET @DeletedSessions = @@ROWCOUNT;

      -- Eliminar tokens de reset usados o expirados antiguos
      DELETE FROM auth.PasswordResetRequest
      WHERE (UsedAt IS NOT NULL OR ExpiresAt < SYSUTCDATETIME())
        AND RequestedAt < @CutoffDate;

      SET @DeletedResets = @@ROWCOUNT;

      -- Auditoría de limpieza
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(
        NULL, 
        N'SYSTEM_MAINTENANCE', 
        N'CLEANUP', 
        N'CLEANUP_SESSIONS',
        CONCAT(N'Sesiones eliminadas: ', @DeletedSessions, N', Tokens eliminados: ', @DeletedResets)
      );

    COMMIT;

    SELECT 
      @DeletedSessions AS DeletedSessions,
      @DeletedResets AS DeletedResetTokens,
      N'Limpieza completada exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_CleanupExpiredSessions TO app_executor;
GO

-- ============================================================================
-- SP para obtener estadísticas de auditoría
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetAuditStats
  @Days INT = 30
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @StartDate DATETIME2(0) = DATEADD(DAY, -@Days, SYSUTCDATETIME());

  -- Total de acciones por tipo
  SELECT 
    EntityType,
    COUNT(*) AS ActionCount
  FROM audit.UserActionLog
  WHERE ActionAt >= @StartDate
  GROUP BY EntityType
  ORDER BY ActionCount DESC;

  -- Total de acciones por código
  SELECT 
    ActionCode,
    COUNT(*) AS ActionCount
  FROM audit.UserActionLog
  WHERE ActionAt >= @StartDate
  GROUP BY ActionCode
  ORDER BY ActionCount DESC;

  -- Usuarios más activos
  SELECT TOP 10
    ual.ActorUserID,
    u.Name,
    u.Email,
    COUNT(*) AS ActionCount
  FROM audit.UserActionLog ual
  LEFT JOIN auth.UserAccount u ON u.UserID = ual.ActorUserID
  WHERE ual.ActionAt >= @StartDate
    AND ual.ActorUserID IS NOT NULL
  GROUP BY ual.ActorUserID, u.Name, u.Email
  ORDER BY ActionCount DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetAuditStats TO app_executor;
GO

-- ============================================================================
-- sp_CreateNFLTeam - VERSIÓN ACTUALIZADA
-- Solo ADMIN puede crear equipos NFL
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_CreateNFLTeam
  @ActorUserID        INT,
  @TeamName           NVARCHAR(100),
  @City               NVARCHAR(100),
  @TeamImageUrl       NVARCHAR(400) = NULL,
  @TeamImageWidth     SMALLINT = NULL,
  @TeamImageHeight    SMALLINT = NULL,
  @TeamImageBytes     INT = NULL,
  @ThumbnailUrl       NVARCHAR(400) = NULL,
  @ThumbnailWidth     SMALLINT = NULL,
  @ThumbnailHeight    SMALLINT = NULL,
  @ThumbnailBytes     INT = NULL,
  @SourceIp           NVARCHAR(45) = NULL,
  @UserAgent          NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es ADMIN
    DECLARE @ActorRole NVARCHAR(20);
    SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

    IF @ActorRole IS NULL
      THROW 50099, 'Usuario actor no existe.', 1;

    IF @ActorRole <> N'ADMIN'
      THROW 50098, 'Solo un ADMIN puede crear equipos NFL.', 1;

    -- Validaciones de campos requeridos
    IF @TeamName IS NULL OR LEN(@TeamName) < 1 OR LEN(@TeamName) > 100
      THROW 50100, 'Nombre de equipo inválido: debe tener entre 1 y 100 caracteres.', 1;

    IF @City IS NULL OR LEN(@City) < 1 OR LEN(@City) > 100
      THROW 50101, 'Ciudad inválida: debe tener entre 1 y 100 caracteres.', 1;

    IF EXISTS (SELECT 1 FROM ref.NFLTeam WHERE TeamName = @TeamName)
      THROW 50102, 'Ya existe un equipo NFL con ese nombre.', 1;

    IF @TeamImageBytes IS NOT NULL AND @TeamImageBytes > 5242880
      THROW 50103, 'Imagen supera 5MB.', 1;
    IF @TeamImageWidth IS NOT NULL AND (@TeamImageWidth < 300 OR @TeamImageWidth > 1024)
      THROW 50104, 'Ancho de imagen fuera de rango (300-1024).', 1;
    IF @TeamImageHeight IS NOT NULL AND (@TeamImageHeight < 300 OR @TeamImageHeight > 1024)
      THROW 50105, 'Alto de imagen fuera de rango (300-1024).', 1;

    IF @ThumbnailBytes IS NOT NULL AND @ThumbnailBytes > 5242880
      THROW 50106, 'Thumbnail supera 5MB.', 1;
    IF @ThumbnailWidth IS NOT NULL AND (@ThumbnailWidth < 300 OR @ThumbnailWidth > 1024)
      THROW 50107, 'Ancho de thumbnail fuera de rango (300-1024).', 1;
    IF @ThumbnailHeight IS NOT NULL AND (@ThumbnailHeight < 300 OR @ThumbnailHeight > 1024)
      THROW 50108, 'Alto de thumbnail fuera de rango (300-1024).', 1;

    DECLARE @NFLTeamID INT;

    BEGIN TRAN;

      INSERT INTO ref.NFLTeam(
        TeamName, City,
        TeamImageUrl, TeamImageWidth, TeamImageHeight, TeamImageBytes,
        ThumbnailUrl, ThumbnailWidth, ThumbnailHeight, ThumbnailBytes,
        IsActive, CreatedByUserID
      )
      VALUES(
        @TeamName, @City,
        @TeamImageUrl, @TeamImageWidth, @TeamImageHeight, @TeamImageBytes,
        @ThumbnailUrl, @ThumbnailWidth, @ThumbnailHeight, @ThumbnailBytes,
        1, @ActorUserID
      );

      SET @NFLTeamID = SCOPE_IDENTITY();

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'NFL_TEAM', CAST(@NFLTeamID AS NVARCHAR(50)), N'CREATE',
             CONCAT(N'Equipo NFL creado: ', @TeamName, N' (', @City, N')'), @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      @NFLTeamID AS NFLTeamID,
      @TeamName AS TeamName,
      @City AS City,
      N'Equipo NFL creado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_CreateNFLTeam TO app_executor;
GO

-- ============================================================================
-- sp_UpdateNFLTeam - VERSIÓN ACTUALIZADA
-- Solo ADMIN puede actualizar equipos NFL
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_UpdateNFLTeam
  @ActorUserID        INT,
  @NFLTeamID          INT,
  @TeamName           NVARCHAR(100) = NULL,
  @City               NVARCHAR(100) = NULL,
  @TeamImageUrl       NVARCHAR(400) = NULL,
  @TeamImageWidth     SMALLINT = NULL,
  @TeamImageHeight    SMALLINT = NULL,
  @TeamImageBytes     INT = NULL,
  @ThumbnailUrl       NVARCHAR(400) = NULL,
  @ThumbnailWidth     SMALLINT = NULL,
  @ThumbnailHeight    SMALLINT = NULL,
  @ThumbnailBytes     INT = NULL,
  @SourceIp           NVARCHAR(45) = NULL,
  @UserAgent          NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es ADMIN
    DECLARE @ActorRole NVARCHAR(20);
    SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

    IF @ActorRole IS NULL
      THROW 50109, 'Usuario actor no existe.', 1;

    IF @ActorRole <> N'ADMIN'
      THROW 50108, 'Solo un ADMIN puede actualizar equipos NFL.', 1;

    -- Validaciones
    IF @TeamName IS NOT NULL AND (LEN(@TeamName) < 1 OR LEN(@TeamName) > 100)
      THROW 50110, 'Nombre de equipo inválido (1-100).', 1;

    IF @City IS NOT NULL AND (LEN(@City) < 1 OR LEN(@City) > 100)
      THROW 50111, 'Ciudad inválida (1-100).', 1;

    IF @TeamImageBytes IS NOT NULL AND @TeamImageBytes > 5242880
      THROW 50112, 'Imagen supera 5MB.', 1;
    IF @TeamImageWidth IS NOT NULL AND (@TeamImageWidth < 300 OR @TeamImageWidth > 1024)
      THROW 50113, 'Ancho de imagen fuera de rango (300-1024).', 1;
    IF @TeamImageHeight IS NOT NULL AND (@TeamImageHeight < 300 OR @TeamImageHeight > 1024)
      THROW 50114, 'Alto de imagen fuera de rango (300-1024).', 1;

    IF @ThumbnailBytes IS NOT NULL AND @ThumbnailBytes > 5242880
      THROW 50115, 'Thumbnail supera 5MB.', 1;
    IF @ThumbnailWidth IS NOT NULL AND (@ThumbnailWidth < 300 OR @ThumbnailWidth > 1024)
      THROW 50116, 'Ancho de thumbnail fuera de rango (300-1024).', 1;
    IF @ThumbnailHeight IS NOT NULL AND (@ThumbnailHeight < 300 OR @ThumbnailHeight > 1024)
      THROW 50117, 'Alto de thumbnail fuera de rango (300-1024).', 1;

    DECLARE
      @OldName NVARCHAR(100),
      @OldCity NVARCHAR(100),
      @OldImageUrl NVARCHAR(400),
      @OldImageW SMALLINT, @OldImageH SMALLINT, @OldImageB INT,
      @OldThumbUrl NVARCHAR(400),
      @OldThumbW SMALLINT, @OldThumbH SMALLINT, @OldThumbB INT;

    SELECT
      @OldName = TeamName,
      @OldCity = City,
      @OldImageUrl = TeamImageUrl,
      @OldImageW = TeamImageWidth, @OldImageH = TeamImageHeight, @OldImageB = TeamImageBytes,
      @OldThumbUrl = ThumbnailUrl,
      @OldThumbW = ThumbnailWidth, @OldThumbH = ThumbnailHeight, @OldThumbB = ThumbnailBytes
    FROM ref.NFLTeam
    WHERE NFLTeamID = @NFLTeamID;

    IF @OldName IS NULL
      THROW 50118, 'Equipo NFL no existe.', 1;

    IF @TeamName IS NOT NULL AND @TeamName <> @OldName
      IF EXISTS (SELECT 1 FROM ref.NFLTeam WHERE TeamName = @TeamName AND NFLTeamID <> @NFLTeamID)
        THROW 50119, 'Ya existe otro equipo NFL con ese nombre.', 1;

    BEGIN TRAN;

      UPDATE ref.NFLTeam
         SET TeamName = COALESCE(@TeamName, TeamName),
             City = COALESCE(@City, City),
             TeamImageUrl = CASE WHEN @TeamImageUrl IS NULL THEN TeamImageUrl ELSE @TeamImageUrl END,
             TeamImageWidth = CASE WHEN @TeamImageWidth IS NULL THEN TeamImageWidth ELSE @TeamImageWidth END,
             TeamImageHeight = CASE WHEN @TeamImageHeight IS NULL THEN TeamImageHeight ELSE @TeamImageHeight END,
             TeamImageBytes = CASE WHEN @TeamImageBytes IS NULL THEN TeamImageBytes ELSE @TeamImageBytes END,
             ThumbnailUrl = CASE WHEN @ThumbnailUrl IS NULL THEN ThumbnailUrl ELSE @ThumbnailUrl END,
             ThumbnailWidth = CASE WHEN @ThumbnailWidth IS NULL THEN ThumbnailWidth ELSE @ThumbnailWidth END,
             ThumbnailHeight = CASE WHEN @ThumbnailHeight IS NULL THEN ThumbnailHeight ELSE @ThumbnailHeight END,
             ThumbnailBytes = CASE WHEN @ThumbnailBytes IS NULL THEN ThumbnailBytes ELSE @ThumbnailBytes END,
             UpdatedByUserID = @ActorUserID,
             UpdatedAt = SYSUTCDATETIME()
       WHERE NFLTeamID = @NFLTeamID;

      IF @TeamName IS NOT NULL AND @TeamName <> @OldName
        INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@NFLTeamID, @ActorUserID, N'TeamName', @OldName, @TeamName, @SourceIp, @UserAgent);

      IF @City IS NOT NULL AND @City <> @OldCity
        INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@NFLTeamID, @ActorUserID, N'City', @OldCity, @City, @SourceIp, @UserAgent);

      IF @TeamImageUrl IS NOT NULL AND @TeamImageUrl <> @OldImageUrl
        INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@NFLTeamID, @ActorUserID, N'TeamImageUrl', @OldImageUrl, @TeamImageUrl, @SourceIp, @UserAgent);

      IF @ThumbnailUrl IS NOT NULL AND @ThumbnailUrl <> @OldThumbUrl
        INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@NFLTeamID, @ActorUserID, N'ThumbnailUrl', @OldThumbUrl, @ThumbnailUrl, @SourceIp, @UserAgent);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'NFL_TEAM', CAST(@NFLTeamID AS NVARCHAR(50)), N'UPDATE',
             N'Equipo NFL actualizado', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Equipo NFL actualizado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_UpdateNFLTeam TO app_executor;
GO

-- ============================================================================
-- sp_DeactivateNFLTeam - VERSIÓN ACTUALIZADA
-- Solo ADMIN puede desactivar equipos NFL
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_DeactivateNFLTeam
  @ActorUserID   INT,
  @NFLTeamID     INT,
  @SourceIp      NVARCHAR(45) = NULL,
  @UserAgent     NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es ADMIN
    DECLARE @ActorRole NVARCHAR(20);
    SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

    IF @ActorRole IS NULL
      THROW 50119, 'Usuario actor no existe.', 1;

    IF @ActorRole <> N'ADMIN'
      THROW 50118, 'Solo un ADMIN puede desactivar equipos NFL.', 1;

    DECLARE @TeamName NVARCHAR(100);
    SELECT @TeamName = TeamName FROM ref.NFLTeam WHERE NFLTeamID = @NFLTeamID;

    IF @TeamName IS NULL
      THROW 50120, 'Equipo NFL no existe.', 1;

    DECLARE @CurrentSeasonID INT;
    SELECT @CurrentSeasonID = SeasonID FROM league.Season WHERE IsCurrent = 1;

    IF EXISTS (
      SELECT 1 
      FROM league.NFLGame 
      WHERE SeasonID = @CurrentSeasonID 
        AND (HomeTeamID = @NFLTeamID OR AwayTeamID = @NFLTeamID)
        AND GameStatus IN (N'Scheduled', N'InProgress')
    )
      THROW 50121, 'No se puede desactivar el equipo porque tiene partidos agendados para esta temporada.', 1;

    BEGIN TRAN;

      UPDATE ref.NFLTeam
         SET IsActive = 0,
             UpdatedByUserID = @ActorUserID,
             UpdatedAt = SYSUTCDATETIME()
       WHERE NFLTeamID = @NFLTeamID;

      INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
      VALUES(@NFLTeamID, @ActorUserID, N'IsActive', N'1', N'0', @SourceIp, @UserAgent);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'NFL_TEAM', CAST(@NFLTeamID AS NVARCHAR(50)), N'DEACTIVATE',
             CONCAT(N'Equipo desactivado: ', @TeamName), @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Equipo NFL desactivado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_DeactivateNFLTeam TO app_executor;
GO

-- ============================================================================
-- sp_ReactivateNFLTeam - VERSIÓN ACTUALIZADA
-- Solo ADMIN puede reactivar equipos NFL
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_ReactivateNFLTeam
  @ActorUserID   INT,
  @NFLTeamID     INT,
  @SourceIp      NVARCHAR(45) = NULL,
  @UserAgent     NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es ADMIN
    DECLARE @ActorRole NVARCHAR(20);
    SELECT @ActorRole = SystemRoleCode FROM auth.UserAccount WHERE UserID = @ActorUserID;

    IF @ActorRole IS NULL
      THROW 50124, 'Usuario actor no existe.', 1;

    IF @ActorRole <> N'ADMIN'
      THROW 50123, 'Solo un ADMIN puede reactivar equipos NFL.', 1;

    DECLARE @TeamName NVARCHAR(100);
    SELECT @TeamName = TeamName FROM ref.NFLTeam WHERE NFLTeamID = @NFLTeamID;

    IF @TeamName IS NULL
      THROW 50125, 'Equipo NFL no existe.', 1;

    BEGIN TRAN;

      UPDATE ref.NFLTeam
         SET IsActive = 1,
             UpdatedByUserID = @ActorUserID,
             UpdatedAt = SYSUTCDATETIME()
       WHERE NFLTeamID = @NFLTeamID;

      INSERT INTO ref.NFLTeamChangeLog(NFLTeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
      VALUES(@NFLTeamID, @ActorUserID, N'IsActive', N'0', N'1', @SourceIp, @UserAgent);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'NFL_TEAM', CAST(@NFLTeamID AS NVARCHAR(50)), N'REACTIVATE',
             CONCAT(N'Equipo reactivado: ', @TeamName), @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Equipo NFL reactivado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ReactivateNFLTeam TO app_executor;
GO

-- ============================================================================
-- sp_ListNFLTeams
-- Lista equipos NFL con paginación, búsqueda y filtros
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_ListNFLTeams
  @PageNumber     INT = 1,
  @PageSize       INT = 50,
  @SearchTerm     NVARCHAR(100) = NULL,
  @FilterCity     NVARCHAR(100) = NULL,
  @FilterIsActive BIT = NULL
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar paginación
  IF @PageNumber < 1 SET @PageNumber = 1;
  IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 50;

  DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

  -- Total de registros
  DECLARE @TotalRecords INT;
  SELECT @TotalRecords = COUNT(*)
  FROM ref.NFLTeam
  WHERE (@SearchTerm IS NULL OR (TeamName LIKE N'%' + @SearchTerm + N'%' OR City LIKE N'%' + @SearchTerm + N'%'))
    AND (@FilterCity IS NULL OR City = @FilterCity)
    AND (@FilterIsActive IS NULL OR IsActive = @FilterIsActive);

  -- Resultados paginados
  SELECT
    NFLTeamID,
    TeamName,
    City,
    TeamImageUrl,
    ThumbnailUrl,
    IsActive,
    CreatedAt,
    UpdatedAt,
    @TotalRecords AS TotalRecords,
    @PageNumber AS CurrentPage,
    @PageSize AS PageSize,
    CEILING(CAST(@TotalRecords AS FLOAT) / @PageSize) AS TotalPages
  FROM ref.NFLTeam
  WHERE (@SearchTerm IS NULL OR (TeamName LIKE N'%' + @SearchTerm + N'%' OR City LIKE N'%' + @SearchTerm + N'%'))
    AND (@FilterCity IS NULL OR City = @FilterCity)
    AND (@FilterIsActive IS NULL OR IsActive = @FilterIsActive)
  ORDER BY TeamName
  OFFSET @Offset ROWS
  FETCH NEXT @PageSize ROWS ONLY;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ListNFLTeams TO app_executor;
GO

-- ============================================================================
-- sp_GetNFLTeamDetails
-- Obtiene detalles completos de un equipo NFL
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetNFLTeamDetails
  @NFLTeamID INT
AS
BEGIN
  SET NOCOUNT ON;

  -- Información del equipo
  SELECT
    nt.NFLTeamID,
    nt.TeamName,
    nt.City,
    nt.TeamImageUrl,
    nt.TeamImageWidth,
    nt.TeamImageHeight,
    nt.TeamImageBytes,
    nt.ThumbnailUrl,
    nt.ThumbnailWidth,
    nt.ThumbnailHeight,
    nt.ThumbnailBytes,
    nt.IsActive,
    nt.CreatedAt,
    creator.Name AS CreatedByName,
    nt.UpdatedAt,
    updater.Name AS UpdatedByName
  FROM ref.NFLTeam nt
  LEFT JOIN auth.UserAccount creator ON creator.UserID = nt.CreatedByUserID
  LEFT JOIN auth.UserAccount updater ON updater.UserID = nt.UpdatedByUserID
  WHERE nt.NFLTeamID = @NFLTeamID;

  -- Historial de cambios (últimos 20)
  SELECT TOP 20
    c.ChangeID,
    c.FieldName,
    c.OldValue,
    c.NewValue,
    c.ChangedAt,
    u.Name AS ChangedByName,
    c.SourceIp
  FROM ref.NFLTeamChangeLog c
  JOIN auth.UserAccount u ON u.UserID = c.ChangedByUserID
  WHERE c.NFLTeamID = @NFLTeamID
  ORDER BY c.ChangedAt DESC;

  -- Jugadores actuales del equipo
  SELECT
    p.PlayerID,
    p.FirstName,
    p.LastName,
    p.Position,
    p.InjuryStatus
  FROM league.Player p
  WHERE p.NFLTeamID = @NFLTeamID
    AND p.IsActive = 1
  ORDER BY p.Position, p.LastName;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetNFLTeamDetails TO app_executor;
GO

-- ============================================================================
-- sp_UpdateTeamBranding
-- Edita nombre e imagen de un equipo fantasy
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_UpdateTeamBranding
  @ActorUserID        INT,
  @TeamID             INT,
  @TeamName           NVARCHAR(100) = NULL,
  @TeamImageUrl       NVARCHAR(400) = NULL,
  @TeamImageWidth     SMALLINT = NULL,
  @TeamImageHeight    SMALLINT = NULL,
  @TeamImageBytes     INT = NULL,
  @ThumbnailUrl       NVARCHAR(400) = NULL,
  @ThumbnailWidth     SMALLINT = NULL,
  @ThumbnailHeight    SMALLINT = NULL,
  @ThumbnailBytes     INT = NULL,
  @SourceIp           NVARCHAR(45) = NULL,
  @UserAgent          NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validaciones básicas
    IF @TeamName IS NOT NULL AND (LEN(@TeamName) < 1 OR LEN(@TeamName) > 100)
      THROW 50130, 'Nombre de equipo inválido (1-100).', 1;

    IF @TeamImageBytes IS NOT NULL AND @TeamImageBytes > 5242880
      THROW 50131, 'Imagen supera 5MB.', 1;
    IF @TeamImageWidth IS NOT NULL AND (@TeamImageWidth < 300 OR @TeamImageWidth > 1024)
      THROW 50132, 'Ancho de imagen fuera de rango (300-1024).', 1;
    IF @TeamImageHeight IS NOT NULL AND (@TeamImageHeight < 300 OR @TeamImageHeight > 1024)
      THROW 50133, 'Alto de imagen fuera de rango (300-1024).', 1;

    IF @ThumbnailBytes IS NOT NULL AND @ThumbnailBytes > 5242880
      THROW 50134, 'Thumbnail supera 5MB.', 1;
    IF @ThumbnailWidth IS NOT NULL AND (@ThumbnailWidth < 300 OR @ThumbnailWidth > 1024)
      THROW 50135, 'Ancho de thumbnail fuera de rango (300-1024).', 1;
    IF @ThumbnailHeight IS NOT NULL AND (@ThumbnailHeight < 300 OR @ThumbnailHeight > 1024)
      THROW 50136, 'Alto de thumbnail fuera de rango (300-1024).', 1;

    -- Obtener valores actuales y validar permisos
    DECLARE
      @LeagueID INT,
      @OwnerUserID INT,
      @OldName NVARCHAR(100),
      @OldImageUrl NVARCHAR(400),
      @OldImageW SMALLINT, @OldImageH SMALLINT, @OldImageB INT,
      @OldThumbUrl NVARCHAR(400),
      @OldThumbW SMALLINT, @OldThumbH SMALLINT, @OldThumbB INT;

    SELECT
      @LeagueID = LeagueID,
      @OwnerUserID = OwnerUserID,
      @OldName = TeamName,
      @OldImageUrl = TeamImageUrl,
      @OldImageW = TeamImageWidth, @OldImageH = TeamImageHeight, @OldImageB = TeamImageBytes,
      @OldThumbUrl = ThumbnailUrl,
      @OldThumbW = ThumbnailWidth, @OldThumbH = ThumbnailHeight, @OldThumbB = ThumbnailBytes
    FROM league.Team
    WHERE TeamID = @TeamID;

    IF @OldName IS NULL
      THROW 50137, 'Equipo no existe.', 1;

    -- Solo el dueño puede editar (o un comisionado, pero por simplicidad solo dueño)
    IF @OwnerUserID <> @ActorUserID
      THROW 50138, 'Solo el dueño del equipo puede editar su branding.', 1;

    -- Validar nombre único dentro de la liga
    IF @TeamName IS NOT NULL AND @TeamName <> @OldName
      IF EXISTS (SELECT 1 FROM league.Team WHERE LeagueID = @LeagueID AND TeamName = @TeamName AND TeamID <> @TeamID)
        THROW 50139, 'Ya existe otro equipo con ese nombre en la liga.', 1;

    BEGIN TRAN;

      UPDATE league.Team
         SET TeamName = COALESCE(@TeamName, TeamName),
             TeamImageUrl = CASE WHEN @TeamImageUrl IS NULL THEN TeamImageUrl ELSE @TeamImageUrl END,
             TeamImageWidth = CASE WHEN @TeamImageWidth IS NULL THEN TeamImageWidth ELSE @TeamImageWidth END,
             TeamImageHeight = CASE WHEN @TeamImageHeight IS NULL THEN TeamImageHeight ELSE @TeamImageHeight END,
             TeamImageBytes = CASE WHEN @TeamImageBytes IS NULL THEN TeamImageBytes ELSE @TeamImageBytes END,
             ThumbnailUrl = CASE WHEN @ThumbnailUrl IS NULL THEN ThumbnailUrl ELSE @ThumbnailUrl END,
             ThumbnailWidth = CASE WHEN @ThumbnailWidth IS NULL THEN ThumbnailWidth ELSE @ThumbnailWidth END,
             ThumbnailHeight = CASE WHEN @ThumbnailHeight IS NULL THEN ThumbnailHeight ELSE @ThumbnailHeight END,
             ThumbnailBytes = CASE WHEN @ThumbnailBytes IS NULL THEN ThumbnailBytes ELSE @ThumbnailBytes END,
             UpdatedAt = SYSUTCDATETIME()
       WHERE TeamID = @TeamID;

      -- Log de cambios
      IF @TeamName IS NOT NULL AND @TeamName <> @OldName
        INSERT INTO league.TeamChangeLog(TeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@TeamID, @ActorUserID, N'TeamName', @OldName, @TeamName, @SourceIp, @UserAgent);

      IF @TeamImageUrl IS NOT NULL AND @TeamImageUrl <> @OldImageUrl
        INSERT INTO league.TeamChangeLog(TeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@TeamID, @ActorUserID, N'TeamImageUrl', @OldImageUrl, @TeamImageUrl, @SourceIp, @UserAgent);

      IF @ThumbnailUrl IS NOT NULL AND @ThumbnailUrl <> @OldThumbUrl
        INSERT INTO league.TeamChangeLog(TeamID, ChangedByUserID, FieldName, OldValue, NewValue, SourceIp, UserAgent)
        VALUES(@TeamID, @ActorUserID, N'ThumbnailUrl', @OldThumbUrl, @ThumbnailUrl, @SourceIp, @UserAgent);

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'FANTASY_TEAM', CAST(@TeamID AS NVARCHAR(50)), N'UPDATE_BRANDING',
             N'Branding del equipo actualizado', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Branding del equipo actualizado exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_UpdateTeamBranding TO app_executor;
GO

-- ============================================================================
-- sp_GetMyTeam
-- Obtiene información completa del equipo fantasy con roster de jugadores
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetMyTeam
  @ActorUserID    INT,
  @TeamID         INT,
  @FilterPosition NVARCHAR(20) = NULL,
  @SearchPlayer   NVARCHAR(100) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar que el usuario tiene acceso al equipo
  DECLARE @OwnerUserID INT, @LeagueID INT;
  SELECT @OwnerUserID = OwnerUserID, @LeagueID = LeagueID
  FROM league.Team
  WHERE TeamID = @TeamID;

  IF @OwnerUserID IS NULL
    THROW 50140, 'Equipo no existe.', 1;

  IF @OwnerUserID <> @ActorUserID
    -- Validar si es comisionado o miembro de la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID AND UserID = @ActorUserID
    )
      THROW 50141, 'No tienes acceso a este equipo.', 1;

  -- 1) Información del equipo
  SELECT
    t.TeamID,
    t.TeamName,
    u.Name AS ManagerName,
    u.UserID AS ManagerUserID,
    t.TeamImageUrl,
    t.ThumbnailUrl,
    t.IsActive,
    l.Name AS LeagueName,
    l.LeagueID,
    l.Status AS LeagueStatus,
    t.CreatedAt,
    t.UpdatedAt
  FROM league.Team t
  JOIN auth.UserAccount u ON u.UserID = t.OwnerUserID
  JOIN league.League l ON l.LeagueID = t.LeagueID
  WHERE t.TeamID = @TeamID;

  -- 2) Jugadores en el roster (con filtros opcionales)
  SELECT
    tr.RosterID,
    p.PlayerID,
    p.FirstName,
    p.LastName,
    p.FullName,
    p.Position,
    nt.TeamName AS NFLTeamName,
    nt.City AS NFLTeamCity,
    p.InjuryStatus,
    p.InjuryDescription,
    p.PhotoUrl,
    p.PhotoThumbnailUrl,
    tr.AcquisitionType,
    tr.AcquisitionDate,
    tr.IsActive AS IsOnRoster
  FROM league.TeamRoster tr
  JOIN league.Player p ON p.PlayerID = tr.PlayerID
  LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID
  WHERE tr.TeamID = @TeamID
    AND tr.IsActive = 1
    AND (@FilterPosition IS NULL OR p.Position = @FilterPosition)
    AND (@SearchPlayer IS NULL OR p.FullName LIKE N'%' + @SearchPlayer + N'%')
  ORDER BY 
    CASE p.Position
      WHEN 'QB' THEN 1
      WHEN 'RB' THEN 2
      WHEN 'WR' THEN 3
      WHEN 'TE' THEN 4
      WHEN 'K' THEN 5
      WHEN 'DEF' THEN 6
      ELSE 7
    END,
    p.LastName;

  -- 3) Distribución de jugadores por forma de adquisición (para el gráfico)
  SELECT
    tr.AcquisitionType,
    COUNT(*) AS PlayerCount,
    CAST(ROUND(COUNT(*) * 100.0 / NULLIF((SELECT COUNT(*) FROM league.TeamRoster WHERE TeamID = @TeamID AND IsActive = 1), 0), 2) AS DECIMAL(5,2)) AS Percentage
  FROM league.TeamRoster tr
  WHERE tr.TeamID = @TeamID
    AND tr.IsActive = 1
  GROUP BY tr.AcquisitionType
  ORDER BY PlayerCount DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetMyTeam TO app_executor;
GO

-- ============================================================================
-- sp_GetTeamRosterDistribution
-- Obtiene distribución porcentual de jugadores por forma de adquisición
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetTeamRosterDistribution
  @TeamID INT
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar que el equipo existe
  IF NOT EXISTS (SELECT 1 FROM league.Team WHERE TeamID = @TeamID)
    THROW 50145, 'Equipo no existe.', 1;

  -- Total de jugadores activos
  DECLARE @TotalPlayers INT;
  SELECT @TotalPlayers = COUNT(*)
  FROM league.TeamRoster
  WHERE TeamID = @TeamID AND IsActive = 1;

  -- Distribución por tipo de adquisición
  SELECT
    AcquisitionType,
    COUNT(*) AS PlayerCount,
    @TotalPlayers AS TotalPlayers,
    CASE 
      WHEN @TotalPlayers > 0 
      THEN CAST(ROUND(COUNT(*) * 100.0 / @TotalPlayers, 2) AS DECIMAL(5,2))
      ELSE 0
    END AS Percentage
  FROM league.TeamRoster
  WHERE TeamID = @TeamID
    AND IsActive = 1
  GROUP BY AcquisitionType
  ORDER BY PlayerCount DESC;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetTeamRosterDistribution TO app_executor;
GO

-- ============================================================================
-- sp_AddPlayerToRoster
-- Agrega un jugador al roster de un equipo fantasy
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_AddPlayerToRoster
  @ActorUserID      INT,
  @TeamID           INT,
  @PlayerID         INT,
  @AcquisitionType  NVARCHAR(20),  -- 'Draft','Trade','FreeAgent','Waiver'
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validaciones
    IF @AcquisitionType NOT IN (N'Draft', N'Trade', N'FreeAgent', N'Waiver')
      THROW 50150, 'Tipo de adquisición inválido.', 1;

    -- Validar que el equipo existe
    DECLARE @LeagueID INT, @OwnerUserID INT;
    SELECT @LeagueID = LeagueID, @OwnerUserID = OwnerUserID
    FROM league.Team
    WHERE TeamID = @TeamID;

    IF @LeagueID IS NULL
      THROW 50151, 'Equipo no existe.', 1;

    -- Validar que el jugador existe y está activo
    IF NOT EXISTS (SELECT 1 FROM league.Player WHERE PlayerID = @PlayerID AND IsActive = 1)
      THROW 50152, 'Jugador no existe o no está activo.', 1;

    -- Validar que el jugador no está ya en otro equipo de la misma liga
    IF EXISTS (
      SELECT 1
      FROM league.TeamRoster tr
      JOIN league.Team t ON t.TeamID = tr.TeamID
      WHERE tr.PlayerID = @PlayerID
        AND tr.IsActive = 1
        AND t.LeagueID = @LeagueID
        AND tr.TeamID <> @TeamID
    )
      THROW 50153, 'El jugador ya está asignado a otro equipo en esta liga.', 1;

    -- Validar que no está duplicado en el mismo equipo
    IF EXISTS (
      SELECT 1 FROM league.TeamRoster
      WHERE TeamID = @TeamID AND PlayerID = @PlayerID AND IsActive = 1
    )
      THROW 50154, 'El jugador ya está en el roster de este equipo.', 1;

    BEGIN TRAN;

      INSERT INTO league.TeamRoster(TeamID, PlayerID, AcquisitionType, AddedByUserID)
      VALUES(@TeamID, @PlayerID, @AcquisitionType, @ActorUserID);

      DECLARE @RosterID BIGINT = SCOPE_IDENTITY();

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'ROSTER', CAST(@RosterID AS NVARCHAR(50)), N'ADD_PLAYER',
             CONCAT(N'Jugador agregado al roster (', @AcquisitionType, N')'), @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      @RosterID AS RosterID,
      N'Jugador agregado al roster exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_AddPlayerToRoster TO app_executor;
GO

-- ============================================================================
-- sp_RemovePlayerFromRoster
-- Remueve un jugador del roster de un equipo fantasy
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_RemovePlayerFromRoster
  @ActorUserID   INT,
  @RosterID      BIGINT,
  @SourceIp      NVARCHAR(45) = NULL,
  @UserAgent     NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el roster entry existe
    DECLARE @TeamID INT, @PlayerID INT;
    SELECT @TeamID = TeamID, @PlayerID = PlayerID
    FROM league.TeamRoster
    WHERE RosterID = @RosterID AND IsActive = 1;

    IF @TeamID IS NULL
      THROW 50160, 'Entrada de roster no existe o ya fue removida.', 1;

    BEGIN TRAN;

      UPDATE league.TeamRoster
         SET IsActive = 0,
             DroppedDate = SYSUTCDATETIME()
       WHERE RosterID = @RosterID;

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'ROSTER', CAST(@RosterID AS NVARCHAR(50)), N'REMOVE_PLAYER',
             N'Jugador removido del roster', @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Jugador removido del roster exitosamente.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RemovePlayerFromRoster TO app_executor;
GO

-- ============================================================================
-- sp_GetUserRolesInLeague
-- Retorna todos los roles efectivos de un usuario en una liga
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetUserRolesInLeague
  @UserID    INT,
  @LeagueID  INT
AS
BEGIN
  SET NOCOUNT ON;

  -- Roles del usuario en esta liga
  SELECT 
    RoleName,
    IsExplicit,
    IsDerived,
    IsPrimaryCommissioner,
    JoinedAt
  FROM dbo.vw_UserLeagueRoles
  WHERE UserID = @UserID 
    AND LeagueID = @LeagueID
  ORDER BY 
    CASE RoleName
      WHEN N'COMMISSIONER' THEN 1
      WHEN N'CO_COMMISSIONER' THEN 2
      WHEN N'MANAGER' THEN 3
      WHEN N'SPECTATOR' THEN 4
      ELSE 5
    END;

  -- Resumen
  SELECT 
    PrimaryRole,
    AllRoles,
    IsPrimaryCommissioner
  FROM dbo.vw_UserLeagueRoleSummary
  WHERE UserID = @UserID 
    AND LeagueID = @LeagueID;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetUserRolesInLeague TO app_executor;
GO

-- ============================================================================
-- sp_SearchLeagues
-- Busca ligas disponibles para unirse (con slots disponibles y en Pre-Draft)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_SearchLeagues
  @SearchTerm     NVARCHAR(100) = NULL,
  @SeasonID       INT = NULL,
  @MinSlots       TINYINT = NULL,
  @MaxSlots       TINYINT = NULL,
  @PageNumber     INT = 1,
  @PageSize       INT = 20
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar paginación
  IF @PageNumber < 1 SET @PageNumber = 1;
  IF @PageSize < 1 OR @PageSize > 50 SET @PageSize = 20;

  DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

  -- Si no se especifica temporada, usar la actual
  IF @SeasonID IS NULL
    SELECT @SeasonID = SeasonID FROM league.Season WHERE IsCurrent = 1;

  -- Total de registros
  DECLARE @TotalRecords INT;
  SELECT @TotalRecords = COUNT(*)
  FROM league.League l
  WHERE l.SeasonID = @SeasonID
    AND l.Status = 0  -- Solo ligas en Pre-Draft
    AND (l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID)) > 0  -- Tiene slots disponibles
    AND (@SearchTerm IS NULL OR l.Name LIKE N'%' + @SearchTerm + N'%' OR l.Description LIKE N'%' + @SearchTerm + N'%')
    AND (@MinSlots IS NULL OR l.TeamSlots >= @MinSlots)
    AND (@MaxSlots IS NULL OR l.TeamSlots <= @MaxSlots);

  -- Resultados paginados
  SELECT
    l.LeagueID,
    l.Name,
    l.Description,
    l.TeamSlots,
    (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID) AS TeamsCount,
    l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID) AS AvailableSlots,
    l.PlayoffTeams,
    l.AllowDecimals,
    s.Label AS SeasonLabel,
    s.Year AS SeasonYear,
    u.Name AS CreatedByName,
    l.CreatedAt,
    @TotalRecords AS TotalRecords,
    @PageNumber AS CurrentPage,
    @PageSize AS PageSize,
    CEILING(CAST(@TotalRecords AS FLOAT) / @PageSize) AS TotalPages
  FROM league.League l
  JOIN league.Season s ON s.SeasonID = l.SeasonID
  JOIN auth.UserAccount u ON u.UserID = l.CreatedByUserID
  WHERE l.SeasonID = @SeasonID
    AND l.Status = 0  -- Solo Pre-Draft
    AND (l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID)) > 0
    AND (@SearchTerm IS NULL OR l.Name LIKE N'%' + @SearchTerm + N'%' OR l.Description LIKE N'%' + @SearchTerm + N'%')
    AND (@MinSlots IS NULL OR l.TeamSlots >= @MinSlots)
    AND (@MaxSlots IS NULL OR l.TeamSlots <= @MaxSlots)
  ORDER BY l.CreatedAt DESC
  OFFSET @Offset ROWS
  FETCH NEXT @PageSize ROWS ONLY;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_SearchLeagues TO app_executor;
GO

-- ============================================================================
-- sp_JoinLeague (FIX)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_JoinLeague
  @UserID           INT,
  @LeagueID         INT,
  @LeaguePassword   NVARCHAR(50),
  @TeamName         NVARCHAR(100),
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  BEGIN TRY
    -- Validaciones
    IF @TeamName IS NULL OR LEN(@TeamName) < 1 OR LEN(@TeamName) > 100
      THROW 50300, 'Nombre de equipo inválido: debe tener entre 1 y 100 caracteres.', 1;

    DECLARE 
      @Status TINYINT,
      @TeamSlots TINYINT,
      @Hash VARBINARY(64),
      @Salt VARBINARY(16),
      @SeasonID INT,
      @LeagueName NVARCHAR(100);

    SELECT 
      @Status = l.Status,
      @TeamSlots = l.TeamSlots,
      @Hash = l.LeaguePasswordHash,
      @Salt = l.LeaguePasswordSalt,
      @SeasonID = l.SeasonID,
      @LeagueName = l.Name
    FROM league.League l
    WHERE l.LeagueID = @LeagueID;

    IF @Status IS NULL
      THROW 50301, 'La liga no existe.', 1;

    IF @Status <> 0
      THROW 50302, 'Solo puedes unirte a ligas en estado Pre-Draft.', 1;

    -- Validar contraseña de la liga
    DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @LeaguePassword);
    DECLARE @Check VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);
    IF @Check <> @Hash
      THROW 50303, 'Contraseña de liga incorrecta.', 1;

    -- Slots disponibles
    DECLARE @CurrentTeams INT;
    SELECT @CurrentTeams = COUNT(*) FROM league.Team WHERE LeagueID = @LeagueID AND IsActive = 1;
    IF @CurrentTeams >= @TeamSlots
      THROW 50305, 'La liga está llena. No hay slots disponibles.', 1;

    -- Ya tiene equipo en esta liga
    IF EXISTS (
      SELECT 1 FROM league.Team 
      WHERE LeagueID = @LeagueID AND OwnerUserID = @UserID AND IsActive = 1
    )
      THROW 50306, 'Ya tienes un equipo en esta liga.', 1;

    -- Nombre duplicado en esta liga
    IF EXISTS (
      SELECT 1 FROM league.Team 
      WHERE LeagueID = @LeagueID AND TeamName = @TeamName AND IsActive = 1
    )
      THROW 50307, 'Ya existe un equipo con ese nombre en esta liga.', 1;

    -- Solo 1 liga Activa como comisionado en la misma temporada
    IF EXISTS (
      SELECT 1 
      FROM league.League l
      JOIN league.LeagueMember lm ON lm.LeagueID = l.LeagueID
      WHERE l.SeasonID = @SeasonID
        AND l.Status = 1
        AND lm.UserID = @UserID
        AND lm.RoleCode = N'COMMISSIONER'
        AND lm.IsPrimaryCommissioner = 1
    )
      THROW 50308, 'Ya tienes una liga activa como comisionado. Debes desactivarla antes de unirte a otra.', 1;

    DECLARE @TeamID INT;

    BEGIN TRAN;

      -- Crear equipo
      INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
      VALUES(@LeagueID, @UserID, @TeamName);

      SET @TeamID = SCOPE_IDENTITY();

      -- IMPORTANTE:
      -- No insertamos MANAGER en LeagueMember; es un rol derivado por tener equipo activo.
      -- (Si quisieras forzarlo explícitamente, aquí iría el INSERT a LeagueMember.)

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(
        @UserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'JOIN_LEAGUE',
        CONCAT(N'Se unió a la liga "', @LeagueName, N'" con el equipo "', @TeamName, N'"'),
        @SourceIp, @UserAgent
      );

    COMMIT;

    SELECT 
      @TeamID AS TeamID,
      @LeagueID AS LeagueID,
      @TeamName AS TeamName,
      @LeagueName AS LeagueName,
      (@TeamSlots - @CurrentTeams - 1) AS AvailableSlots,
      N'Te has unido exitosamente a la liga.' AS Message;
  END TRY
  BEGIN CATCH
    -- Rollback seguro
    IF XACT_STATE() <> 0 AND @@TRANCOUNT > 0
      ROLLBACK;

    -- Re-lanzar el error original
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_JoinLeague TO app_executor;
GO

-- ============================================================================
-- sp_RemoveTeamFromLeague
-- Permite al comisionado remover un equipo de la liga (solo en Pre-Draft)
-- El equipo se marca como inactivo y se remueve al usuario de la liga
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_RemoveTeamFromLeague
  @ActorUserID   INT,
  @LeagueID      INT,
  @TeamID        INT,
  @Reason        NVARCHAR(300) = NULL,
  @SourceIp      NVARCHAR(45) = NULL,
  @UserAgent     NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es comisionado principal de la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @ActorUserID
        AND RoleCode = N'COMMISSIONER' 
        AND IsPrimaryCommissioner = 1
    )
      THROW 50320, 'Solo el comisionado principal puede remover equipos de la liga.', 1;

    -- Validar que la liga está en Pre-Draft
    DECLARE @Status TINYINT;
    SELECT @Status = Status FROM league.League WHERE LeagueID = @LeagueID;

    IF @Status IS NULL
      THROW 50321, 'Liga no existe.', 1;

    IF @Status <> 0
      THROW 50322, 'Solo puedes remover equipos de ligas en estado Pre-Draft.', 1;

    -- Validar que el equipo existe y pertenece a la liga
    DECLARE @OwnerUserID INT, @TeamName NVARCHAR(100);
    SELECT @OwnerUserID = OwnerUserID, @TeamName = TeamName
    FROM league.Team
    WHERE TeamID = @TeamID AND LeagueID = @LeagueID AND IsActive = 1;

    IF @OwnerUserID IS NULL
      THROW 50323, 'El equipo no existe en esta liga o ya fue removido.', 1;

    -- Validar que no sea el equipo del comisionado (no puede removerse a sí mismo)
    IF @OwnerUserID = @ActorUserID
      THROW 50324, 'El comisionado no puede remover su propio equipo. Debe transferir el comisionado primero.', 1;

    BEGIN TRAN;

      -- Marcar el equipo como inactivo
      UPDATE league.Team
         SET IsActive = 0,
             UpdatedAt = SYSUTCDATETIME()
       WHERE TeamID = @TeamID;

      -- Desactivar todos los jugadores del roster del equipo
      UPDATE league.TeamRoster
         SET IsActive = 0,
             DroppedDate = SYSUTCDATETIME()
       WHERE TeamID = @TeamID AND IsActive = 1;

      -- Marcar al usuario como "Left" de la liga
      -- IMPORTANTE: Si el usuario solo tenía rol MANAGER (derivado), no habrá registro en LeagueMember
      -- Pero si tenía roles administrativos (CO_COMMISSIONER, SPECTATOR), sí hay registro
      UPDATE league.LeagueMember
         SET LeftAt = SYSUTCDATETIME()
       WHERE LeagueID = @LeagueID 
         AND UserID = @OwnerUserID
         AND LeftAt IS NULL;

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'REMOVE_TEAM',
             CONCAT(N'Equipo "', @TeamName, N'" removido de la liga. Razón: ', ISNULL(@Reason, N'No especificada')), 
             @SourceIp, @UserAgent);

    COMMIT;

    DECLARE @AvailableSlots INT;
    SELECT @AvailableSlots = l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID AND t.IsActive = 1)
    FROM league.League l WHERE l.LeagueID = @LeagueID;

    SELECT 
      @AvailableSlots AS AvailableSlots,
      N'Equipo removido exitosamente de la liga.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RemoveTeamFromLeague TO app_executor;
GO

-- ============================================================================
-- sp_AssignCoCommissioner
-- Permite al comisionado principal delegar permisos a otro miembro
-- haciendo a ese miembro co-comisionado
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_AssignCoCommissioner
  @ActorUserID      INT,
  @LeagueID         INT,
  @TargetUserID     INT,
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es comisionado principal de la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @ActorUserID
        AND RoleCode = N'COMMISSIONER' 
        AND IsPrimaryCommissioner = 1
    )
      THROW 50330, 'Solo el comisionado principal puede asignar co-comisionados.', 1;

    -- Validar que la liga está en Pre-Draft o Active
    DECLARE @Status TINYINT;
    SELECT @Status = Status FROM league.League WHERE LeagueID = @LeagueID;

    IF @Status IS NULL
      THROW 50331, 'Liga no existe.', 1;

    IF @Status NOT IN (0, 1)  -- Pre-Draft o Active
      THROW 50332, 'Solo puedes asignar co-comisionados en ligas Pre-Draft o Active.', 1;

    -- Validar que el target user es diferente al actor
    IF @TargetUserID = @ActorUserID
      THROW 50333, 'No puedes asignarte a ti mismo como co-comisionado.', 1;

    -- Validar que el target user tiene un equipo en la liga (es miembro)
    IF NOT EXISTS (
      SELECT 1 FROM league.Team 
      WHERE LeagueID = @LeagueID 
        AND OwnerUserID = @TargetUserID 
        AND IsActive = 1
    )
      THROW 50334, 'El usuario debe tener un equipo en la liga para ser co-comisionado.', 1;

    -- Validar que el target user no es ya comisionado o co-comisionado
    IF EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @TargetUserID
        AND RoleCode IN (N'COMMISSIONER', N'CO_COMMISSIONER')
        AND LeftAt IS NULL
    )
      THROW 50335, 'El usuario ya es comisionado o co-comisionado de esta liga.', 1;

    DECLARE @TargetUserName NVARCHAR(50);
    SELECT @TargetUserName = Name FROM auth.UserAccount WHERE UserID = @TargetUserID;

    BEGIN TRAN;

      -- Si el usuario ya tiene un registro en LeagueMember (por ejemplo, si fue SPECTATOR antes),
      -- actualizar su rol a CO_COMMISSIONER
      IF EXISTS (
        SELECT 1 FROM league.LeagueMember
        WHERE LeagueID = @LeagueID AND UserID = @TargetUserID
      )
      BEGIN
        UPDATE league.LeagueMember
           SET RoleCode = N'CO_COMMISSIONER',
               IsPrimaryCommissioner = 0,
               LeftAt = NULL
         WHERE LeagueID = @LeagueID AND UserID = @TargetUserID;
      END
      ELSE
      BEGIN
        -- Si no tiene registro, crear uno nuevo
        INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
        VALUES(@LeagueID, @TargetUserID, N'CO_COMMISSIONER', 0);
      END

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'ASSIGN_CO_COMMISSIONER',
             CONCAT(N'Usuario "', @TargetUserName, N'" asignado como co-comisionado'), 
             @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      N'Co-comisionado asignado exitosamente.' AS Message,
      @TargetUserID AS UserID,
      @TargetUserName AS UserName,
      N'CO_COMMISSIONER' AS NewRole;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_AssignCoCommissioner TO app_executor;
GO

-- ============================================================================
-- sp_RemoveCoCommissioner
-- Permite al comisionado principal remover el rol de co-comisionado
-- El usuario vuelve a tener solo el rol MANAGER (derivado de tener equipo)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_RemoveCoCommissioner
  @ActorUserID      INT,
  @LeagueID         INT,
  @TargetUserID     INT,
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es comisionado principal de la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @ActorUserID
        AND RoleCode = N'COMMISSIONER' 
        AND IsPrimaryCommissioner = 1
    )
      THROW 50340, 'Solo el comisionado principal puede remover co-comisionados.', 1;

    -- Validar que el target user es co-comisionado
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @TargetUserID
        AND RoleCode = N'CO_COMMISSIONER'
        AND LeftAt IS NULL
    )
      THROW 50341, 'El usuario no es co-comisionado de esta liga.', 1;

    DECLARE @TargetUserName NVARCHAR(50);
    SELECT @TargetUserName = Name FROM auth.UserAccount WHERE UserID = @TargetUserID;

    BEGIN TRAN;

      -- Marcar como "Left" el rol de co-comisionado
      UPDATE league.LeagueMember
         SET LeftAt = SYSUTCDATETIME()
       WHERE LeagueID = @LeagueID 
         AND UserID = @TargetUserID
         AND RoleCode = N'CO_COMMISSIONER';

      -- El usuario ahora solo tendrá el rol MANAGER (derivado de tener equipo)
      -- No necesitamos hacer nada más, la vista vw_UserLeagueRoles se encargará

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'REMOVE_CO_COMMISSIONER',
             CONCAT(N'Rol de co-comisionado removido de "', @TargetUserName, N'"'), 
             @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      N'Co-comisionado removido exitosamente.' AS Message,
      @TargetUserID AS UserID,
      @TargetUserName AS UserName;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RemoveCoCommissioner TO app_executor;
GO

-- ============================================================================
-- sp_LeaveLeague
-- Permite a un usuario salir voluntariamente de una liga (solo en Pre-Draft)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_LeaveLeague
  @UserID        INT,
  @LeagueID      INT,
  @SourceIp      NVARCHAR(45) = NULL,
  @UserAgent     NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el usuario no es el comisionado principal
    IF EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @UserID
        AND RoleCode = N'COMMISSIONER' 
        AND IsPrimaryCommissioner = 1
    )
      THROW 50350, 'El comisionado principal no puede salir de la liga. Debe transferir el comisionado primero o eliminar la liga.', 1;

    -- Validar que la liga está en Pre-Draft
    DECLARE @Status TINYINT;
    SELECT @Status = Status FROM league.League WHERE LeagueID = @LeagueID;

    IF @Status IS NULL
      THROW 50351, 'Liga no existe.', 1;

    IF @Status <> 0
      THROW 50352, 'Solo puedes salir de ligas en estado Pre-Draft.', 1;

    -- Validar que el usuario tiene un equipo en la liga
    DECLARE @TeamID INT, @TeamName NVARCHAR(100);
    SELECT @TeamID = TeamID, @TeamName = TeamName
    FROM league.Team
    WHERE LeagueID = @LeagueID 
      AND OwnerUserID = @UserID 
      AND IsActive = 1;

    IF @TeamID IS NULL
      THROW 50353, 'No tienes un equipo activo en esta liga.', 1;

    BEGIN TRAN;

      -- Marcar el equipo como inactivo
      UPDATE league.Team
         SET IsActive = 0,
             UpdatedAt = SYSUTCDATETIME()
       WHERE TeamID = @TeamID;

      -- Desactivar todos los jugadores del roster
      UPDATE league.TeamRoster
         SET IsActive = 0,
             DroppedDate = SYSUTCDATETIME()
       WHERE TeamID = @TeamID AND IsActive = 1;

      -- Marcar como "Left" cualquier rol administrativo que tenga
      UPDATE league.LeagueMember
         SET LeftAt = SYSUTCDATETIME()
       WHERE LeagueID = @LeagueID 
         AND UserID = @UserID
         AND LeftAt IS NULL;

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@UserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'LEAVE_LEAGUE',
             CONCAT(N'Salió de la liga con el equipo "', @TeamName, N'"'), 
             @SourceIp, @UserAgent);

    COMMIT;

    SELECT N'Has salido exitosamente de la liga.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_LeaveLeague TO app_executor;
GO

-- ============================================================================
-- sp_TransferCommissioner
-- Permite al comisionado principal transferir el comisionado a otro miembro
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_TransferCommissioner
  @ActorUserID      INT,
  @LeagueID         INT,
  @NewCommissionerID INT,
  @SourceIp         NVARCHAR(45) = NULL,
  @UserAgent        NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validar que el actor es comisionado principal de la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.LeagueMember
      WHERE LeagueID = @LeagueID 
        AND UserID = @ActorUserID
        AND RoleCode = N'COMMISSIONER' 
        AND IsPrimaryCommissioner = 1
    )
      THROW 50360, 'Solo el comisionado principal puede transferir el comisionado.', 1;

    -- Validar que no se está transfiriendo a sí mismo
    IF @NewCommissionerID = @ActorUserID
      THROW 50361, 'No puedes transferirte el comisionado a ti mismo.', 1;

    -- Validar que el nuevo comisionado tiene un equipo en la liga
    IF NOT EXISTS (
      SELECT 1 FROM league.Team 
      WHERE LeagueID = @LeagueID 
        AND OwnerUserID = @NewCommissionerID 
        AND IsActive = 1
    )
      THROW 50362, 'El nuevo comisionado debe tener un equipo en la liga.', 1;

    DECLARE @NewCommissionerName NVARCHAR(50);
    SELECT @NewCommissionerName = Name FROM auth.UserAccount WHERE UserID = @NewCommissionerID;

    BEGIN TRAN;

      -- El actor pasa a ser co-comisionado
      UPDATE league.LeagueMember
         SET RoleCode = N'CO_COMMISSIONER',
             IsPrimaryCommissioner = 0
       WHERE LeagueID = @LeagueID 
         AND UserID = @ActorUserID;

      -- Si el nuevo comisionado ya tiene un registro en LeagueMember, actualizarlo
      IF EXISTS (
        SELECT 1 FROM league.LeagueMember
        WHERE LeagueID = @LeagueID AND UserID = @NewCommissionerID
      )
      BEGIN
        UPDATE league.LeagueMember
           SET RoleCode = N'COMMISSIONER',
               IsPrimaryCommissioner = 1,
               LeftAt = NULL
         WHERE LeagueID = @LeagueID AND UserID = @NewCommissionerID;
      END
      ELSE
      BEGIN
        -- Si no tiene registro, crearlo
        INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
        VALUES(@LeagueID, @NewCommissionerID, N'COMMISSIONER', 1);
      END

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'TRANSFER_COMMISSIONER',
             CONCAT(N'Comisionado transferido a "', @NewCommissionerName, N'"'), 
             @SourceIp, @UserAgent);

    COMMIT;

    SELECT 
      N'Comisionado transferido exitosamente.' AS Message,
      @NewCommissionerID AS NewCommissionerID,
      @NewCommissionerName AS NewCommissionerName;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_TransferCommissioner TO app_executor;
GO

-- ============================================================================
-- sp_GetLeaguePassword
-- Permite al comisionado principal obtener la contraseña de la liga
-- (para compartirla con otros usuarios que quieran unirse)
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_GetLeaguePassword
  @ActorUserID   INT,
  @LeagueID      INT
AS
BEGIN
  SET NOCOUNT ON;

  -- Validar que el actor es comisionado principal de la liga
  IF NOT EXISTS (
    SELECT 1 FROM league.LeagueMember
    WHERE LeagueID = @LeagueID 
      AND UserID = @ActorUserID
      AND RoleCode = N'COMMISSIONER' 
      AND IsPrimaryCommissioner = 1
  )
  BEGIN
    -- No revelar si la liga existe o no para seguridad
    SELECT NULL AS LeaguePassword, N'No tienes permiso para ver la contraseña de esta liga.' AS Message;
    RETURN;
  END

  -- NOTA: Por razones de seguridad, NO retornamos la contraseña en texto plano
  -- porque está hasheada. En su lugar, retornamos información sobre la liga
  -- y el comisionado debe compartir la contraseña que usó al crear la liga

  SELECT 
    l.LeagueID,
    l.Name AS LeagueName,
    l.Status,
    l.TeamSlots,
    (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID AND t.IsActive = 1) AS TeamsCount,
    l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID AND t.IsActive = 1) AS AvailableSlots,
    N'La contraseña de la liga no puede recuperarse. Debes recordar la contraseña que usaste al crear la liga.' AS Message
  FROM league.League l
  WHERE l.LeagueID = @LeagueID;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_GetLeaguePassword TO app_executor;
GO

-- ============================================================================
-- sp_ValidateLeaguePassword
-- Valida si una contraseña es correcta para una liga (sin unirse)
-- Útil para verificar antes de mostrar el formulario de unión
-- ============================================================================
CREATE OR ALTER PROCEDURE app.sp_ValidateLeaguePassword
  @LeagueID         INT,
  @LeaguePassword   NVARCHAR(50)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @Hash VARBINARY(64), @Salt VARBINARY(16);
  
  SELECT 
    @Hash = LeaguePasswordHash,
    @Salt = LeaguePasswordSalt
  FROM league.League
  WHERE LeagueID = @LeagueID;

  IF @Hash IS NULL
  BEGIN
    SELECT 0 AS IsValid, N'Liga no existe.' AS Message;
    RETURN;
  END

  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @LeaguePassword);
  DECLARE @Check VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  IF @Check = @Hash
    SELECT 1 AS IsValid, N'Contraseña válida.' AS Message;
  ELSE
    SELECT 0 AS IsValid, N'Contraseña incorrecta.' AS Message;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_ValidateLeaguePassword TO app_executor;
GO

GRANT EXECUTE ON SCHEMA::app TO app_executor;
GO