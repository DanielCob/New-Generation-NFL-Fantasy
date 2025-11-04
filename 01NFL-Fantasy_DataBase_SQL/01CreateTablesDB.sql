/* ================================================
   BASE DE DATOS PRINCIPAL
   ================================================ */
-- Crea la base de datos solo si no existe
IF DB_ID(N'XNFLFantasyDB') IS NULL
BEGIN
  CREATE DATABASE XNFLFantasyDB;
END
GO
USE XNFLFantasyDB;
GO


/* ================================================
   CREACION DE ESQUEMAS
   ================================================ */
-- Crea los esquemas principales para organizar las tablas
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'auth')   EXEC('CREATE SCHEMA auth AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'league') EXEC('CREATE SCHEMA league AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'ref')    EXEC('CREATE SCHEMA ref AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'scoring')EXEC('CREATE SCHEMA scoring AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'audit')  EXEC('CREATE SCHEMA audit AUTHORIZATION dbo;');
GO


/* ================================================
   ROLES Y PERMISOS
   ================================================ */
-- Crea un rol de ejecucion con privilegios minimos
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'app_executor')
    CREATE ROLE app_executor AUTHORIZATION dbo;
GO

/* ================================================
   MODULO DE AUTENTICACION (auth)
   ================================================ */
-- Tabla de roles del sistema (ADMIN, USER, BRAND_MANAGER)
IF OBJECT_ID('auth.SystemRole','U') IS NULL
BEGIN
  CREATE TABLE auth.SystemRole(
    RoleCode NVARCHAR(20) NOT NULL CONSTRAINT PK_SystemRole PRIMARY KEY,
    Display NVARCHAR(40) NOT NULL,
    Description NVARCHAR(200) NULL
  );
END
GO

/* ================================================
   MODULO DE AUTENTICACION (auth)
   ================================================ */
-- Tabla principal de usuarios del sistema
IF OBJECT_ID('auth.UserAccount','U') IS NULL
BEGIN
  CREATE TABLE auth.UserAccount(
    UserID INT IDENTITY(1,1) CONSTRAINT PK_UserAccount PRIMARY KEY,
    Email NVARCHAR(50) NOT NULL CONSTRAINT UQ_UserAccount_Email UNIQUE,
    PasswordHash VARBINARY(64) NOT NULL,
    PasswordSalt VARBINARY(16) NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Alias NVARCHAR(50) NULL,
    SystemRoleCode NVARCHAR(20) NOT NULL CONSTRAINT DF_UserAccount_SystemRole DEFAULT('USER'),
    LanguageCode NVARCHAR(10) NOT NULL CONSTRAINT DF_UserAccount_Lang DEFAULT('en'),
    ProfileImageUrl NVARCHAR(400) NULL,
    ProfileImageWidth SMALLINT NULL,
    ProfileImageHeight SMALLINT NULL,
    ProfileImageBytes INT NULL,
    AccountStatus TINYINT NOT NULL CONSTRAINT DF_UserAccount_Status DEFAULT(1),
    FailedLoginCount SMALLINT NOT NULL CONSTRAINT DF_UserAccount_Fails DEFAULT(0),
    LockedUntil DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserAccount_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserAccount_UpdatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT CK_UserAccount_ImageDims CHECK (
      (ProfileImageWidth IS NULL OR (ProfileImageWidth BETWEEN 300 AND 1024)) AND
      (ProfileImageHeight IS NULL OR (ProfileImageHeight BETWEEN 300 AND 1024))
    ),
    CONSTRAINT CK_UserAccount_ImageSize CHECK (ProfileImageBytes IS NULL OR ProfileImageBytes <= 5242880)
  );
END
GO

-- Indices de busqueda sobre alias y estado
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserAccount_Alias' AND object_id=OBJECT_ID('auth.UserAccount'))
  CREATE NONCLUSTERED INDEX IX_UserAccount_Alias ON auth.UserAccount(Alias);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserAccount_Status' AND object_id=OBJECT_ID('auth.UserAccount'))
  CREATE NONCLUSTERED INDEX IX_UserAccount_Status ON auth.UserAccount(AccountStatus);
GO
-- Vincula usuario con rol del sistema
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_UserAccount_SystemRole')
  ALTER TABLE auth.UserAccount ADD CONSTRAINT FK_UserAccount_SystemRole FOREIGN KEY(SystemRoleCode) REFERENCES auth.SystemRole(RoleCode) ON DELETE NO ACTION;
GO
-- Indice de busqueda sobre rol del sistema
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserAccount_SystemRole' AND object_id=OBJECT_ID('auth.UserAccount'))
  CREATE NONCLUSTERED INDEX IX_UserAccount_SystemRole ON auth.UserAccount(SystemRoleCode);
