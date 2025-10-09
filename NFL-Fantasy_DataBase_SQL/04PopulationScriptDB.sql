USE XNFLFantasyDB;
SET NOCOUNT ON;

/* ============================================================
   1) REF: Roles de liga
   ============================================================ */
MERGE ref.LeagueRole AS T
USING (VALUES
  (N'COMMISSIONER',     N'Comisionado'),
  (N'CO_COMMISSIONER',  N'Co-Comisionado'),
  (N'MANAGER',          N'Manager'),
  (N'SPECTATOR',        N'Espectador')
) AS S(RoleCode, Display)
ON (T.RoleCode = S.RoleCode)
WHEN MATCHED AND T.Display <> S.Display THEN
  UPDATE SET T.Display = S.Display
WHEN NOT MATCHED BY TARGET THEN
  INSERT(RoleCode, Display) VALUES(S.RoleCode, S.Display);

/* ============================================================
   2) REF: PositionFormat + PositionSlot
   ============================================================ */
-- 2.1 Formatos
MERGE ref.PositionFormat AS T
USING (VALUES
  (N'Default',    N'Formato estándar ofensivo/DEF/K con banca e IR'),
  (N'Detallado',  N'Más profundidad ofensiva y banca ampliada'),
  (N'Extremo',    N'Incluye posiciones defensivas individuales (DL/LB/CB)'),
  (N'Ofensivo',   N'Ofensivo pesado con más flex y banca')
) AS S(Name, Description)
ON (T.Name = S.Name)
WHEN MATCHED AND ISNULL(T.Description,N'') <> ISNULL(S.Description,N'') THEN
  UPDATE SET T.Description = S.Description
WHEN NOT MATCHED BY TARGET THEN
  INSERT(Name, Description) VALUES(S.Name, S.Description);

-- 2.2 Slots por formato (upsert por (PositionFormatID, PositionCode))
DECLARE @Slots TABLE(PositionFormatName NVARCHAR(50), PositionCode NVARCHAR(20), SlotCount TINYINT);

-- Default
INSERT INTO @Slots VALUES
 (N'Default', N'QB', 1),(N'Default', N'RB', 2),(N'Default', N'WR', 2),
 (N'Default', N'TE', 1),(N'Default', N'RB/WR', 1),
 (N'Default', N'K', 1),(N'Default', N'DEF', 1),
 (N'Default', N'BENCH', 6),(N'Default', N'IR', 2);

-- Detallado
INSERT INTO @Slots VALUES
 (N'Detallado', N'QB', 1),(N'Detallado', N'RB', 2),(N'Detallado', N'WR', 3),
 (N'Detallado', N'TE', 1),(N'Detallado', N'RB/WR', 1),
 (N'Detallado', N'K', 1),(N'Detallado', N'DEF', 1),
 (N'Detallado', N'BENCH', 8),(N'Detallado', N'IR', 2);

-- Extremo (agrega defensivas individuales)
INSERT INTO @Slots VALUES
 (N'Extremo', N'QB', 1),(N'Extremo', N'RB', 2),(N'Extremo', N'WR', 2),
 (N'Extremo', N'TE', 1),(N'Extremo', N'RB/WR', 1),
 (N'Extremo', N'DL', 1),(N'Extremo', N'LB', 1),(N'Extremo', N'CB', 1),
 (N'Extremo', N'K', 1),(N'Extremo', N'DEF', 1),
 (N'Extremo', N'BENCH', 10),(N'Extremo', N'IR', 3);

-- Ofensivo (más flex y banca)
INSERT INTO @Slots VALUES
 (N'Ofensivo', N'QB', 1),(N'Ofensivo', N'RB', 3),(N'Ofensivo', N'WR', 3),
 (N'Ofensivo', N'TE', 1),(N'Ofensivo', N'RB/WR', 2),
 (N'Ofensivo', N'K', 1),(N'Ofensivo', N'DEF', 1),
 (N'Ofensivo', N'BENCH', 7),(N'Ofensivo', N'IR', 2);

