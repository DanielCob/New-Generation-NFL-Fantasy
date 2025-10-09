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
  @ProfileImageBytes  INT = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validaciones de campos
    IF @Name IS NULL OR LEN(@Name) < 1 OR LEN(@Name) > 50
      THROW 50001, 'Nombre inválido: debe tener entre 1 y 50 caracteres.', 1;

    IF @Email IS NULL OR LEN(@Email) > 50 OR @Email NOT LIKE '%_@_%._%'
      THROW 50002, 'Correo inválido o supera 50 caracteres.', 1;

    IF @Alias IS NOT NULL AND LEN(@Alias) > 50
      THROW 50003, 'Alias supera 50 caracteres.', 1;

    IF @Password IS NULL OR @PasswordConfirm IS NULL OR @Password <> @PasswordConfirm
      THROW 50004, 'La confirmación de contraseña no coincide.', 1;

    -- Complejidad de contraseña (8-12, alfanumérica, min1 mayús, min1 minús, min1 dígito)
    IF LEN(@Password) < 8 OR LEN(@Password) > 12
      THROW 50005, 'Contraseña inválida: longitud 8-12.', 1;

    -- case-sensitive checks con collate binario
    IF PATINDEX('%[A-Z]%', @Password COLLATE Latin1_General_BIN2) = 0
      THROW 50006, 'Contraseña debe incluir al menos una mayúscula.', 1;
    IF PATINDEX('%[a-z]%', @Password COLLATE Latin1_General_BIN2) = 0
      THROW 50007, 'Contraseña debe incluir al menos una minúscula.', 1;
    IF PATINDEX('%[0-9]%', @Password) = 0
      THROW 50008, 'Contraseña debe incluir al menos un dígito.', 1;
    IF @Password LIKE '%[^0-9A-Za-z]%'
      THROW 50009, 'Contraseña debe ser alfanumérica (sin caracteres especiales).', 1;

    -- Imagen (opcional) reglas: bytes <= 5MB, dims 300-1024 si vienen
    IF @ProfileImageBytes IS NOT NULL AND @ProfileImageBytes > 5242880
      THROW 50010, 'Imagen supera 5MB.', 1;
    IF @ProfileImageWidth IS NOT NULL AND (@ProfileImageWidth < 300 OR @ProfileImageWidth > 1024)
      THROW 50011, 'Ancho de imagen fuera de rango (300-1024).', 1;
    IF @ProfileImageHeight IS NOT NULL AND (@ProfileImageHeight < 300 OR @ProfileImageHeight > 1024)
      THROW 50012, 'Alto de imagen fuera de rango (300-1024).', 1;

    -- Unicidad de email
    IF EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email = @Email)
      THROW 50013, 'Correo duplicado.', 1;

    DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
    DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @Password);
    DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

    DECLARE @UserID INT;

    BEGIN TRAN;
      INSERT INTO auth.UserAccount
      (Email, PasswordHash, PasswordSalt, Name, Alias, LanguageCode,
       ProfileImageUrl, ProfileImageWidth, ProfileImageHeight, ProfileImageBytes,
       AccountStatus, FailedLoginCount, LockedUntil)
      VALUES
      (@Email, @Hash, @Salt, @Name, @Alias, @LanguageCode,
       @ProfileImageUrl, @ProfileImageWidth, @ProfileImageHeight, @ProfileImageBytes,
       1, 0, NULL);

      SET @UserID = SCOPE_IDENTITY();

      -- Auditoría: alta de usuario
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'CREATE', N'Registro exitoso');

    COMMIT;

    SELECT @UserID AS UserID, N'Registro exitoso.' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RegisterUser TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_Login
  @Email       NVARCHAR(50),
  @Password    NVARCHAR(50),
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

  -- Log intentos (con o sin usuario) al final, para uniformidad.

  IF @UserID IS NULL
  BEGIN
    SET @Message = N'Credenciales inválidas.';
    INSERT INTO auth.LoginAttempt(UserID, Email, Success) VALUES(NULL, @Email, 0);
    RETURN;
  END

  -- Cuenta bloqueada
  IF @Status = 2 AND (@LockedUntil IS NULL OR @LockedUntil > SYSUTCDATETIME())
  BEGIN
    SET @Message = N'Cuenta bloqueada. Solicite restablecer.';
    INSERT INTO auth.LoginAttempt(UserID, Email, Success) VALUES(@UserID, @Email, 0);
    RETURN;
  END

  -- Verificar contraseña
  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @Password);
  DECLARE @Check VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  IF @Check <> @Hash
  BEGIN
    -- fallo: incrementar contador y bloquear si llega a 5
    BEGIN TRY
      BEGIN TRAN;
        UPDATE auth.UserAccount
           SET FailedLoginCount = FailedLoginCount + 1,
               AccountStatus = CASE WHEN FailedLoginCount + 1 >= 5 THEN 2 ELSE AccountStatus END,
               LockedUntil = CASE WHEN FailedLoginCount + 1 >= 5 THEN DATEADD(HOUR, 1, SYSUTCDATETIME()) ELSE LockedUntil END,
               UpdatedAt = SYSUTCDATETIME()
         WHERE UserID = @UserID;

        INSERT INTO auth.LoginAttempt(UserID, Email, Success) VALUES(@UserID, @Email, 0);
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

  -- Éxito: resetear fallos y crear sesión (12 horas de inactividad)
  BEGIN TRY
    BEGIN TRAN;

      UPDATE auth.UserAccount
         SET FailedLoginCount = 0,
             AccountStatus = 1,       -- Active
             LockedUntil = NULL,
             UpdatedAt = SYSUTCDATETIME()
       WHERE UserID = @UserID;

      SET @SessionID = NEWID();
      INSERT INTO auth.Session(SessionID, UserID, ExpiresAt)
      VALUES(@SessionID, @UserID, DATEADD(HOUR, 12, SYSUTCDATETIME()));

      INSERT INTO auth.LoginAttempt(UserID, Email, Success) VALUES(@UserID, @Email, 1);

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'LOGIN', N'Inicio de sesión exitoso');

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

