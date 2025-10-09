/* ================================
   DB FROM SCRATCH
   ================================ */
IF DB_ID(N'XNFLFantasyDB') IS NULL
BEGIN
  CREATE DATABASE XNFLFantasyDB;
END
GO
USE XNFLFantasyDB;
GO

/* ================================
   Schemas
   ================================ */
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'auth')   EXEC('CREATE SCHEMA auth AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'league') EXEC('CREATE SCHEMA league AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'ref')    EXEC('CREATE SCHEMA ref AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'scoring')EXEC('CREATE SCHEMA scoring AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'audit')  EXEC('CREATE SCHEMA audit AUTHORIZATION dbo;');
GO

/* ================================
   Least-privilege role
   ================================ */
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'app_executor')
    CREATE ROLE app_executor AUTHORIZATION dbo;
GO

IF OBJECT_ID('auth.UserAccount','U') IS NULL
BEGIN
  CREATE TABLE auth.UserAccount(
    -- PK
    UserID               INT IDENTITY(1,1) CONSTRAINT PK_UserAccount PRIMARY KEY,

    -- Identidad / credenciales
    Email                NVARCHAR(50)  NOT NULL CONSTRAINT UQ_UserAccount_Email UNIQUE,
    -- Regla de email válido se valida en API (aquí garantizamos unicidad y longitud).
    PasswordHash         VARBINARY(64) NOT NULL,
    PasswordSalt         VARBINARY(16) NOT NULL,

    -- Perfil visible
    Name                 NVARCHAR(50)  NOT NULL,   -- (1–50)
    Alias                NVARCHAR(50)  NULL,       -- (0–50, no-único)
    LanguageCode         NVARCHAR(10)  NOT NULL CONSTRAINT DF_UserAccount_Lang DEFAULT('en'),

    -- Imagen de perfil (metadatos para validar en DB)
    ProfileImageUrl      NVARCHAR(400) NULL,
    ProfileImageWidth    SMALLINT      NULL,       -- 300–1024 px
    ProfileImageHeight   SMALLINT      NULL,       -- 300–1024 px
    ProfileImageBytes    INT           NULL,       -- <= 5 MB

    -- Estado de cuenta / seguridad
    AccountStatus        TINYINT       NOT NULL CONSTRAINT DF_UserAccount_Status DEFAULT(1), -- 1=Active, 2=Locked, 0=Disabled
    FailedLoginCount     SMALLINT      NOT NULL CONSTRAINT DF_UserAccount_Fails DEFAULT(0),
    LockedUntil          DATETIME2(0)  NULL,

    -- Timestamps
    CreatedAt            DATETIME2(0)  NOT NULL CONSTRAINT DF_UserAccount_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt            DATETIME2(0)  NOT NULL CONSTRAINT DF_UserAccount_UpdatedAt DEFAULT(SYSUTCDATETIME()),

    -- CHECKs de imagen
    CONSTRAINT CK_UserAccount_ImageDims  CHECK (
      (ProfileImageWidth  IS NULL OR (ProfileImageWidth  BETWEEN 300 AND 1024)) AND
      (ProfileImageHeight IS NULL OR (ProfileImageHeight BETWEEN 300 AND 1024))
    ),
    CONSTRAINT CK_UserAccount_ImageSize  CHECK (ProfileImageBytes IS NULL OR ProfileImageBytes <= 5242880) -- 5 * 1024 * 1024
  );
END
GO

-- Índices útiles
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserAccount_Alias' AND object_id=OBJECT_ID('auth.UserAccount'))
  CREATE NONCLUSTERED INDEX IX_UserAccount_Alias ON auth.UserAccount(Alias);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserAccount_Status' AND object_id=OBJECT_ID('auth.UserAccount'))
  CREATE NONCLUSTERED INDEX IX_UserAccount_Status ON auth.UserAccount(AccountStatus);
GO