MERGE ref.PositionSlot AS T
USING (
  SELECT pf.PositionFormatID, s.PositionCode, s.SlotCount
  FROM @Slots s
  JOIN ref.PositionFormat pf ON pf.Name = s.PositionFormatName
) AS S(PositionFormatID, PositionCode, SlotCount)
ON (T.PositionFormatID = S.PositionFormatID AND T.PositionCode = S.PositionCode)
WHEN MATCHED AND T.SlotCount <> S.SlotCount THEN
  UPDATE SET T.SlotCount = S.SlotCount
WHEN NOT MATCHED BY TARGET THEN
  INSERT(PositionFormatID, PositionCode, SlotCount)
  VALUES(S.PositionFormatID, S.PositionCode, S.SlotCount);

DECLARE @PF_Default   INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Default');
DECLARE @PF_Ofensivo  INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Ofensivo');
DECLARE @PF_Extremo   INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Extremo');

/* ============================================================
   3) SCORING: Schemas + Rules (cuatro plantillas V1)
   ============================================================ */
-- 3.1 Schemas
MERGE scoring.ScoringSchema AS T
USING (VALUES
  (N'Default',           1, N'PPR estándar con defensa y pateador'),
  (N'PrioridadCarrera',  1, N'Pondera juego terrestre y baja PPR'),
  (N'MaxPuntos',         1, N'Esquema alto en TD y yardas'),
  (N'PrioridadDefensa',  1, N'Enfatiza métricas defensivas')
) AS S(Name, Version, Description)
ON (T.Name = S.Name AND T.Version = S.Version)
WHEN MATCHED AND ISNULL(T.Description,N'') <> ISNULL(S.Description,N'') THEN
  UPDATE SET T.Description = S.Description
WHEN NOT MATCHED BY TARGET THEN
  INSERT(Name, Version, Description, IsTemplate, CreatedByUserID)
  VALUES(S.Name, S.Version, S.Description, 1, NULL);

DECLARE @SS_Default  INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'Default' AND Version=1);
DECLARE @SS_Run      INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'PrioridadCarrera' AND Version=1);
DECLARE @SS_Max      INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'MaxPuntos' AND Version=1);
DECLARE @SS_Def      INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'PrioridadDefensa' AND Version=1);

-- 3.2 Rules helper
DECLARE @Rules TABLE(
  SchemaName NVARCHAR(50), Version INT,
  MetricCode NVARCHAR(50), PointsPerUnit DECIMAL(9,4) NULL,
  Unit NVARCHAR(20) NULL, UnitValue INT NULL,
  FlatPoints DECIMAL(9,4) NULL
);

-- Default (PPR 1)
INSERT INTO @Rules VALUES
 (N'Default',1,N'PASS_YDS',1.0,N'YARD',25,NULL),
 (N'Default',1,N'PASS_TD',NULL,NULL,NULL,4),
 (N'Default',1,N'PASS_INT',NULL,NULL,NULL,-2),
 (N'Default',1,N'RUSH_YDS',1.0,N'YARD',10,NULL),
 (N'Default',1,N'RUSH_TD',NULL,NULL,NULL,6),
 (N'Default',1,N'REC',1.0,N'EVENT',1,NULL),
 (N'Default',1,N'REC_YDS',1.0,N'YARD',10,NULL),
 (N'Default',1,N'REC_TD',NULL,NULL,NULL,6),
 (N'Default',1,N'FUM_LOST',NULL,NULL,NULL,-2),
 (N'Default',1,N'K_FG_0_39',NULL,NULL,NULL,3),
 (N'Default',1,N'K_FG_40_49',NULL,NULL,NULL,4),
 (N'Default',1,N'K_FG_50_PLUS',NULL,NULL,NULL,5),
 (N'Default',1,N'K_XP',NULL,NULL,NULL,1),
 (N'Default',1,N'DEF_SACK',NULL,NULL,NULL,1),
 (N'Default',1,N'DEF_INT',NULL,NULL,NULL,2),
 (N'Default',1,N'DEF_FR',NULL,NULL,NULL,2),
 (N'Default',1,N'DEF_TD',NULL,NULL,NULL,6),
 (N'Default',1,N'DEF_SAFETY',NULL,NULL,NULL,2);