CREATE OR ALTER PROCEDURE app.sp_Logout
  @SessionID UNIQUEIDENTIFIER
AS
BEGIN
  SET NOCOUNT ON;

  UPDATE auth.Session
     SET IsValid = 0
   WHERE SessionID = @SessionID AND IsValid = 1;

  INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
  SELECT s.UserID, N'USER_PROFILE', CAST(s.UserID AS NVARCHAR(50)), N'LOGOUT', N'Cierre de sesión'
  FROM auth.Session s WHERE s.SessionID = @SessionID;

  SELECT N'Sesión cerrada.' AS Message;
END
GO

GRANT EXECUTE ON OBJECT::app.sp_Logout TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_RequestPasswordReset
  @Email    NVARCHAR(50),
  @Token    NVARCHAR(100) OUTPUT,
  @ExpiresAt DATETIME2(0) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET @Token = NULL; SET @ExpiresAt = NULL;

  DECLARE @UserID INT;
  SELECT @UserID = UserID FROM auth.UserAccount WHERE Email = @Email;

  -- Respuesta genérica para no filtrar existencia
  IF @UserID IS NULL
  BEGIN
    SET @Token = NULL; SET @ExpiresAt = NULL;
    RETURN;
  END

  -- Generar token opaco (GUID + aleatorio base36 simple)
  DECLARE @guid NVARCHAR(36) = CONVERT(NVARCHAR(36), NEWID());
  DECLARE @rand NVARCHAR(12) = REPLACE(CONVERT(NVARCHAR(12), NEWID()),'-','');
  SET @Token = @guid + N'-' + @rand;

  SET @ExpiresAt = DATEADD(MINUTE, 60, SYSUTCDATETIME()); -- 60 minutos

  INSERT INTO auth.PasswordResetRequest(UserID, Token, ExpiresAt)
  VALUES(@UserID, @Token, @ExpiresAt);

  INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
  VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'RESET_REQUEST', N'Solicitud de restablecimiento');

END
GO

GRANT EXECUTE ON OBJECT::app.sp_RequestPasswordReset TO app_executor;
GO

CREATE OR ALTER PROCEDURE app.sp_ResetPasswordWithToken
  @Token            NVARCHAR(100),
  @NewPassword      NVARCHAR(50),
  @ConfirmPassword  NVARCHAR(50)