GO


-- Registra sesiones activas de usuario
IF OBJECT_ID('auth.Session','U') IS NULL
BEGIN
  CREATE TABLE auth.Session(
    SessionID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Session PRIMARY KEY DEFAULT NEWID(),
    UserID INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Session_CreatedAt DEFAULT(SYSUTCDATETIME()),
    LastActivityAt DATETIME2(0) NOT NULL CONSTRAINT DF_Session_LastActivity DEFAULT(SYSUTCDATETIME()),
    ExpiresAt DATETIME2(0) NOT NULL,
    IsValid BIT NOT NULL CONSTRAINT DF_Session_IsValid DEFAULT(1),
    Ip NVARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL
  );
END
GO

-- Vincula sesiones con usuario y agrega indices de vigencia
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Session_User')
  ALTER TABLE auth.Session ADD CONSTRAINT FK_Session_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Session_User_Validity' AND object_id=OBJECT_ID('auth.Session'))
  CREATE NONCLUSTERED INDEX IX_Session_User_Validity ON auth.Session(UserID, IsValid, ExpiresAt);
GO


-- Registra intentos de inicio de sesion
IF OBJECT_ID('auth.LoginAttempt','U') IS NULL
BEGIN
  CREATE TABLE auth.LoginAttempt(
    LoginAttemptID BIGINT IDENTITY(1,1) CONSTRAINT PK_LoginAttempt PRIMARY KEY,
    UserID INT NULL,
    Email NVARCHAR(50) NOT NULL,
    AttemptedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LoginAttempt_At DEFAULT(SYSUTCDATETIME()),
    Success BIT NOT NULL,
    Ip NVARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LoginAttempt_User')
  ALTER TABLE auth.LoginAttempt ADD CONSTRAINT FK_LoginAttempt_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LoginAttempt_User_At' AND object_id=OBJECT_ID('auth.LoginAttempt'))
  CREATE NONCLUSTERED INDEX IX_LoginAttempt_User_At ON auth.LoginAttempt(UserID, AttemptedAt DESC);
GO


-- Solicitudes de restablecimiento de contrasena
IF OBJECT_ID('auth.PasswordResetRequest','U') IS NULL
BEGIN
  CREATE TABLE auth.PasswordResetRequest(
    ResetID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordReset PRIMARY KEY DEFAULT NEWID(),
    UserID INT NOT NULL,
    Token NVARCHAR(100) NOT NULL CONSTRAINT UQ_PasswordReset_Token UNIQUE,
    RequestedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PasswordReset_ReqAt DEFAULT(SYSUTCDATETIME()),
    ExpiresAt DATETIME2(0) NOT NULL,
    UsedAt DATETIME2(0) NULL,
    FromIp NVARCHAR(45) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PasswordReset_User')
  ALTER TABLE auth.PasswordResetRequest ADD CONSTRAINT FK_PasswordReset_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO


-- Registro historico de cambios de perfil de usuario
IF OBJECT_ID('auth.ProfileChangeLog','U') IS NULL
BEGIN
  CREATE TABLE auth.ProfileChangeLog(
    ChangeID BIGINT IDENTITY(1,1) CONSTRAINT PK_ProfileChangeLog PRIMARY KEY,
    UserID INT NOT NULL,
    ChangedByUserID INT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(1000) NULL,
    NewValue NVARCHAR(1000) NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProfileChangeLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp NVARCHAR(45) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ProfileChangeLog_User')
  ALTER TABLE auth.ProfileChangeLog ADD CONSTRAINT FK_ProfileChangeLog_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ProfileChangeLog_ByUser')
  ALTER TABLE auth.ProfileChangeLog ADD CONSTRAINT FK_ProfileChangeLog_ByUser FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ProfileChangeLog_User_At' AND object_id=OBJECT_ID('auth.ProfileChangeLog'))
  CREATE NONCLUSTERED INDEX IX_ProfileChangeLog_User_At ON auth.ProfileChangeLog(UserID, ChangedAt DESC);
GO


-- Registro historico de cambios de rol del sistema
IF OBJECT_ID('auth.SystemRoleChangeLog','U') IS NULL
BEGIN
  CREATE TABLE auth.SystemRoleChangeLog(
    ChangeID BIGINT IDENTITY(1,1) CONSTRAINT PK_SystemRoleChangeLog PRIMARY KEY,
    UserID INT NOT NULL,
    ChangedByUserID INT NOT NULL,
    OldRoleCode NVARCHAR(20) NOT NULL,
    NewRoleCode NVARCHAR(20) NOT NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SystemRoleChangeLog_At DEFAULT(SYSUTCDATETIME()),
    Reason NVARCHAR(300) NULL,
    SourceIp NVARCHAR(45) NULL
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_SystemRoleChangeLog_User')
  ALTER TABLE auth.SystemRoleChangeLog ADD CONSTRAINT FK_SystemRoleChangeLog_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_SystemRoleChangeLog_ByUser')
  ALTER TABLE auth.SystemRoleChangeLog ADD CONSTRAINT FK_SystemRoleChangeLog_ByUser FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SystemRoleChangeLog_User_At' AND object_id=OBJECT_ID('auth.SystemRoleChangeLog'))
  CREATE NONCLUSTERED INDEX IX_SystemRoleChangeLog_User_At ON auth.SystemRoleChangeLog(UserID, ChangedAt DESC);
GO


/* ================================================
   MODULO DE REFERENCIAS (ref)
   ================================================ */
-- Roles permitidos dentro de una liga
IF OBJECT_ID('ref.LeagueRole','U') IS NULL
BEGIN
  CREATE TABLE ref.LeagueRole(
    RoleCode NVARCHAR(20) NOT NULL CONSTRAINT PK_LeagueRole PRIMARY KEY,
    Display NVARCHAR(40) NOT NULL
  );
END
GO

-- Formatos de posicion configurables
IF OBJECT_ID('ref.PositionFormat','U') IS NULL
BEGIN
  CREATE TABLE ref.PositionFormat(
    PositionFormatID INT IDENTITY(1,1) CONSTRAINT PK_PositionFormat PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_PositionFormat_Name UNIQUE,
    Description NVARCHAR(300) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PositionFormat_CreatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO

-- Estructura de slots para posiciones por formato
IF OBJECT_ID('ref.PositionSlot','U') IS NULL
BEGIN
  CREATE TABLE ref.PositionSlot(
    PositionFormatID INT NOT NULL,
    PositionCode NVARCHAR(20) NOT NULL,
    SlotCount TINYINT NOT NULL,
    PointsAllowed BIT NOT NULL CONSTRAINT DF_PositionSlot_PointsAllowed DEFAULT(1),
    CONSTRAINT PK_PositionSlot PRIMARY KEY(PositionFormatID, PositionCode)
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PositionSlot_Format')
  ALTER TABLE ref.PositionSlot ADD CONSTRAINT FK_PositionSlot_Format FOREIGN KEY(PositionFormatID) REFERENCES ref.PositionFormat(PositionFormatID) ON DELETE CASCADE;
GO

-- Equipos de la NFL con metadata visual
IF OBJECT_ID('ref.NFLTeam','U') IS NULL
BEGIN
  CREATE TABLE ref.NFLTeam(
    NFLTeamID INT IDENTITY(1,1) CONSTRAINT PK_NFLTeam PRIMARY KEY,
    TeamName NVARCHAR(100) NOT NULL CONSTRAINT UQ_NFLTeam_Name UNIQUE,
    City NVARCHAR(100) NOT NULL,
    TeamImageUrl NVARCHAR(400) NULL,
    TeamImageWidth SMALLINT NULL,
    TeamImageHeight SMALLINT NULL,
    TeamImageBytes INT NULL,
    ThumbnailUrl NVARCHAR(400) NULL,
    ThumbnailWidth SMALLINT NULL,
    ThumbnailHeight SMALLINT NULL,
    ThumbnailBytes INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_NFLTeam_IsActive DEFAULT(1),
    CreatedByUserID INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NFLTeam_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedByUserID INT NULL,
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NFLTeam_UpdatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO

-- Relaciones de auditoria de equipos NFL
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLTeam_Creator')
  ALTER TABLE ref.NFLTeam ADD CONSTRAINT FK_NFLTeam_Creator FOREIGN KEY(CreatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLTeam_Updater')
  ALTER TABLE ref.NFLTeam ADD CONSTRAINT FK_NFLTeam_Updater FOREIGN KEY(UpdatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLTeam_IsActive' AND object_id=OBJECT_ID('ref.NFLTeam'))
  CREATE NONCLUSTERED INDEX IX_NFLTeam_IsActive ON ref.NFLTeam(IsActive);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLTeam_City' AND object_id=OBJECT_ID('ref.NFLTeam'))
  CREATE NONCLUSTERED INDEX IX_NFLTeam_City ON ref.NFLTeam(City);
GO

-- Registro de cambios en datos de equipos NFL
IF OBJECT_ID('ref.NFLTeamChangeLog','U') IS NULL
BEGIN
  CREATE TABLE ref.NFLTeamChangeLog(
    ChangeID BIGINT IDENTITY(1,1) CONSTRAINT PK_NFLTeamChangeLog PRIMARY KEY,
    NFLTeamID INT NOT NULL,
    ChangedByUserID INT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(1000) NULL,
    NewValue NVARCHAR(1000) NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NFLTeamChangeLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp NVARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLTeamChangeLog_Team')
  ALTER TABLE ref.NFLTeamChangeLog ADD CONSTRAINT FK_NFLTeamChangeLog_Team FOREIGN KEY(NFLTeamID) REFERENCES ref.NFLTeam(NFLTeamID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLTeamChangeLog_User')
  ALTER TABLE ref.NFLTeamChangeLog ADD CONSTRAINT FK_NFLTeamChangeLog_User FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLTeamChangeLog_Team_At' AND object_id=OBJECT_ID('ref.NFLTeamChangeLog'))
  CREATE NONCLUSTERED INDEX IX_NFLTeamChangeLog_Team_At ON ref.NFLTeamChangeLog(NFLTeamID, ChangedAt DESC);
GO


/* ================================================
   MODULO DE PUNTAJE (scoring)
   ================================================ */
-- Esquema de reglas de puntuacion
IF OBJECT_ID('scoring.ScoringSchema','U') IS NULL
BEGIN
  CREATE TABLE scoring.ScoringSchema(
    ScoringSchemaID INT IDENTITY(1,1) CONSTRAINT PK_ScoringSchema PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Version INT NOT NULL CONSTRAINT DF_ScoringSchema_Version DEFAULT(1),
    IsTemplate BIT NOT NULL CONSTRAINT DF_ScoringSchema_IsTemplate DEFAULT(1),
    Description NVARCHAR(300) NULL,
    CreatedByUserID INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ScoringSchema_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT UQ_ScoringSchema_NameVer UNIQUE(Name, Version)
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ScoringSchema_Creator')
  ALTER TABLE scoring.ScoringSchema ADD CONSTRAINT FK_ScoringSchema_Creator FOREIGN KEY(CreatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO

-- Reglas individuales dentro de un esquema de puntuacion
IF OBJECT_ID('scoring.ScoringRule','U') IS NULL
BEGIN
  CREATE TABLE scoring.ScoringRule(
    ScoringSchemaID INT NOT NULL,
    MetricCode NVARCHAR(50) NOT NULL,
    PointsPerUnit DECIMAL(9,4) NULL,
    Unit NVARCHAR(20) NULL,
    UnitValue INT NULL,
    FlatPoints DECIMAL(9,4) NULL,
    CONSTRAINT PK_ScoringRule PRIMARY KEY(ScoringSchemaID, MetricCode),
    CONSTRAINT CK_ScoringRule_Logic CHECK(
      (FlatPoints IS NOT NULL) OR (PointsPerUnit IS NOT NULL AND Unit IS NOT NULL AND UnitValue IS NOT NULL)
    )
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ScoringRule_Schema')
  ALTER TABLE scoring.ScoringRule ADD CONSTRAINT FK_ScoringRule_Schema FOREIGN KEY(ScoringSchemaID) REFERENCES scoring.ScoringSchema(ScoringSchemaID) ON DELETE CASCADE;
GO


/* ================================================
   MODULO DE LIGAS (league)
   ================================================ */
-- Temporadas de juego
IF OBJECT_ID('league.Season','U') IS NULL
BEGIN
  CREATE TABLE league.Season(
    SeasonID INT IDENTITY(1,1) CONSTRAINT PK_Season PRIMARY KEY,
    Label NVARCHAR(100) NOT NULL CONSTRAINT UQ_Season_Label UNIQUE,
    Year INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsCurrent BIT NOT NULL CONSTRAINT DF_Season_IsCurrent DEFAULT(0),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Season_CreatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_Season_IsCurrent' AND object_id=OBJECT_ID('league.Season'))
  CREATE UNIQUE INDEX UQ_Season_IsCurrent ON league.Season(IsCurrent) WHERE IsCurrent = 1;
GO

-- 2) Tabla de semanas por temporada
IF OBJECT_ID('league.SeasonWeek','U') IS NULL
BEGIN
  CREATE TABLE league.SeasonWeek(
    SeasonID  INT      NOT NULL,
    WeekNumber TINYINT NOT NULL,
    StartDate DATE     NOT NULL,
    EndDate   DATE     NOT NULL,
    CONSTRAINT PK_SeasonWeek PRIMARY KEY(SeasonID, WeekNumber),
    CONSTRAINT CK_SeasonWeek_Dates CHECK (EndDate >= StartDate)
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_SeasonWeek_Season')
  ALTER TABLE league.SeasonWeek
    ADD CONSTRAINT FK_SeasonWeek_Season
      FOREIGN KEY(SeasonID) REFERENCES league.Season(SeasonID) ON DELETE CASCADE;
GO

-- Configuracion general de ligas
IF OBJECT_ID('league.League','U') IS NULL
BEGIN
  CREATE TABLE league.League(
    LeagueID INT IDENTITY(1,1) CONSTRAINT PK_League PRIMARY KEY,
    SeasonID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    TeamSlots TINYINT NOT NULL,
    LeaguePasswordHash VARBINARY(64) NOT NULL,
    LeaguePasswordSalt VARBINARY(16) NOT NULL,
    Status TINYINT NOT NULL CONSTRAINT DF_League_Status DEFAULT(0),
    AllowDecimals BIT NOT NULL CONSTRAINT DF_League_AllowDecimals DEFAULT(1),
    PlayoffTeams TINYINT NOT NULL CONSTRAINT CK_League_PlayoffTeams CHECK (PlayoffTeams IN (4,6)),
    TradeDeadlineEnabled BIT NOT NULL CONSTRAINT DF_League_TDEnabled DEFAULT(0),
    TradeDeadlineDate DATE NULL,
    MaxRosterChangesPerTeam INT NULL CONSTRAINT CK_League_MaxRosterChanges CHECK (MaxRosterChangesPerTeam BETWEEN 1 AND 100 OR MaxRosterChangesPerTeam IS NULL),
    MaxFreeAgentAddsPerTeam INT NULL CONSTRAINT CK_League_MaxFA CHECK (MaxFreeAgentAddsPerTeam BETWEEN 1 AND 100 OR MaxFreeAgentAddsPerTeam IS NULL),
    PositionFormatID INT NOT NULL,
    ScoringSchemaID INT NOT NULL,
    CreatedByUserID INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_League_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_League_UpdatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT CK_League_TeamSlots CHECK (TeamSlots IN (4,6,8,10,12,14,16,18,20)),
    CONSTRAINT UQ_League_Season_Name UNIQUE(SeasonID, Name)
  );
END
GO

-- Llaves foraneas para relacionar liga con temporada y esquemas
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_League_Season')
  ALTER TABLE league.League ADD CONSTRAINT FK_League_Season FOREIGN KEY(SeasonID) REFERENCES league.Season(SeasonID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_League_PositionFormat')
  ALTER TABLE league.League ADD CONSTRAINT FK_League_PositionFormat FOREIGN KEY(PositionFormatID) REFERENCES ref.PositionFormat(PositionFormatID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_League_ScoringSchema')
  ALTER TABLE league.League ADD CONSTRAINT FK_League_ScoringSchema FOREIGN KEY(ScoringSchemaID) REFERENCES scoring.ScoringSchema(ScoringSchemaID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_League_Creator')
  ALTER TABLE league.League ADD CONSTRAINT FK_League_Creator FOREIGN KEY(CreatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

-- Indices para filtrado de estado y temporada
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_League_Status' AND object_id=OBJECT_ID('league.League'))
  CREATE NONCLUSTERED INDEX IX_League_Status ON league.League(Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_League_Season' AND object_id=OBJECT_ID('league.League'))
  CREATE NONCLUSTERED INDEX IX_League_Season ON league.League(SeasonID);
GO

-- Miembros asociados a una liga
IF OBJECT_ID('league.LeagueMember','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueMember(
    LeagueID INT NOT NULL,
    UserID INT NOT NULL,
    RoleCode NVARCHAR(20) NOT NULL,
    JoinedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueMember_JoinedAt DEFAULT(SYSUTCDATETIME()),
    LeftAt DATETIME2(0) NULL,
    CONSTRAINT PK_LeagueMember PRIMARY KEY(LeagueID, UserID)
  );
END
GO

-- Llaves y restricciones de miembros de liga
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_League')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_League FOREIGN KEY(LeagueID) REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_User')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_Role')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_Role FOREIGN KEY(RoleCode) REFERENCES ref.LeagueRole(RoleCode) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_LeagueMember_UniqueCommissioner' AND object_id=OBJECT_ID('league.LeagueMember'))
  CREATE UNIQUE INDEX UQ_LeagueMember_UniqueCommissioner ON league.LeagueMember(LeagueID) WHERE RoleCode = 'COMMISSIONER';
GO


-- Equipos creados dentro de una liga
IF OBJECT_ID('league.Team','U') IS NOT NULL
  DROP TABLE league.Team;
GO

CREATE TABLE league.Team(
  TeamID INT IDENTITY(1,1) CONSTRAINT PK_Team PRIMARY KEY,
  LeagueID INT NOT NULL,
  OwnerUserID INT NOT NULL,
  TeamName NVARCHAR(100) NOT NULL,
  TeamImageUrl NVARCHAR(400) NULL,
  TeamImageWidth SMALLINT NULL,
  TeamImageHeight SMALLINT NULL,
  TeamImageBytes INT NULL,
  ThumbnailUrl NVARCHAR(400) NULL,
  ThumbnailWidth SMALLINT NULL,
  ThumbnailHeight SMALLINT NULL,
  ThumbnailBytes INT NULL,
  IsActive BIT NOT NULL CONSTRAINT DF_Team_IsActive DEFAULT(1),
  CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Team_CreatedAt DEFAULT(SYSUTCDATETIME()),
  UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Team_UpdatedAt DEFAULT(SYSUTCDATETIME()),
  CONSTRAINT UQ_Team_League_TeamName UNIQUE(LeagueID, TeamName),
  CONSTRAINT UQ_Team_League_Owner UNIQUE(LeagueID, OwnerUserID),
  CONSTRAINT CK_Team_ImageDims CHECK (
    (TeamImageWidth IS NULL OR (TeamImageWidth BETWEEN 300 AND 1024)) AND
    (TeamImageHeight IS NULL OR (TeamImageHeight BETWEEN 300 AND 1024))
  ),
  CONSTRAINT CK_Team_ImageSize CHECK (TeamImageBytes IS NULL OR TeamImageBytes <= 5242880),
  CONSTRAINT CK_Team_ThumbnailDims CHECK (
    (ThumbnailWidth IS NULL OR (ThumbnailWidth BETWEEN 300 AND 1024)) AND
    (ThumbnailHeight IS NULL OR (ThumbnailHeight BETWEEN 300 AND 1024))
  ),
  CONSTRAINT CK_Team_ThumbnailSize CHECK (ThumbnailBytes IS NULL OR ThumbnailBytes <= 5242880)
);
GO

-- Llaves y restricciones para equipos
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Team_League')
  ALTER TABLE league.Team ADD CONSTRAINT FK_Team_League FOREIGN KEY(LeagueID) REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Team_Owner')
  ALTER TABLE league.Team ADD CONSTRAINT FK_Team_Owner FOREIGN KEY(OwnerUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Team_IsActive' AND object_id=OBJECT_ID('league.Team'))
  CREATE NONCLUSTERED INDEX IX_Team_IsActive ON league.Team(IsActive);
GO


-- Jugadores de la liga y su estado
IF OBJECT_ID('league.Player','U') IS NULL
BEGIN
  CREATE TABLE league.Player(
    PlayerID INT IDENTITY(1,1) CONSTRAINT PK_Player PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    FullName AS (FirstName + N' ' + LastName) PERSISTED,
    Position NVARCHAR(20) NOT NULL,
    NFLTeamID INT NULL,
    InjuryStatus NVARCHAR(50) NULL,
    InjuryDescription NVARCHAR(300) NULL,
    PhotoUrl NVARCHAR(400) NULL,
    PhotoThumbnailUrl NVARCHAR(400) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Player_IsActive DEFAULT(1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Player_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Player_UpdatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Player_NFLTeam')
  ALTER TABLE league.Player ADD CONSTRAINT FK_Player_NFLTeam FOREIGN KEY(NFLTeamID) REFERENCES ref.NFLTeam(NFLTeamID) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Player_Position' AND object_id=OBJECT_ID('league.Player'))
  CREATE NONCLUSTERED INDEX IX_Player_Position ON league.Player(Position);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Player_NFLTeam' AND object_id=OBJECT_ID('league.Player'))
  CREATE NONCLUSTERED INDEX IX_Player_NFLTeam ON league.Player(NFLTeamID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Player_LastName' AND object_id=OBJECT_ID('league.Player'))
  CREATE NONCLUSTERED INDEX IX_Player_LastName ON league.Player(LastName);
GO


-- Relacion entre equipos y jugadores (roster)
IF OBJECT_ID('league.TeamRoster','U') IS NULL
BEGIN
  CREATE TABLE league.TeamRoster(
    RosterID BIGINT IDENTITY(1,1) CONSTRAINT PK_TeamRoster PRIMARY KEY,
    TeamID INT NOT NULL,
    PlayerID INT NOT NULL,
    AcquisitionType NVARCHAR(20) NOT NULL,
    AcquisitionDate DATETIME2(0) NOT NULL CONSTRAINT DF_TeamRoster_AcqDate DEFAULT(SYSUTCDATETIME()),
    IsActive BIT NOT NULL CONSTRAINT DF_TeamRoster_IsActive DEFAULT(1),
    DroppedDate DATETIME2(0) NULL,
    AddedByUserID INT NOT NULL,
    CONSTRAINT UQ_TeamRoster_Team_Player UNIQUE(TeamID, PlayerID),
    CONSTRAINT CK_TeamRoster_AcqType CHECK (AcquisitionType IN (N'Draft',N'Trade',N'FreeAgent',N'Waiver'))
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_TeamRoster_Team')
  ALTER TABLE league.TeamRoster ADD CONSTRAINT FK_TeamRoster_Team FOREIGN KEY(TeamID) REFERENCES league.Team(TeamID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_TeamRoster_Player')
  ALTER TABLE league.TeamRoster ADD CONSTRAINT FK_TeamRoster_Player FOREIGN KEY(PlayerID) REFERENCES league.Player(PlayerID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_TeamRoster_AddedBy')
  ALTER TABLE league.TeamRoster ADD CONSTRAINT FK_TeamRoster_AddedBy FOREIGN KEY(AddedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TeamRoster_Team_Active' AND object_id=OBJECT_ID('league.TeamRoster'))
  CREATE NONCLUSTERED INDEX IX_TeamRoster_Team_Active ON league.TeamRoster(TeamID, IsActive);
GO


-- Registro de cambios en equipos
IF OBJECT_ID('league.TeamChangeLog','U') IS NULL
BEGIN
  CREATE TABLE league.TeamChangeLog(
    ChangeID BIGINT IDENTITY(1,1) CONSTRAINT PK_TeamChangeLog PRIMARY KEY,
    TeamID INT NOT NULL,
    ChangedByUserID INT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(1000) NULL,
    NewValue NVARCHAR(1000) NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TeamChangeLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp NVARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_TeamChangeLog_Team')
  ALTER TABLE league.TeamChangeLog ADD CONSTRAINT FK_TeamChangeLog_Team FOREIGN KEY(TeamID) REFERENCES league.Team(TeamID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_TeamChangeLog_User')
  ALTER TABLE league.TeamChangeLog ADD CONSTRAINT FK_TeamChangeLog_User FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TeamChangeLog_Team_At' AND object_id=OBJECT_ID('league.TeamChangeLog'))
  CREATE NONCLUSTERED INDEX IX_TeamChangeLog_Team_At ON league.TeamChangeLog(TeamID, ChangedAt DESC);
GO

-- Registro de partidos oficiales de la NFL asociados a la temporada
IF OBJECT_ID('league.NFLGame','U') IS NULL
BEGIN
  CREATE TABLE league.NFLGame(
    NFLGameID INT IDENTITY(1,1) CONSTRAINT PK_NFLGame PRIMARY KEY,
    SeasonID INT NOT NULL,
    Week TINYINT NOT NULL,
    HomeTeamID INT NOT NULL,
    AwayTeamID INT NOT NULL,
    GameDate DATE NOT NULL,
    GameTime TIME(0) NULL,
    GameStatus NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NFLGame_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NFLGame_UpdatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT CK_NFLGame_Teams CHECK (HomeTeamID <> AwayTeamID),
    CONSTRAINT CK_NFLGame_Week CHECK (Week BETWEEN 1 AND 22),
    CONSTRAINT CK_NFLGame_Status CHECK (GameStatus IN (N'Scheduled',N'InProgress',N'Final',N'Postponed',N'Cancelled'))
  );
END
GO

-- Llaves foraneas y relaciones de los partidos NFL
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLGame_Season')
  ALTER TABLE league.NFLGame ADD CONSTRAINT FK_NFLGame_Season FOREIGN KEY(SeasonID) REFERENCES league.Season(SeasonID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLGame_HomeTeam')
  ALTER TABLE league.NFLGame ADD CONSTRAINT FK_NFLGame_HomeTeam FOREIGN KEY(HomeTeamID) REFERENCES ref.NFLTeam(NFLTeamID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_NFLGame_AwayTeam')
  ALTER TABLE league.NFLGame ADD CONSTRAINT FK_NFLGame_AwayTeam FOREIGN KEY(AwayTeamID) REFERENCES ref.NFLTeam(NFLTeamID) ON DELETE NO ACTION;
GO

-- Indices de optimizacion por temporada, equipos y semana
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLGame_Season_Week' AND object_id=OBJECT_ID('league.NFLGame'))
  CREATE NONCLUSTERED INDEX IX_NFLGame_Season_Week ON league.NFLGame(SeasonID, Week);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLGame_HomeTeam' AND object_id=OBJECT_ID('league.NFLGame'))
  CREATE NONCLUSTERED INDEX IX_NFLGame_HomeTeam ON league.NFLGame(HomeTeamID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NFLGame_AwayTeam' AND object_id=OBJECT_ID('league.NFLGame'))
  CREATE NONCLUSTERED INDEX IX_NFLGame_AwayTeam ON league.NFLGame(AwayTeamID);
GO


-- Historial de cambios en el estado de las ligas
IF OBJECT_ID('league.LeagueStatusHistory','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueStatusHistory(
    StatusHistoryID BIGINT IDENTITY(1,1) CONSTRAINT PK_LeagueStatusHistory PRIMARY KEY,
    LeagueID INT NOT NULL,
    OldStatus TINYINT NOT NULL,
    NewStatus TINYINT NOT NULL,
    ChangedByUserID INT NOT NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueStatusHistory_At DEFAULT(SYSUTCDATETIME()),
    Reason NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueStatus_League')
  ALTER TABLE league.LeagueStatusHistory ADD CONSTRAINT FK_LeagueStatus_League FOREIGN KEY(LeagueID) REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueStatus_User')
  ALTER TABLE league.LeagueStatusHistory ADD CONSTRAINT FK_LeagueStatus_User FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LeagueStatus_League_At' AND object_id=OBJECT_ID('league.LeagueStatusHistory'))
  CREATE NONCLUSTERED INDEX IX_LeagueStatus_League_At ON league.LeagueStatusHistory(LeagueID, ChangedAt DESC);
GO


-- Registro historico de cambios de configuracion de ligas
IF OBJECT_ID('league.LeagueConfigHistory','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueConfigHistory(
    ConfigHistoryID BIGINT IDENTITY(1,1) CONSTRAINT PK_LeagueConfigHistory PRIMARY KEY,
    LeagueID INT NOT NULL,
    ChangedByUserID INT NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(1000) NULL,
    NewValue NVARCHAR(1000) NULL,
    ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueConfigHistory_At DEFAULT(SYSUTCDATETIME())
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueConfig_League')
  ALTER TABLE league.LeagueConfigHistory ADD CONSTRAINT FK_LeagueConfig_League FOREIGN KEY(LeagueID) REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueConfig_User')
  ALTER TABLE league.LeagueConfigHistory ADD CONSTRAINT FK_LeagueConfig_User FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LeagueConfig_League_At' AND object_id=OBJECT_ID('league.LeagueConfigHistory'))
  CREATE NONCLUSTERED INDEX IX_LeagueConfig_League_At ON league.LeagueConfigHistory(LeagueID, ChangedAt DESC);
GO


/* ================================================
   MODULO DE AUDITORIA (audit)
   ================================================ */
-- Registro centralizado de acciones de usuario
IF OBJECT_ID('audit.UserActionLog','U') IS NULL
BEGIN
  CREATE TABLE audit.UserActionLog(
    ActionLogID BIGINT IDENTITY(1,1) CONSTRAINT PK_UserActionLog PRIMARY KEY,
    ActorUserID INT NULL,
    ImpersonatedByUserID INT NULL,
    EntityType NVARCHAR(50) NOT NULL,
    EntityID NVARCHAR(50) NOT NULL,
    ActionCode NVARCHAR(50) NOT NULL,
    ActionAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserActionLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp NVARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL,
    Details NVARCHAR(MAX) NULL
  );
END
GO

-- Relaciones de auditoria con usuarios
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_UserActionLog_Actor')
  ALTER TABLE audit.UserActionLog ADD CONSTRAINT FK_UserActionLog_Actor FOREIGN KEY(ActorUserID) REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_UserActionLog_Impersonator')
  ALTER TABLE audit.UserActionLog ADD CONSTRAINT FK_UserActionLog_Impersonator FOREIGN KEY(ImpersonatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

-- Indices de optimizacion de consultas de auditoria
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserActionLog_At' AND object_id=OBJECT_ID('audit.UserActionLog'))
  CREATE NONCLUSTERED INDEX IX_UserActionLog_At ON audit.UserActionLog(ActionAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserActionLog_Entity' AND object_id=OBJECT_ID('audit.UserActionLog'))
  CREATE NONCLUSTERED INDEX IX_UserActionLog_Entity ON audit.UserActionLog(EntityType, EntityID);
GO


/* ================================================
   FIN DE SCRIPT
   ================================================ */
-- Este script define toda la estructura base de la base de datos XNFLFantasyDB,
-- incluyendo autenticacion, referencias, puntuaciones, ligas y auditoria.
GO