-- PrioridadCarrera (medio PPR, más yardas de carrera)
INSERT INTO @Rules VALUES
 (N'PrioridadCarrera',1,N'PASS_YDS',1.0,N'YARD',30,NULL),
 (N'PrioridadCarrera',1,N'PASS_TD',NULL,NULL,NULL,4),
 (N'PrioridadCarrera',1,N'PASS_INT',NULL,NULL,NULL,-2),
 (N'PrioridadCarrera',1,N'RUSH_YDS',1.0,N'YARD',8,NULL),
 (N'PrioridadCarrera',1,N'RUSH_TD',NULL,NULL,NULL,7),
 (N'PrioridadCarrera',1,N'REC',0.5,N'EVENT',1,NULL),
 (N'PrioridadCarrera',1,N'REC_YDS',1.0,N'YARD',12,NULL),
 (N'PrioridadCarrera',1,N'REC_TD',NULL,NULL,NULL,6),
 (N'PrioridadCarrera',1,N'FUM_LOST',NULL,NULL,NULL,-2),
 (N'PrioridadCarrera',1,N'K_FG_0_39',NULL,NULL,NULL,3),
 (N'PrioridadCarrera',1,N'K_FG_40_49',NULL,NULL,NULL,4),
 (N'PrioridadCarrera',1,N'K_FG_50_PLUS',NULL,NULL,NULL,5),
 (N'PrioridadCarrera',1,N'K_XP',NULL,NULL,NULL,1),
 (N'PrioridadCarrera',1,N'DEF_SACK',NULL,NULL,NULL,1),
 (N'PrioridadCarrera',1,N'DEF_INT',NULL,NULL,NULL,2),
 (N'PrioridadCarrera',1,N'DEF_FR',NULL,NULL,NULL,2),
 (N'PrioridadCarrera',1,N'DEF_TD',NULL,NULL,NULL,6),
 (N'PrioridadCarrera',1,N'DEF_SAFETY',NULL,NULL,NULL,2);

-- MaxPuntos (todo un poco más alto)
INSERT INTO @Rules VALUES
 (N'MaxPuntos',1,N'PASS_YDS',1.0,N'YARD',20,NULL),
 (N'MaxPuntos',1,N'PASS_TD',NULL,NULL,NULL,6),
 (N'MaxPuntos',1,N'PASS_INT',NULL,NULL,NULL,-2),
 (N'MaxPuntos',1,N'RUSH_YDS',1.0,N'YARD',8,NULL),
 (N'MaxPuntos',1,N'RUSH_TD',NULL,NULL,NULL,8),
 (N'MaxPuntos',1,N'REC',1.0,N'EVENT',1,NULL),
 (N'MaxPuntos',1,N'REC_YDS',1.0,N'YARD',8,NULL),
 (N'MaxPuntos',1,N'REC_TD',NULL,NULL,NULL,8),
 (N'MaxPuntos',1,N'FUM_LOST',NULL,NULL,NULL,-2),
 (N'MaxPuntos',1,N'K_FG_0_39',NULL,NULL,NULL,4),
 (N'MaxPuntos',1,N'K_FG_40_49',NULL,NULL,NULL,5),
 (N'MaxPuntos',1,N'K_FG_50_PLUS',NULL,NULL,NULL,6),
 (N'MaxPuntos',1,N'K_XP',NULL,NULL,NULL,1),
 (N'MaxPuntos',1,N'DEF_SACK',NULL,NULL,NULL,1),
 (N'MaxPuntos',1,N'DEF_INT',NULL,NULL,NULL,3),
 (N'MaxPuntos',1,N'DEF_FR',NULL,NULL,NULL,3),
 (N'MaxPuntos',1,N'DEF_TD',NULL,NULL,NULL,8),
 (N'MaxPuntos',1,N'DEF_SAFETY',NULL,NULL,NULL,3);