AS
BEGIN
  SET NOCOUNT ON;

  IF @NewPassword IS NULL OR @ConfirmPassword IS NULL OR @NewPassword <> @ConfirmPassword
    THROW 50020, 'La confirmación de contraseña no coincide.', 1;

  -- Complejidad
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

      -- Actualizar contraseña + desbloquear + resetear contadores + invalidar sesiones
      UPDATE auth.UserAccount
         SET PasswordHash = @Hash,
             PasswordSalt = @Salt,
             AccountStatus = 1,      -- Active
             FailedLoginCount = 0,
             LockedUntil = NULL,
             UpdatedAt = @Now
       WHERE UserID = @UserID;

      UPDATE auth.PasswordResetRequest
         SET UsedAt = @Now
       WHERE Token = @Token;

      UPDATE auth.Session SET IsValid = 0 WHERE UserID = @UserID AND IsValid = 1;

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@UserID, N'USER_PROFILE', CAST(@UserID AS NVARCHAR(50)), N'RESET_PASSWORD', N'Password restablecida');

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

CREATE OR ALTER PROCEDURE app.sp_UpdateUserProfile
  @ActorUserID        INT,            -- quién hace el cambio (mismo usuario o admin en el futuro)
  @TargetUserID       INT,
  @Name               NVARCHAR(50) = NULL,
  @Alias              NVARCHAR(50) = NULL,
  @LanguageCode       NVARCHAR(10) = NULL,
  @ProfileImageUrl    NVARCHAR(400) = NULL,
  @ProfileImageWidth  SMALLINT = NULL,
  @ProfileImageHeight SMALLINT = NULL,
  @ProfileImageBytes  INT = NULL
AS
BEGIN
  SET NOCOUNT ON;

  -- Validaciones de formato
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

      -- Log por campo cambiado
      IF @Name IS NOT NULL AND @Name <> @OldName
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'Name', @OldName, @Name);

      IF @Alias IS NOT NULL AND @Alias <> @OldAlias
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'Alias', @OldAlias, @Alias);

      IF @LanguageCode IS NOT NULL AND @LanguageCode <> @OldLang
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'LanguageCode', @OldLang, @LanguageCode);

      IF @ProfileImageUrl IS NOT NULL AND @ProfileImageUrl <> @OldUrl
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageUrl', @OldUrl, @ProfileImageUrl);

      IF @ProfileImageWidth IS NOT NULL AND @ProfileImageWidth <> @OldW
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageWidth', CONVERT(NVARCHAR(50), @OldW), CONVERT(NVARCHAR(50), @ProfileImageWidth));

      IF @ProfileImageHeight IS NOT NULL AND @ProfileImageHeight <> @OldH
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageHeight', CONVERT(NVARCHAR(50), @OldH), CONVERT(NVARCHAR(50), @ProfileImageHeight));

      IF @ProfileImageBytes IS NOT NULL AND @ProfileImageBytes <> @OldB
        INSERT INTO auth.ProfileChangeLog(UserID, ChangedByUserID, FieldName, OldValue, NewValue)
        VALUES(@TargetUserID, @ActorUserID, N'ProfileImageBytes', CONVERT(NVARCHAR(50), @OldB), CONVERT(NVARCHAR(50), @ProfileImageBytes));

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@ActorUserID, N'USER_PROFILE', CAST(@TargetUserID AS NVARCHAR(50)), N'UPDATE', N'Actualización de perfil');

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

CREATE OR ALTER PROCEDURE app.sp_GetUserProfile
  @UserID INT
AS
BEGIN
  SET NOCOUNT ON;

  -- 1) Perfil (incluye Role global inicial = 'MANAGER')
  SELECT
    u.UserID, u.Email, u.Name, u.Alias, u.LanguageCode,
    u.ProfileImageUrl, u.ProfileImageWidth, u.ProfileImageHeight, u.ProfileImageBytes,
    u.AccountStatus, u.CreatedAt, u.UpdatedAt,
    CAST(N'MANAGER' AS NVARCHAR(20)) AS [Role]
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

CREATE OR ALTER PROCEDURE app.sp_CreateLeague
  @CreatorUserID        INT,
  @Name                 NVARCHAR(100),
  @Description          NVARCHAR(500) = NULL,
  @TeamSlots            TINYINT,
  @LeaguePassword       NVARCHAR(50),
  @InitialTeamName      NVARCHAR(50),
  @PlayoffTeams         TINYINT = 4,          -- 4 o 6
  @AllowDecimals        BIT = 1,
  @PositionFormatID     INT = NULL,           -- si NULL, usa 'Default'
  @ScoringSchemaID      INT = NULL            -- si NULL, usa 'Default', Version 1