IF OBJECT_ID('auth.Session','U') IS NULL
BEGIN
  CREATE TABLE auth.Session(
    SessionID       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Session PRIMARY KEY DEFAULT NEWID(),
    UserID          INT NOT NULL,
    CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Session_CreatedAt DEFAULT(SYSUTCDATETIME()),
    LastActivityAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Session_LastActivity DEFAULT(SYSUTCDATETIME()),
    ExpiresAt       DATETIME2(0) NOT NULL,  -- Set por SP = CreatedAt + 12h
    IsValid         BIT NOT NULL CONSTRAINT DF_Session_IsValid DEFAULT(1),
    Ip              NVARCHAR(45) NULL,
    UserAgent       NVARCHAR(300) NULL
  );
END
GO
-- FK
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Session_User')
  ALTER TABLE auth.Session
  ADD CONSTRAINT FK_Session_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO

-- Índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Session_User_Validity' AND object_id=OBJECT_ID('auth.Session'))
  CREATE NONCLUSTERED INDEX IX_Session_User_Validity ON auth.Session(UserID, IsValid, ExpiresAt);
GO


IF OBJECT_ID('auth.LoginAttempt','U') IS NULL
BEGIN
  CREATE TABLE auth.LoginAttempt(
    LoginAttemptID  BIGINT IDENTITY(1,1) CONSTRAINT PK_LoginAttempt PRIMARY KEY,
    UserID          INT NULL,                -- NULL si el email no existe
    Email           NVARCHAR(50) NOT NULL,   -- email intentado
    AttemptedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_LoginAttempt_At DEFAULT(SYSUTCDATETIME()),
    Success         BIT NOT NULL,
    Ip              NVARCHAR(45) NULL,
    UserAgent       NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LoginAttempt_User')
  ALTER TABLE auth.LoginAttempt
  ADD CONSTRAINT FK_LoginAttempt_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LoginAttempt_User_At' AND object_id=OBJECT_ID('auth.LoginAttempt'))
  CREATE NONCLUSTERED INDEX IX_LoginAttempt_User_At ON auth.LoginAttempt(UserID, AttemptedAt DESC);
GO


IF OBJECT_ID('auth.PasswordResetRequest','U') IS NULL
BEGIN
  CREATE TABLE auth.PasswordResetRequest(
    ResetID       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordReset PRIMARY KEY DEFAULT NEWID(),
    UserID        INT NOT NULL,
    Token         NVARCHAR(100) NOT NULL CONSTRAINT UQ_PasswordReset_Token UNIQUE,
    RequestedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_PasswordReset_ReqAt DEFAULT(SYSUTCDATETIME()),
    ExpiresAt     DATETIME2(0) NOT NULL,
    UsedAt        DATETIME2(0) NULL,
    FromIp        NVARCHAR(45) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PasswordReset_User')
  ALTER TABLE auth.PasswordResetRequest
  ADD CONSTRAINT FK_PasswordReset_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO


IF OBJECT_ID('auth.ProfileChangeLog','U') IS NULL
BEGIN
  CREATE TABLE auth.ProfileChangeLog(
    ChangeID        BIGINT IDENTITY(1,1) CONSTRAINT PK_ProfileChangeLog PRIMARY KEY,
    UserID          INT NOT NULL,
    ChangedByUserID INT NOT NULL,   -- puede ser el mismo UserID
    FieldName       NVARCHAR(100) NOT NULL,
    OldValue        NVARCHAR(1000) NULL,
    NewValue        NVARCHAR(1000) NULL,
    ChangedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_ProfileChangeLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp        NVARCHAR(45) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ProfileChangeLog_User')
  ALTER TABLE auth.ProfileChangeLog
  ADD CONSTRAINT FK_ProfileChangeLog_User FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ProfileChangeLog_ByUser')
  ALTER TABLE auth.ProfileChangeLog
  ADD CONSTRAINT FK_ProfileChangeLog_ByUser FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ProfileChangeLog_User_At' AND object_id=OBJECT_ID('auth.ProfileChangeLog'))
  CREATE NONCLUSTERED INDEX IX_ProfileChangeLog_User_At ON auth.ProfileChangeLog(UserID, ChangedAt DESC);
GO


IF OBJECT_ID('ref.LeagueRole','U') IS NULL
BEGIN
  CREATE TABLE ref.LeagueRole(
    RoleCode  NVARCHAR(20) NOT NULL CONSTRAINT PK_LeagueRole PRIMARY KEY, -- 'COMMISSIONER','CO_COMMISSIONER','MANAGER','SPECTATOR'
    Display   NVARCHAR(40) NOT NULL
  );
END
GO


IF OBJECT_ID('ref.PositionFormat','U') IS NULL
BEGIN
  CREATE TABLE ref.PositionFormat(
    PositionFormatID  INT IDENTITY(1,1) CONSTRAINT PK_PositionFormat PRIMARY KEY,
    Name              NVARCHAR(50) NOT NULL CONSTRAINT UQ_PositionFormat_Name UNIQUE, -- 'Default','Extremo','Detallado','Ofensivo'
    Description       NVARCHAR(300) NULL,
    CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_PositionFormat_CreatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO

IF OBJECT_ID('ref.PositionSlot','U') IS NULL
BEGIN
  CREATE TABLE ref.PositionSlot(
    PositionFormatID  INT NOT NULL,
    PositionCode      NVARCHAR(20) NOT NULL, -- 'QB','RB','WR','TE','K','DEF','DL','LB','CB','RB/WR','BENCH','IR'
    SlotCount         TINYINT NOT NULL,      -- cantidad de plazas
    CONSTRAINT PK_PositionSlot PRIMARY KEY(PositionFormatID, PositionCode)
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_PositionSlot_Format')
  ALTER TABLE ref.PositionSlot
  ADD CONSTRAINT FK_PositionSlot_Format FOREIGN KEY(PositionFormatID) REFERENCES ref.PositionFormat(PositionFormatID) ON DELETE CASCADE;
GO


IF OBJECT_ID('scoring.ScoringSchema','U') IS NULL
BEGIN
  CREATE TABLE scoring.ScoringSchema(
    ScoringSchemaID  INT IDENTITY(1,1) CONSTRAINT PK_ScoringSchema PRIMARY KEY,
    Name             NVARCHAR(50) NOT NULL,     -- 'Default','PrioridadCarrera','MaxPuntos','PrioridadDefensa'
    Version          INT NOT NULL CONSTRAINT DF_ScoringSchema_Version DEFAULT(1),
    IsTemplate       BIT NOT NULL CONSTRAINT DF_ScoringSchema_IsTemplate DEFAULT(1),
    Description      NVARCHAR(300) NULL,
    CreatedByUserID  INT NULL,
    CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_ScoringSchema_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT UQ_ScoringSchema_NameVer UNIQUE(Name, Version)
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ScoringSchema_Creator')
  ALTER TABLE scoring.ScoringSchema
  ADD CONSTRAINT FK_ScoringSchema_Creator FOREIGN KEY(CreatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO

IF OBJECT_ID('scoring.ScoringRule','U') IS NULL
BEGIN
  CREATE TABLE scoring.ScoringRule(
    ScoringSchemaID  INT NOT NULL,
    MetricCode       NVARCHAR(50) NOT NULL,     -- p.ej. 'PASS_YDS','PASS_TD','PASS_INT','RUSH_YDS','REC','SACK','FG_50_PLUS', etc.
    PointsPerUnit    DECIMAL(9,4) NULL,         -- cuando aplica por unidad
    Unit             NVARCHAR(20)  NULL,        -- 'YARD','POINT','ATTEMPT','EVENT'...
    UnitValue        INT           NULL,        -- p.ej. 25 yds por punto
    FlatPoints       DECIMAL(9,4)  NULL,        -- cuando es un puntaje plano
    CONSTRAINT PK_ScoringRule PRIMARY KEY(ScoringSchemaID, MetricCode),
    CONSTRAINT CK_ScoringRule_Logic CHECK(
      (FlatPoints IS NOT NULL) OR (PointsPerUnit IS NOT NULL AND Unit IS NOT NULL AND UnitValue IS NOT NULL)
    )
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_ScoringRule_Schema')
  ALTER TABLE scoring.ScoringRule
  ADD CONSTRAINT FK_ScoringRule_Schema FOREIGN KEY(ScoringSchemaID) REFERENCES scoring.ScoringSchema(ScoringSchemaID) ON DELETE CASCADE;
GO


IF OBJECT_ID('league.Season','U') IS NULL
BEGIN
  CREATE TABLE league.Season(
    SeasonID    INT IDENTITY(1,1) CONSTRAINT PK_Season PRIMARY KEY,
    Label       NVARCHAR(20) NOT NULL CONSTRAINT UQ_Season_Label UNIQUE, -- 'NFL 2025'
    Year        INT NOT NULL,
    StartDate   DATE NOT NULL,
    EndDate     DATE NOT NULL,
    IsCurrent   BIT  NOT NULL CONSTRAINT DF_Season_IsCurrent DEFAULT(0),
    CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_Season_CreatedAt DEFAULT(SYSUTCDATETIME())
  );
END
GO

-- Sólo una temporada con IsCurrent=1 (índice filtrado único)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_Season_IsCurrent' AND object_id=OBJECT_ID('league.Season'))
  CREATE UNIQUE INDEX UQ_Season_IsCurrent ON league.Season(IsCurrent) WHERE IsCurrent = 1;
GO


IF OBJECT_ID('league.League','U') IS NULL
BEGIN
  CREATE TABLE league.League(
    LeagueID                 INT IDENTITY(1,1) CONSTRAINT PK_League PRIMARY KEY,
    SeasonID                 INT NOT NULL,
    Name                     NVARCHAR(100) NOT NULL,
    Description              NVARCHAR(500) NULL,

    -- Capacidad
    TeamSlots                TINYINT NOT NULL,
    CONSTRAINT CK_League_TeamSlots CHECK (TeamSlots IN (4,6,8,10,12,14,16,18,20)),

    -- Seguridad de liga (misma política que cuentas: se valida en API; aquí guardamos hash+salt)
    LeaguePasswordHash       VARBINARY(64) NOT NULL,
    LeaguePasswordSalt       VARBINARY(16) NOT NULL,

    -- Estados y reglas base
    Status                   TINYINT  NOT NULL CONSTRAINT DF_League_Status DEFAULT(0), -- 0=PreDraft, 1=Active, 2=Inactive, 3=Closed
    AllowDecimals            BIT      NOT NULL CONSTRAINT DF_League_AllowDecimals DEFAULT(1),
    PlayoffTeams             TINYINT  NOT NULL CONSTRAINT CK_League_PlayoffTeams CHECK (PlayoffTeams IN (4,6)),

    -- Trade deadline / límites de movimientos
    TradeDeadlineEnabled     BIT      NOT NULL CONSTRAINT DF_League_TDEnabled DEFAULT(0),
    TradeDeadlineDate        DATE     NULL, -- Validar en SP: si Enabled=1, dentro de temporada
    MaxRosterChangesPerTeam  INT      NULL CONSTRAINT CK_League_MaxRosterChanges CHECK (MaxRosterChangesPerTeam BETWEEN 1 AND 100 OR MaxRosterChangesPerTeam IS NULL),
    MaxFreeAgentAddsPerTeam  INT      NULL CONSTRAINT CK_League_MaxFA CHECK (MaxFreeAgentAddsPerTeam BETWEEN 1 AND 100 OR MaxFreeAgentAddsPerTeam IS NULL),

    -- Defaults asignados al crear
    PositionFormatID         INT NOT NULL,
    ScoringSchemaID          INT NOT NULL,

    -- Metadata
    CreatedByUserID          INT NOT NULL,
    CreatedAt                DATETIME2(0) NOT NULL CONSTRAINT DF_League_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt                DATETIME2(0) NOT NULL CONSTRAINT DF_League_UpdatedAt DEFAULT(SYSUTCDATETIME()),

    -- Unicidad de nombre por temporada
    CONSTRAINT UQ_League_Season_Name UNIQUE(SeasonID, Name)
  );
END
GO

-- FKs
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_League_Season')
  ALTER TABLE league.League ADD CONSTRAINT FK_League_Season FOREIGN KEY(SeasonID)        REFERENCES league.Season(SeasonID) ON DELETE NO ACTION;
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

-- Índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_League_Status' AND object_id=OBJECT_ID('league.League'))
  CREATE NONCLUSTERED INDEX IX_League_Status ON league.League(Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_League_Season' AND object_id=OBJECT_ID('league.League'))
  CREATE NONCLUSTERED INDEX IX_League_Season ON league.League(SeasonID);
GO


IF OBJECT_ID('league.LeagueMember','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueMember(
    LeagueID              INT NOT NULL,
    UserID                INT NOT NULL,
    RoleCode              NVARCHAR(20) NOT NULL, -- FK a ref.LeagueRole
    IsPrimaryCommissioner BIT NOT NULL CONSTRAINT DF_LeagueMember_PrimaryComm DEFAULT(0),
    JoinedAt              DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueMember_JoinedAt DEFAULT(SYSUTCDATETIME()),
    LeftAt                DATETIME2(0) NULL,
    CONSTRAINT PK_LeagueMember PRIMARY KEY(LeagueID, UserID),

    -- Si no es COMMISSIONER, no puede marcarse como principal
    CONSTRAINT CK_LeagueMember_PrimaryConsistency CHECK(
      CASE WHEN RoleCode <> 'COMMISSIONER' AND IsPrimaryCommissioner = 1 THEN 0 ELSE 1 END = 1
    )
  );
END
GO

-- FKs
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_League')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_League FOREIGN KEY(LeagueID) REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_User')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_User FOREIGN KEY(UserID)   REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueMember_Role')
  ALTER TABLE league.LeagueMember ADD CONSTRAINT FK_LeagueMember_Role FOREIGN KEY(RoleCode) REFERENCES ref.LeagueRole(RoleCode) ON DELETE NO ACTION;
GO

-- Un comisionado principal por liga (índice filtrado único)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_LeagueMember_PrimaryComm' AND object_id=OBJECT_ID('league.LeagueMember'))
  CREATE UNIQUE INDEX UQ_LeagueMember_PrimaryComm
  ON league.LeagueMember(LeagueID)
  WHERE IsPrimaryCommissioner = 1;
GO


IF OBJECT_ID('league.Team','U') IS NULL
BEGIN
  CREATE TABLE league.Team(
    TeamID        INT IDENTITY(1,1) CONSTRAINT PK_Team PRIMARY KEY,
    LeagueID      INT NOT NULL,
    OwnerUserID   INT NOT NULL,            -- dueño del equipo (manager/comisionado)
    TeamName      NVARCHAR(50) NOT NULL,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Team_CreatedAt DEFAULT(SYSUTCDATETIME()),

    CONSTRAINT UQ_Team_League_TeamName UNIQUE(LeagueID, TeamName),
    CONSTRAINT UQ_Team_League_Owner    UNIQUE(LeagueID, OwnerUserID)
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Team_League')
  ALTER TABLE league.Team ADD CONSTRAINT FK_Team_League FOREIGN KEY(LeagueID)    REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Team_Owner')
  ALTER TABLE league.Team ADD CONSTRAINT FK_Team_Owner FOREIGN KEY(OwnerUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO


IF OBJECT_ID('league.LeagueStatusHistory','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueStatusHistory(
    StatusHistoryID  BIGINT IDENTITY(1,1) CONSTRAINT PK_LeagueStatusHistory PRIMARY KEY,
    LeagueID         INT NOT NULL,
    OldStatus        TINYINT NOT NULL,
    NewStatus        TINYINT NOT NULL,
    ChangedByUserID  INT NOT NULL,
    ChangedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueStatusHistory_At DEFAULT(SYSUTCDATETIME()),
    Reason           NVARCHAR(300) NULL
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueStatus_League')
  ALTER TABLE league.LeagueStatusHistory ADD CONSTRAINT FK_LeagueStatus_League FOREIGN KEY(LeagueID)        REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueStatus_User')
  ALTER TABLE league.LeagueStatusHistory ADD CONSTRAINT FK_LeagueStatus_User   FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LeagueStatus_League_At' AND object_id=OBJECT_ID('league.LeagueStatusHistory'))
  CREATE NONCLUSTERED INDEX IX_LeagueStatus_League_At ON league.LeagueStatusHistory(LeagueID, ChangedAt DESC);
GO


IF OBJECT_ID('league.LeagueConfigHistory','U') IS NULL
BEGIN
  CREATE TABLE league.LeagueConfigHistory(
    ConfigHistoryID  BIGINT IDENTITY(1,1) CONSTRAINT PK_LeagueConfigHistory PRIMARY KEY,
    LeagueID         INT NOT NULL,
    ChangedByUserID  INT NOT NULL,
    FieldName        NVARCHAR(100) NOT NULL,  -- p.ej. 'TeamSlots','PlayoffTeams','AllowDecimals','ScoringSchemaID', etc.
    OldValue         NVARCHAR(1000) NULL,
    NewValue         NVARCHAR(1000) NULL,
    ChangedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_LeagueConfigHistory_At DEFAULT(SYSUTCDATETIME())
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueConfig_League')
  ALTER TABLE league.LeagueConfigHistory ADD CONSTRAINT FK_LeagueConfig_League FOREIGN KEY(LeagueID)        REFERENCES league.League(LeagueID) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_LeagueConfig_User')
  ALTER TABLE league.LeagueConfigHistory ADD CONSTRAINT FK_LeagueConfig_User   FOREIGN KEY(ChangedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LeagueConfig_League_At' AND object_id=OBJECT_ID('league.LeagueConfigHistory'))
  CREATE NONCLUSTERED INDEX IX_LeagueConfig_League_At ON league.LeagueConfigHistory(LeagueID, ChangedAt DESC);
GO


IF OBJECT_ID('audit.UserActionLog','U') IS NULL
BEGIN
  CREATE TABLE audit.UserActionLog(
    ActionLogID          BIGINT IDENTITY(1,1) CONSTRAINT PK_UserActionLog PRIMARY KEY,
    ActorUserID          INT NULL,              -- NULL si público/no autenticado
    ImpersonatedByUserID INT NULL,              -- si aplica
    EntityType           NVARCHAR(50) NOT NULL, -- 'USER_PROFILE','LEAGUE','LEAGUE_MEMBER','TEAM', etc.
    EntityID             NVARCHAR(50) NOT NULL, -- ID textual para trazabilidad
    ActionCode           NVARCHAR(50) NOT NULL, -- 'CREATE','UPDATE','STATUS_CHANGE','LOGIN','LOGOUT','RESET_REQUEST', ...
    ActionAt             DATETIME2(0) NOT NULL CONSTRAINT DF_UserActionLog_At DEFAULT(SYSUTCDATETIME()),
    SourceIp             NVARCHAR(45) NULL,
    UserAgent            NVARCHAR(300) NULL,
    Details              NVARCHAR(MAX) NULL
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_UserActionLog_Actor')
  ALTER TABLE audit.UserActionLog ADD CONSTRAINT FK_UserActionLog_Actor FOREIGN KEY(ActorUserID)          REFERENCES auth.UserAccount(UserID) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_UserActionLog_Impersonator')
  ALTER TABLE audit.UserActionLog ADD CONSTRAINT FK_UserActionLog_Impersonator FOREIGN KEY(ImpersonatedByUserID) REFERENCES auth.UserAccount(UserID) ON DELETE NO ACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserActionLog_At' AND object_id=OBJECT_ID('audit.UserActionLog'))
  CREATE NONCLUSTERED INDEX IX_UserActionLog_At ON audit.UserActionLog(ActionAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserActionLog_Entity' AND object_id=OBJECT_ID('audit.UserActionLog'))
  CREATE NONCLUSTERED INDEX IX_UserActionLog_Entity ON audit.UserActionLog(EntityType, EntityID);
GO