-- PrioridadDefensa (defensa más valiosa)
INSERT INTO @Rules VALUES
 (N'PrioridadDefensa',1,N'PASS_YDS',1.0,N'YARD',30,NULL),
 (N'PrioridadDefensa',1,N'PASS_TD',NULL,NULL,NULL,4),
 (N'PrioridadDefensa',1,N'PASS_INT',NULL,NULL,NULL,-2),
 (N'PrioridadDefensa',1,N'RUSH_YDS',1.0,N'YARD',12,NULL),
 (N'PrioridadDefensa',1,N'RUSH_TD',NULL,NULL,NULL,6),
 (N'PrioridadDefensa',1,N'REC',0.0,N'EVENT',1,NULL),
 (N'PrioridadDefensa',1,N'REC_YDS',1.0,N'YARD',12,NULL),
 (N'PrioridadDefensa',1,N'REC_TD',NULL,NULL,NULL,6),
 (N'PrioridadDefensa',1,N'FUM_LOST',NULL,NULL,NULL,-3),
 (N'PrioridadDefensa',1,N'K_FG_0_39',NULL,NULL,NULL,3),
 (N'PrioridadDefensa',1,N'K_FG_40_49',NULL,NULL,NULL,4),
 (N'PrioridadDefensa',1,N'K_FG_50_PLUS',NULL,NULL,NULL,5),
 (N'PrioridadDefensa',1,N'K_XP',NULL,NULL,NULL,1),
 (N'PrioridadDefensa',1,N'DEF_SACK',NULL,NULL,NULL,2),
 (N'PrioridadDefensa',1,N'DEF_INT',NULL,NULL,NULL,3),
 (N'PrioridadDefensa',1,N'DEF_FR',NULL,NULL,NULL,3),
 (N'PrioridadDefensa',1,N'DEF_TD',NULL,NULL,NULL,8),
 (N'PrioridadDefensa',1,N'DEF_SAFETY',NULL,NULL,NULL,3);

-- Upsert de reglas
MERGE scoring.ScoringRule AS T
USING (
  SELECT ss.ScoringSchemaID, r.MetricCode, r.PointsPerUnit, r.Unit, r.UnitValue, r.FlatPoints
  FROM @Rules r
  JOIN scoring.ScoringSchema ss ON ss.Name = r.SchemaName AND ss.Version = r.Version
) AS S(ScoringSchemaID, MetricCode, PointsPerUnit, Unit, UnitValue, FlatPoints)
ON (T.ScoringSchemaID = S.ScoringSchemaID AND T.MetricCode = S.MetricCode)
WHEN MATCHED AND (
   ISNULL(T.PointsPerUnit, -9999) <> ISNULL(S.PointsPerUnit, -9999)
OR ISNULL(T.Unit,'') <> ISNULL(S.Unit,'')
OR ISNULL(T.UnitValue,-9999) <> ISNULL(S.UnitValue,-9999)
OR ISNULL(T.FlatPoints,-9999) <> ISNULL(S.FlatPoints,-9999)
) THEN
  UPDATE SET
    T.PointsPerUnit = S.PointsPerUnit,
    T.Unit          = S.Unit,
    T.UnitValue     = S.UnitValue,
    T.FlatPoints    = S.FlatPoints
WHEN NOT MATCHED BY TARGET THEN
  INSERT(ScoringSchemaID, MetricCode, PointsPerUnit, Unit, UnitValue, FlatPoints)
  VALUES(S.ScoringSchemaID, S.MetricCode, S.PointsPerUnit, S.Unit, S.UnitValue, S.FlatPoints);

/* ============================================================
   4) SEASONS: Crear temporada actual (y una previa)
   ============================================================ */