AS
BEGIN
  SET NOCOUNT ON;

  -- Validaciones
  IF @Name IS NULL OR LEN(@Name) < 1 OR LEN(@Name) > 100
    THROW 50040, 'Nombre de liga inválido (1-100).', 1;

  IF @TeamSlots NOT IN (4,6,8,10,12,14,16,18,20)
    THROW 50041, 'TeamSlots inválido.', 1;

  IF @PlayoffTeams NOT IN (4,6)
    THROW 50042, 'PlayoffTeams inválido (4 o 6).', 1;

  IF @InitialTeamName IS NULL OR LEN(@InitialTeamName) < 1 OR LEN(@InitialTeamName) > 50
    THROW 50043, 'Nombre de equipo inválido (1-50).', 1;

  -- Password de liga con mismas reglas de cuenta
  IF @LeaguePassword IS NULL OR LEN(@LeaguePassword) < 8 OR LEN(@LeaguePassword) > 12
    THROW 50044, 'Contraseña de liga inválida: longitud 8-12.', 1;
  IF PATINDEX('%[A-Z]%', @LeaguePassword COLLATE Latin1_General_BIN2) = 0
    THROW 50045, 'Contraseña de liga debe incluir al menos una mayúscula.', 1;
  IF PATINDEX('%[a-z]%', @LeaguePassword COLLATE Latin1_General_BIN2) = 0
    THROW 50046, 'Contraseña de liga debe incluir al menos una minúscula.', 1;
  IF PATINDEX('%[0-9]%', @LeaguePassword) = 0
    THROW 50047, 'Contraseña de liga debe incluir al menos un dígito.', 1;
  IF @LeaguePassword LIKE '%[^0-9A-Za-z]%'
    THROW 50048, 'Contraseña de liga debe ser alfanumérica.', 1;

  DECLARE @SeasonID INT;
  SELECT @SeasonID = SeasonID FROM league.Season WHERE IsCurrent = 1;
  IF @SeasonID IS NULL
    THROW 50049, 'No hay temporada actual configurada.', 1;

  -- Defaults para formatos/score si vienen NULL
  IF @PositionFormatID IS NULL
    SELECT TOP 1 @PositionFormatID = PositionFormatID FROM ref.PositionFormat WHERE Name = N'Default';
  IF @PositionFormatID IS NULL
    THROW 50050, 'No existe PositionFormat por defecto.', 1;

  IF @ScoringSchemaID IS NULL
    SELECT TOP 1 @ScoringSchemaID = ScoringSchemaID FROM scoring.ScoringSchema WHERE Name = N'Default' AND Version = 1;
  IF @ScoringSchemaID IS NULL
    THROW 50051, 'No existe ScoringSchema por defecto.', 1;

  -- Creator debe existir
  IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE UserID = @CreatorUserID)
    THROW 50052, 'Usuario creador no existe.', 1;

  -- Validar unicidad (SeasonID, Name)
  IF EXISTS (SELECT 1 FROM league.League WHERE SeasonID = @SeasonID AND Name = @Name)
    THROW 50053, 'Ya existe liga con ese nombre en la temporada actual.', 1;

  DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
  DECLARE @PwdBytes VARBINARY(4000) = CONVERT(VARBINARY(4000), @LeaguePassword);
  DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_256', @PwdBytes + @Salt);

  DECLARE @LeagueID INT;

  BEGIN TRY
    BEGIN TRAN;

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

      -- Miembro comisionado principal
      INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
      VALUES(@LeagueID, @CreatorUserID, N'COMMISSIONER', 1);

      -- Equipo del comisionado
      INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
      VALUES(@LeagueID, @CreatorUserID, @InitialTeamName);

      -- Auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@CreatorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'CREATE',
             N'Liga creada en Pre-Draft con equipo del comisionado');

    COMMIT;

    -- Cupos disponibles = TeamSlots - equipos
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

CREATE OR ALTER PROCEDURE app.sp_SetLeagueStatus
  @ActorUserID INT,
  @LeagueID    INT,
  @NewStatus   TINYINT,       -- 0=PreDraft, 1=Active, 2=Inactive, 3=Closed
  @Reason      NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;

  -- Validación rol
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
    -- Nada que hacer, pero registramos
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

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'STATUS_CHANGE',
             CONCAT(N'De ', @OldStatus, N' a ', @NewStatus, N'. ', ISNULL(@Reason, N'')));

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