DECLARE @y INT = YEAR(SYSUTCDATETIME());
DECLARE @label NVARCHAR(20) = CONCAT(N'NFL ', @y);
DECLARE @prevlabel NVARCHAR(20) = CONCAT(N'NFL ', @y-1);

-- Temporada previa (no current)
IF NOT EXISTS (SELECT 1 FROM league.Season WHERE Label=@prevlabel)
BEGIN
  INSERT INTO league.Season(Label, Year, StartDate, EndDate, IsCurrent)
  VALUES(@prevlabel, @y-1, DATEFROMPARTS(@y-1,9,1), DATEFROMPARTS(@y,2,28), 0);
END

-- Temporada actual
IF NOT EXISTS (SELECT 1 FROM league.Season WHERE Label=@label)
BEGIN
  -- Asegurar single current
  UPDATE league.Season SET IsCurrent = 0 WHERE IsCurrent = 1;
  INSERT INTO league.Season(Label, Year, StartDate, EndDate, IsCurrent)
  VALUES(@label, @y, DATEFROMPARTS(@y,9,1), DATEFROMPARTS(@y+1,2,28), 1);
END
ELSE
BEGIN
  -- Si ya existía, márcala como current y desmarca el resto
  DECLARE @curId INT = (SELECT SeasonID FROM league.Season WHERE Label=@label);
  UPDATE league.Season SET IsCurrent = CASE WHEN SeasonID=@curId THEN 1 ELSE 0 END;
END

DECLARE @SeasonID_Current INT = (SELECT SeasonID FROM league.Season WHERE IsCurrent=1);

 /* ============================================================
    5) USERS: Crear cuentas dummy mediante SP (hash/salt)
    ============================================================ */
DECLARE @tmp TABLE(UserID INT, Message NVARCHAR(200));

-- Admin/Comisionado
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'admin@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Admin Demo', @Email=N'admin@xnfldemo.com', @Alias=N'admin',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
END

-- Co-comisionado
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'coco@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Co Admin', @Email=N'coco@xnfldemo.com', @Alias=N'coco',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'es',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
END

-- Managers
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'alice@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Alice Runner', @Email=N'alice@xnfldemo.com', @Alias=N'alice',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
END

IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'bob@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Bob Catch', @Email=N'bob@xnfldemo.com', @Alias=N'bob',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
END

IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'carol@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Carol Kick', @Email=N'carol@xnfldemo.com', @Alias=N'carol',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'es',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
END

DECLARE
  @U_Admin INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'admin@xnfldemo.com'),
  @U_Co    INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'coco@xnfldemo.com'),
  @U_Alice INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'alice@xnfldemo.com'),
  @U_Bob   INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'bob@xnfldemo.com'),
  @U_Carol INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'carol@xnfldemo.com');

 /* ============================================================
    6) LEAGUES: Crear 2 ligas demo con SP + miembros/equipos
    ============================================================ */
-- Helpers de catálogos
DECLARE
  @PF_DefaultID INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Default'),
  @PF_OfID      INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Ofensivo'),
  @SS_DefaultID INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'Default' AND Version=1),
  @SS_MaxID     INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'MaxPuntos' AND Version=1);

-- 6.1 Liga principal (se activa)
IF NOT EXISTS (
  SELECT 1
  FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'XNFL Prime League'
)
BEGIN
  DECLARE @tCreatePrime TABLE(
    LeagueID INT, Name NVARCHAR(100), TeamSlots TINYINT, AvailableSlots INT,
    Status TINYINT, PlayoffTeams TINYINT, AllowDecimals BIT, CreatedAt DATETIME2(0)
  );

  INSERT INTO @tCreatePrime
  EXEC app.sp_CreateLeague
       @CreatorUserID    = @U_Admin,
       @Name             = N'XNFL Prime League',
       @Description      = N'Liga demo principal',
       @TeamSlots        = 10,
       @LeaguePassword   = N'LeaguePass1',
       @InitialTeamName  = N'Prime Wolves',
       @PlayoffTeams     = 6,
       @AllowDecimals    = 1,
       @PositionFormatID = @PF_DefaultID,
       @ScoringSchemaID  = @SS_DefaultID;