CREATE OR ALTER PROCEDURE app.sp_EditLeagueConfig
  @ActorUserID              INT,
  @LeagueID                 INT,
  @Name                     NVARCHAR(100) = NULL,  -- editable siempre (con reglas)
  @Description              NVARCHAR(500) = NULL,  -- opcional
  @TeamSlots                TINYINT = NULL,        -- sólo Pre-Draft
  @PositionFormatID         INT = NULL,            -- sólo Pre-Draft
  @ScoringSchemaID          INT = NULL,            -- sólo Pre-Draft
  @PlayoffTeams             TINYINT = NULL,        -- sólo Pre-Draft (4/6)
  @AllowDecimals            BIT = NULL,            -- sólo Pre-Draft
  @TradeDeadlineEnabled     BIT = NULL,            -- sólo Pre-Draft
  @TradeDeadlineDate        DATE = NULL,           -- sólo Pre-Draft (si enabled)
  @MaxRosterChangesPerTeam  INT = NULL,            -- 1-100 o NULL (sin límite) -- editable siempre
  @MaxFreeAgentAddsPerTeam  INT = NULL             -- 1-100 o NULL (sin límite) -- editable siempre
AS
BEGIN
  SET NOCOUNT ON;

  -- Validación de permisos (comisionado principal)
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

  -- Validaciones específicas de campos
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

  -- Si se pretende editar campos restringidos y NO está en Pre-Draft -> rechazo
  IF @Status <> 0 AND (
       @TeamSlots IS NOT NULL OR @PositionFormatID IS NOT NULL OR @ScoringSchemaID IS NOT NULL
    OR @PlayoffTeams IS NOT NULL OR @AllowDecimals IS NOT NULL
    OR @TradeDeadlineEnabled IS NOT NULL OR @TradeDeadlineDate IS NOT NULL
  )
    THROW 50077, 'Esas configuraciones solo se pueden editar en estado Pre-Draft.', 1;

  -- Nombre: unicidad por temporada
  IF @Name IS NOT NULL AND @Name <> @OldName
    IF EXISTS (SELECT 1 FROM league.League WHERE SeasonID = @SeasonID AND Name = @Name)
      THROW 50078, 'Ya existe una liga con ese nombre en la temporada.', 1;

  -- TeamSlots: no reducir por debajo de equipos actuales
  IF @TeamSlots IS NOT NULL
  BEGIN
    DECLARE @Teams INT = (SELECT COUNT(*) FROM league.Team WHERE LeagueID = @LeagueID);
    IF @TeamSlots < @Teams
      THROW 50079, 'No se puede reducir TeamSlots por debajo de los equipos ya registrados.', 1;
  END

  -- Trade deadline: si enabled, fecha dentro de temporada
  IF @TradeDeadlineEnabled = 1
  BEGIN
    DECLARE @Start DATE, @End DATE;
    SELECT @Start = s.StartDate, @End = s.EndDate FROM league.Season s WHERE s.SeasonID = @SeasonID;

    IF @TradeDeadlineDate IS NULL OR @TradeDeadlineDate < @Start OR @TradeDeadlineDate > @End
      THROW 50080, 'TradeDeadlineDate debe estar dentro de la temporada.', 1;
  END
  ELSE IF @TradeDeadlineEnabled = 0
  BEGIN
    -- Si se desactiva, ignoramos la fecha
    SET @TradeDeadlineDate = NULL;
  END

  BEGIN TRY
    BEGIN TRAN;

      -- Actualización
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

      -- Historial por cada cambio relevante
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

      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@ActorUserID, N'LEAGUE', CAST(@LeagueID AS NVARCHAR(50)), N'UPDATE', N'Edición de configuración de liga');

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

CREATE OR ALTER PROCEDURE app.sp_GetLeagueSummary
  @LeagueID INT
AS
BEGIN
  SET NOCOUNT ON;

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

  -- Opcional: retornar lista de equipos
  SELECT t.TeamID, t.TeamName, t.OwnerUserID, u.Name AS OwnerName, t.CreatedAt
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

CREATE OR ALTER PROCEDURE app.sp_LogoutAllSessions
  @ActorUserID INT
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    BEGIN TRAN;

      -- invalidar todas las sesiones activas del usuario
      UPDATE auth.Session
         SET IsValid = 0
       WHERE UserID = @ActorUserID
         AND IsValid = 1;

      -- auditoría
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
      VALUES(@ActorUserID, N'USER_PROFILE', CAST(@ActorUserID AS NVARCHAR(50)), N'LOGOUT_ALL', N'Cierre de sesión global');

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

GRANT EXECUTE ON SCHEMA::app TO app_executor;
GO