END

DECLARE @L_Prime INT = (
  SELECT l.LeagueID FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'XNFL Prime League'
);

-- Co-comisionado
IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Prime AND UserID=@U_Co)
  INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
  VALUES(@L_Prime, @U_Co, N'CO_COMMISSIONER', 0);

-- Equipos de managers (respetando unicidad por usuario en la liga)
IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Alice)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Alice, N'Sharks');
  IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Prime AND UserID=@U_Alice)
    INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
    VALUES(@L_Prime, @U_Alice, N'MANAGER', 0);
END

IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Bob)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Bob, N'Titans');
  IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Prime AND UserID=@U_Bob)
    INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
    VALUES(@L_Prime, @U_Bob, N'MANAGER', 0);
END

IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Carol)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Carol, N'Raptors');
  IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Prime AND UserID=@U_Carol)
    INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
    VALUES(@L_Prime, @U_Carol, N'MANAGER', 0);
END

-- Activar liga principal (si ya estaba activa, registra historial "sin cambios")
EXEC app.sp_SetLeagueStatus
  @ActorUserID = @U_Admin,
  @LeagueID    = @L_Prime,
  @NewStatus   = 1,
  @Reason      = N'Población inicial';

-- 6.2 Liga secundaria (queda en PreDraft)
IF NOT EXISTS (
  SELECT 1
  FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'Rookies League'
)
BEGIN
  DECLARE @tCreateRookies TABLE(
    LeagueID INT, Name NVARCHAR(100), TeamSlots TINYINT, AvailableSlots INT,
    Status TINYINT, PlayoffTeams TINYINT, AllowDecimals BIT, CreatedAt DATETIME2(0)
  );

  INSERT INTO @tCreateRookies
  EXEC app.sp_CreateLeague
       @CreatorUserID    = @U_Co,
       @Name             = N'Rookies League',
       @Description      = N'Liga demo ofensiva para onboarding',
       @TeamSlots        = 8,
       @LeaguePassword   = N'LeaguePass1',
       @InitialTeamName  = N'Rookie Bears',
       @PlayoffTeams     = 4,
       @AllowDecimals    = 1,
       @PositionFormatID = @PF_OfID,
       @ScoringSchemaID  = @SS_MaxID;
END

DECLARE @L_Rookies INT = (
  SELECT l.LeagueID FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'Rookies League'
);

-- Sumar un manager a Rookies League
IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Rookies AND OwnerUserID=@U_Alice)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Rookies, @U_Alice, N'Aces');
  IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Rookies AND UserID=@U_Alice)
    INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
    VALUES(@L_Rookies, @U_Alice, N'MANAGER', 0);
END

/* ============================================================
   7) (Opcional) Semillas de auditoría ya generadas por SP.
   Nada adicional: sp_RegisterUser / sp_CreateLeague / sp_SetLeagueStatus
   ya escriben en audit.UserActionLog.
   ============================================================ */

/* ============================================================
   8) Vistas rápidas de verificación (no cambian estado)
   (Puedes comentar estos SELECT si no los quieres)
   ============================================================ */
-- SELECT * FROM ref.LeagueRole;
-- SELECT * FROM ref.PositionFormat;
-- SELECT * FROM ref.PositionSlot ORDER BY PositionFormatID, PositionCode;
-- SELECT * FROM scoring.ScoringSchema;
-- SELECT * FROM scoring.ScoringRule WHERE ScoringSchemaID IN (@SS_Default,@SS_Run,@SS_Max,@SS_Def) ORDER BY ScoringSchemaID, MetricCode;
-- SELECT * FROM league.Season;
-- SELECT LeagueID, SeasonID, Name, TeamSlots, Status, PositionFormatID, ScoringSchemaID FROM league.League;
-- SELECT * FROM league.LeagueMember;
-- SELECT * FROM league.Team;