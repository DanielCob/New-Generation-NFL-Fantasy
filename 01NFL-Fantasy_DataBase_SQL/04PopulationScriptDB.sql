USE XNFLFantasyDB;
SET NOCOUNT ON;

/* ============================================================
   SECCIÓN 0: AUTH - Roles del Sistema (NUEVO)
   ============================================================ */
PRINT N'Poblando roles del sistema...';

MERGE auth.SystemRole AS T
USING (VALUES
  (N'ADMIN',          N'Administrador',     N'Control total del sistema, gestión de usuarios y equipos NFL'),
  (N'USER',           N'Usuario',           N'Usuario regular, puede ser manager o comisionado de ligas'),
  (N'BRAND_MANAGER',  N'Gestor de Marca',   N'Usuario con permisos especiales para gestión de marca')
) AS S(RoleCode, Display, Description)
ON (T.RoleCode = S.RoleCode)
WHEN MATCHED AND (T.Display <> S.Display OR ISNULL(T.Description, N'') <> ISNULL(S.Description, N'')) THEN
  UPDATE SET T.Display = S.Display, T.Description = S.Description
WHEN NOT MATCHED BY TARGET THEN
  INSERT(RoleCode, Display, Description) VALUES(S.RoleCode, S.Display, S.Description);

DECLARE @RoleCount INT = @@ROWCOUNT;
PRINT N'✓ ' + CAST(@RoleCount AS NVARCHAR(10)) + N' roles del sistema insertados/actualizados';

/* ============================================================
   SECCIÓN 1: REF - Roles de liga (SIN CAMBIOS)
   ============================================================ */
PRINT N'Poblando roles de liga...';

MERGE ref.LeagueRole AS T
USING (VALUES
  (N'COMMISSIONER',     N'Comisionado'),
  (N'CO_COMMISSIONER',  N'Co-Comisionado'),
  (N'SPECTATOR',        N'Espectador')
) AS S(RoleCode, Display)
ON (T.RoleCode = S.RoleCode)
WHEN MATCHED AND T.Display <> S.Display THEN
  UPDATE SET T.Display = S.Display
WHEN NOT MATCHED BY TARGET THEN
  INSERT(RoleCode, Display) VALUES(S.RoleCode, S.Display);

/* ============================================================
   SECCIÓN 2: REF - PositionFormat + PositionSlot (SIN CAMBIOS)
   ============================================================ */
PRINT N'Poblando formatos de posición...';

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

-- 2.2 Slots por formato
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

-- Extremo
INSERT INTO @Slots VALUES
 (N'Extremo', N'QB', 1),(N'Extremo', N'RB', 2),(N'Extremo', N'WR', 2),
 (N'Extremo', N'TE', 1),(N'Extremo', N'RB/WR', 1),
 (N'Extremo', N'DL', 1),(N'Extremo', N'LB', 1),(N'Extremo', N'CB', 1),
 (N'Extremo', N'K', 1),(N'Extremo', N'DEF', 1),
 (N'Extremo', N'BENCH', 10),(N'Extremo', N'IR', 3);

-- Ofensivo
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

PRINT N'✓ Formatos de posición y slots poblados';

/* ============================================================
   SECCIÓN 3: REF - Equipos NFL
   ============================================================ */
PRINT N'Poblando equipos NFL...';

-- Tabla temporal para equipos NFL
DECLARE @NFLTeams TABLE(
  TeamName NVARCHAR(100),
  City NVARCHAR(100)
);

INSERT INTO @NFLTeams VALUES
  -- AFC East
  (N'Buffalo Bills', N'Buffalo'),
  (N'Miami Dolphins', N'Miami'),
  (N'New England Patriots', N'Foxborough'),
  (N'New York Jets', N'East Rutherford'),
  -- AFC North
  (N'Baltimore Ravens', N'Baltimore'),
  (N'Cincinnati Bengals', N'Cincinnati'),
  (N'Cleveland Browns', N'Cleveland'),
  (N'Pittsburgh Steelers', N'Pittsburgh'),
  -- AFC South
  (N'Houston Texans', N'Houston'),
  (N'Indianapolis Colts', N'Indianapolis'),
  (N'Jacksonville Jaguars', N'Jacksonville'),
  (N'Tennessee Titans', N'Nashville'),
  -- AFC West
  (N'Denver Broncos', N'Denver'),
  (N'Kansas City Chiefs', N'Kansas City'),
  (N'Las Vegas Raiders', N'Las Vegas'),
  (N'Los Angeles Chargers', N'Los Angeles'),
  -- NFC East
  (N'Dallas Cowboys', N'Arlington'),
  (N'New York Giants', N'East Rutherford'),
  (N'Philadelphia Eagles', N'Philadelphia'),
  (N'Washington Commanders', N'Landover'),
  -- NFC North
  (N'Chicago Bears', N'Chicago'),
  (N'Detroit Lions', N'Detroit'),
  (N'Green Bay Packers', N'Green Bay'),
  (N'Minnesota Vikings', N'Minneapolis'),
  -- NFC South
  (N'Atlanta Falcons', N'Atlanta'),
  (N'Carolina Panthers', N'Charlotte'),
  (N'New Orleans Saints', N'New Orleans'),
  (N'Tampa Bay Buccaneers', N'Tampa'),
  -- NFC West
  (N'Arizona Cardinals', N'Glendale'),
  (N'Los Angeles Rams', N'Los Angeles'),
  (N'San Francisco 49ers', N'Santa Clara'),
  (N'Seattle Seahawks', N'Seattle');

-- Insertar equipos NFL
MERGE ref.NFLTeam AS T
USING @NFLTeams AS S
ON (T.TeamName = S.TeamName)
WHEN NOT MATCHED BY TARGET THEN
  INSERT(TeamName, City, IsActive, CreatedByUserID)
  VALUES(S.TeamName, S.City, 1, NULL);

DECLARE @NFLTeamCount INT = @@ROWCOUNT;
PRINT N'✓ ' + CAST(@NFLTeamCount AS NVARCHAR(10)) + N' equipos NFL insertados/actualizados';

/* ============================================================
   SECCIÓN 4: SCORING - Schemas + Rules (SIN CAMBIOS)
   ============================================================ */
PRINT N'Poblando esquemas de puntuación...';

-- 4.1 Schemas
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

-- 4.2 Rules
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

-- PrioridadCarrera
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

-- MaxPuntos
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

-- PrioridadDefensa
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

PRINT N'✓ Esquemas de puntuación y reglas poblados';

/* ============================================================
   SECCIÓN 5: SEASONS - Crear temporada actual (SIN CAMBIOS)
   ============================================================ */
PRINT N'Poblando temporadas...';

DECLARE @y INT = YEAR(SYSUTCDATETIME());
DECLARE @label NVARCHAR(20) = N'NFL ' + CAST(@y AS NVARCHAR(10));
DECLARE @prevlabel NVARCHAR(20) = N'NFL ' + CAST(@y-1 AS NVARCHAR(10));

-- Temporada previa
IF NOT EXISTS (SELECT 1 FROM league.Season WHERE Label=@prevlabel)
BEGIN
  INSERT INTO league.Season(Label, Year, StartDate, EndDate, IsCurrent)
  VALUES(@prevlabel, @y-1, DATEFROMPARTS(@y-1,9,1), DATEFROMPARTS(@y,2,28), 0);
END

-- Temporada actual
IF NOT EXISTS (SELECT 1 FROM league.Season WHERE Label=@label)
BEGIN
  UPDATE league.Season SET IsCurrent = 0 WHERE IsCurrent = 1;
  INSERT INTO league.Season(Label, Year, StartDate, EndDate, IsCurrent)
  VALUES(@label, @y, DATEFROMPARTS(@y,9,1), DATEFROMPARTS(@y+1,2,28), 1);
END
ELSE
BEGIN
  DECLARE @curId INT = (SELECT SeasonID FROM league.Season WHERE Label=@label);
  UPDATE league.Season SET IsCurrent = CASE WHEN SeasonID=@curId THEN 1 ELSE 0 END;
END

DECLARE @SeasonID_Current INT = (SELECT SeasonID FROM league.Season WHERE IsCurrent=1);

PRINT N'✓ Temporada actual: ' + @label + N' (ID: ' + CAST(@SeasonID_Current AS NVARCHAR(10)) + N')';

/* ============================================================
   SECCIÓN 6: USERS - Crear cuentas dummy
   ============================================================ */
PRINT N'Poblando usuarios demo...';

DECLARE @tmp TABLE(UserID INT, SystemRoleCode NVARCHAR(20), Message NVARCHAR(200));

-- Admin/Comisionado (será promovido a ADMIN después)
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'admin@xnfldemo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Admin Demo', @Email=N'admin@xnfldemo.com', @Alias=N'admin',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario admin@xnfldemo.com creado';
END

-- Co-comisionado
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'coco@xnfldemo.com')
BEGIN
  DELETE FROM @tmp;
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Co Admin', @Email=N'coco@xnfldemo.com', @Alias=N'coco',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'es',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario coco@xnfldemo.com creado';
END

-- Managers
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'alice@xnfldemo.com')
BEGIN
  DELETE FROM @tmp;
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Alice Runner', @Email=N'alice@xnfldemo.com', @Alias=N'alice',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario alice@xnfldemo.com creado';
END

IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'bob@xnfldemo.com')
BEGIN
  DELETE FROM @tmp;
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Bob Catch', @Email=N'bob@xnfldemo.com', @Alias=N'bob',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario bob@xnfldemo.com creado';
END

IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'carol@xnfldemo.com')
BEGIN
  DELETE FROM @tmp;
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Carol Kick', @Email=N'carol@xnfldemo.com', @Alias=N'carol',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'es',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario carol@xnfldemo.com creado';
END

-- Brand Manager de ejemplo
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'brand@xnfldemo.com')
BEGIN
  DELETE FROM @tmp;
  INSERT INTO @tmp EXEC app.sp_RegisterUser
     @Name=N'Brand Manager Demo', @Email=N'brand@xnfldemo.com', @Alias=N'brandmgr',
     @Password=N'Secure123', @PasswordConfirm=N'Secure123', @LanguageCode=N'en',
     @ProfileImageUrl=NULL, @ProfileImageWidth=NULL, @ProfileImageHeight=NULL, @ProfileImageBytes=NULL;
  PRINT N'✓ Usuario brand@xnfldemo.com creado';
END

DECLARE
  @U_Admin INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'admin@xnfldemo.com'),
  @U_Co    INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'coco@xnfldemo.com'),
  @U_Alice INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'alice@xnfldemo.com'),
  @U_Bob   INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'bob@xnfldemo.com'),
  @U_Carol INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'carol@xnfldemo.com'),
  @U_Brand INT   = (SELECT UserID FROM auth.UserAccount WHERE Email=N'brand@xnfldemo.com');

PRINT N'✓ 6 usuarios demo disponibles';

/* ============================================================
   SECCIÓN 6.5: USUARIOS - Promover roles del sistema (NUEVO)
   ============================================================ */
PRINT N'Configurando roles del sistema...';

-- Promover admin@xnfldemo.com a ADMIN
-- Nota: Como no tenemos un ADMIN todavía, hacemos esto directamente en la DB
IF EXISTS (SELECT 1 FROM auth.UserAccount WHERE UserID = @U_Admin AND SystemRoleCode <> N'ADMIN')
BEGIN
  UPDATE auth.UserAccount
  SET SystemRoleCode = N'ADMIN',
      UpdatedAt = SYSUTCDATETIME()
  WHERE UserID = @U_Admin;

  -- Registrar el cambio en el log
  INSERT INTO auth.SystemRoleChangeLog(UserID, ChangedByUserID, OldRoleCode, NewRoleCode, Reason)
  VALUES(@U_Admin, @U_Admin, N'USER', N'ADMIN', N'Población inicial - usuario administrador del sistema');

  -- Auditoría
  INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, Details)
  VALUES(@U_Admin, N'USER_PROFILE', CAST(@U_Admin AS NVARCHAR(50)), N'SYSTEM_ROLE_INIT',
         N'Usuario promovido a ADMIN durante población inicial');

  PRINT N'✓ Usuario admin@xnfldemo.com promovido a ADMIN';
END

-- Promover brand@xnfldemo.com a BRAND_MANAGER usando el ADMIN recién creado
IF EXISTS (SELECT 1 FROM auth.UserAccount WHERE UserID = @U_Brand AND SystemRoleCode <> N'BRAND_MANAGER')
BEGIN
  -- Ahora podemos usar el SP porque ya tenemos un ADMIN
  DECLARE @tmpRole TABLE(UserID INT, OldRole NVARCHAR(20), NewRole NVARCHAR(20), Message NVARCHAR(200));
  
  INSERT INTO @tmpRole
  EXEC app.sp_ChangeUserSystemRole
    @ActorUserID = @U_Admin,
    @TargetUserID = @U_Brand,
    @NewRoleCode = N'BRAND_MANAGER',
    @Reason = N'Población inicial - usuario de ejemplo para Brand Manager';

  PRINT N'✓ Usuario brand@xnfldemo.com promovido a BRAND_MANAGER';
END

/* ============================================================
   SECCIÓN 7: PLAYERS - Jugadores NFL
   ============================================================ */
PRINT N'Poblando jugadores NFL...';

-- Obtener IDs de equipos NFL
DECLARE
  @Chiefs_ID INT     = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Kansas City Chiefs'),
  @Bills_ID INT      = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Buffalo Bills'),
  @Eagles_ID INT     = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Philadelphia Eagles'),
  @49ers_ID INT      = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'San Francisco 49ers'),
  @Cowboys_ID INT    = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Dallas Cowboys'),
  @Dolphins_ID INT   = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Miami Dolphins'),
  @Ravens_ID INT     = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Baltimore Ravens'),
  @Bengals_ID INT    = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Cincinnati Bengals'),
  @Lions_ID INT      = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Detroit Lions'),
  @Packers_ID INT    = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Green Bay Packers'),
  @Vikings_ID INT    = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Minnesota Vikings'),
  @Browns_ID INT     = (SELECT NFLTeamID FROM ref.NFLTeam WHERE TeamName = N'Cleveland Browns');

-- Tabla temporal para jugadores
DECLARE @Players TABLE(
  FirstName NVARCHAR(50),
  LastName NVARCHAR(50),
  Position NVARCHAR(20),
  NFLTeamID INT,
  InjuryStatus NVARCHAR(50)
);

INSERT INTO @Players VALUES
  -- Quarterbacks
  (N'Patrick', N'Mahomes', N'QB', @Chiefs_ID, N'Healthy'),
  (N'Josh', N'Allen', N'QB', @Bills_ID, N'Healthy'),
  (N'Jalen', N'Hurts', N'QB', @Eagles_ID, N'Healthy'),
  (N'Brock', N'Purdy', N'QB', @49ers_ID, N'Healthy'),
  (N'Dak', N'Prescott', N'QB', @Cowboys_ID, N'Healthy'),
  (N'Tua', N'Tagovailoa', N'QB', @Dolphins_ID, N'Healthy'),
  (N'Lamar', N'Jackson', N'QB', @Ravens_ID, N'Healthy'),
  (N'Joe', N'Burrow', N'QB', @Bengals_ID, N'Healthy'),
  
  -- Running Backs
  (N'Christian', N'McCaffrey', N'RB', @49ers_ID, N'Healthy'),
  (N'Derrick', N'Henry', N'RB', @Ravens_ID, N'Healthy'),
  (N'Nick', N'Chubb', N'RB', @Browns_ID, N'Questionable'),
  (N'Josh', N'Jacobs', N'RB', @Packers_ID, N'Healthy'),
  (N'Tony', N'Pollard', N'RB', @Cowboys_ID, N'Healthy'),
  (N'Saquon', N'Barkley', N'RB', @Eagles_ID, N'Healthy'),
  (N'Jahmyr', N'Gibbs', N'RB', @Lions_ID, N'Healthy'),
  (N'David', N'Montgomery', N'RB', @Lions_ID, N'Healthy'),
  
  -- Wide Receivers
  (N'Tyreek', N'Hill', N'WR', @Dolphins_ID, N'Healthy'),
  (N'CeeDee', N'Lamb', N'WR', @Cowboys_ID, N'Healthy'),
  (N'Justin', N'Jefferson', N'WR', @Vikings_ID, N'Healthy'),
  (N'Stefon', N'Diggs', N'WR', @Bills_ID, N'Healthy'),
  (N'Deebo', N'Samuel', N'WR', @49ers_ID, N'Healthy'),
  (N'Brandon', N'Aiyuk', N'WR', @49ers_ID, N'Healthy'),
  (N'AJ', N'Brown', N'WR', @Eagles_ID, N'Healthy'),
  (N'Amon-Ra', N'St. Brown', N'WR', @Lions_ID, N'Healthy'),
  (N'Rashee', N'Rice', N'WR', @Chiefs_ID, N'Healthy'),
  (N'Amari', N'Cooper', N'WR', @Browns_ID, N'Healthy'),
  
  -- Tight Ends
  (N'Travis', N'Kelce', N'TE', @Chiefs_ID, N'Healthy'),
  (N'George', N'Kittle', N'TE', @49ers_ID, N'Healthy'),
  (N'Mark', N'Andrews', N'TE', @Ravens_ID, N'Out'),
  (N'TJ', N'Hockenson', N'TE', @Vikings_ID, N'IR'),
  (N'Dallas', N'Goedert', N'TE', @Eagles_ID, N'Healthy'),
  (N'Dalton', N'Kincaid', N'TE', @Bills_ID, N'Healthy'),
  
  -- Kickers
  (N'Harrison', N'Butker', N'K', @Chiefs_ID, N'Healthy'),
  (N'Justin', N'Tucker', N'K', @Ravens_ID, N'Healthy'),
  (N'Jake', N'Moody', N'K', @49ers_ID, N'Healthy'),
  (N'Tyler', N'Bass', N'K', @Bills_ID, N'Healthy'),
  (N'Brandon', N'Aubrey', N'K', @Cowboys_ID, N'Healthy'),
  (N'Jason', N'Sanders', N'K', @Dolphins_ID, N'Healthy'),
  
  -- Defenses (representadas como jugadores especiales)
  (N'49ers', N'Defense', N'DEF', @49ers_ID, N'Healthy'),
  (N'Cowboys', N'Defense', N'DEF', @Cowboys_ID, N'Healthy'),
  (N'Ravens', N'Defense', N'DEF', @Ravens_ID, N'Healthy'),
  (N'Bills', N'Defense', N'DEF', @Bills_ID, N'Healthy'),
  (N'Eagles', N'Defense', N'DEF', @Eagles_ID, N'Healthy'),
  (N'Chiefs', N'Defense', N'DEF', @Chiefs_ID, N'Healthy');

-- Insertar jugadores
MERGE league.Player AS T
USING @Players AS S
ON (T.FirstName = S.FirstName AND T.LastName = S.LastName AND T.NFLTeamID = S.NFLTeamID)
WHEN NOT MATCHED BY TARGET THEN
  INSERT(FirstName, LastName, Position, NFLTeamID, InjuryStatus, IsActive)
  VALUES(S.FirstName, S.LastName, S.Position, S.NFLTeamID, S.InjuryStatus, 1);

DECLARE @PlayerCount INT = @@ROWCOUNT;
PRINT N'✓ ' + CAST(@PlayerCount AS NVARCHAR(10)) + N' jugadores NFL insertados/actualizados';

/* ============================================================
   SECCIÓN 8: LEAGUES - Crear ligas demo
   ============================================================ */
PRINT N'Poblando ligas demo...';

-- Helpers de catálogos
DECLARE
  @PF_DefaultID INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Default'),
  @PF_OfID      INT = (SELECT PositionFormatID FROM ref.PositionFormat WHERE Name=N'Ofensivo'),
  @SS_DefaultID INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'Default' AND Version=1),
  @SS_MaxID     INT = (SELECT ScoringSchemaID FROM scoring.ScoringSchema WHERE Name=N'MaxPuntos' AND Version=1);

-- 8.1 Liga principal (se activa)
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
       @InitialTeamName  = N'Kansas City Chiefs',
       @PlayoffTeams     = 6,
       @AllowDecimals    = 1,
       @PositionFormatID = @PF_DefaultID,
       @ScoringSchemaID  = @SS_DefaultID;
  
  PRINT N'✓ Liga XNFL Prime League creada';
END

DECLARE @L_Prime INT = (
  SELECT l.LeagueID FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'XNFL Prime League'
);

-- Co-comisionado
IF NOT EXISTS (SELECT 1 FROM league.LeagueMember WHERE LeagueID=@L_Prime AND UserID=@U_Co)
BEGIN
  INSERT INTO league.LeagueMember(LeagueID, UserID, RoleCode, IsPrimaryCommissioner)
  VALUES(@L_Prime, @U_Co, N'CO_COMMISSIONER', 0);
  PRINT N'✓ Co-comisionado agregado a Prime League';
END

-- Equipos de managers (SIN insertar en LeagueMember - el rol MANAGER se deriva del equipo)
IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Alice)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Alice, N'Buffalo Bills');
  -- ✅ NO insertar en LeagueMember - MANAGER se deriva automáticamente
  PRINT N'✓ Equipo Buffalo Bills agregado (Alice)';
END

IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Bob)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Bob, N'Dallas Cowboys');
  -- ✅ NO insertar en LeagueMember - MANAGER se deriva automáticamente
  PRINT N'✓ Equipo Dallas Cowboys agregado (Bob)';
END

IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Prime AND OwnerUserID=@U_Carol)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Prime, @U_Carol, N'Philadelphia Eagles');
  -- ✅ NO insertar en LeagueMember - MANAGER se deriva automáticamente
  PRINT N'✓ Equipo Philadelphia Eagles agregado (Carol)';
END

-- Activar liga principal
EXEC app.sp_SetLeagueStatus
  @ActorUserID = @U_Admin,
  @LeagueID    = @L_Prime,
  @NewStatus   = 1,
  @Reason      = N'Población inicial';

PRINT N'✓ Liga Prime activada';

-- 8.2 Liga secundaria
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
       @InitialTeamName  = N'San Francisco 49ers',
       @PlayoffTeams     = 4,
       @AllowDecimals    = 1,
       @PositionFormatID = @PF_OfID,
       @ScoringSchemaID  = @SS_MaxID;
  
  PRINT N'✓ Liga Rookies League creada';
END

DECLARE @L_Rookies INT = (
  SELECT l.LeagueID FROM league.League l
  WHERE l.SeasonID = @SeasonID_Current AND l.Name = N'Rookies League'
);

-- Sumar un manager a Rookies League (SIN insertar en LeagueMember)
IF NOT EXISTS (SELECT 1 FROM league.Team WHERE LeagueID=@L_Rookies AND OwnerUserID=@U_Alice)
BEGIN
  INSERT INTO league.Team(LeagueID, OwnerUserID, TeamName)
  VALUES(@L_Rookies, @U_Alice, N'Miami Dolphins');
  -- ✅ NO insertar en LeagueMember - MANAGER se deriva automáticamente
  PRINT N'✓ Equipo Miami Dolphins agregado a Rookies League (Alice)';
END

/* ============================================================
   SECCIÓN 9: NFL GAMES - Partidos programados
   ============================================================ */
PRINT N'Poblando partidos NFL demo...';

-- Tabla temporal para partidos
DECLARE @Games TABLE(
  Week TINYINT,
  HomeTeamName NVARCHAR(100),
  AwayTeamName NVARCHAR(100),
  GameDate DATE,
  GameStatus NVARCHAR(20)
);

-- Partidos de ejemplo (Semanas 1-3 de la temporada actual)
INSERT INTO @Games VALUES
  (1, N'Kansas City Chiefs', N'Baltimore Ravens', DATEFROMPARTS(@y, 9, 7), N'Scheduled'),
  (1, N'Buffalo Bills', N'Miami Dolphins', DATEFROMPARTS(@y, 9, 8), N'Scheduled'),
  (1, N'Dallas Cowboys', N'New York Giants', DATEFROMPARTS(@y, 9, 8), N'Scheduled'),
  (1, N'Philadelphia Eagles', N'New England Patriots', DATEFROMPARTS(@y, 9, 8), N'Scheduled'),
  (1, N'San Francisco 49ers', N'Pittsburgh Steelers', DATEFROMPARTS(@y, 9, 8), N'Scheduled'),
  
  (2, N'Miami Dolphins', N'Buffalo Bills', DATEFROMPARTS(@y, 9, 14), N'Scheduled'),
  (2, N'Baltimore Ravens', N'Cincinnati Bengals', DATEFROMPARTS(@y, 9, 15), N'Scheduled'),
  (2, N'Philadelphia Eagles', N'Minnesota Vikings', DATEFROMPARTS(@y, 9, 15), N'Scheduled'),
  (2, N'Dallas Cowboys', N'New York Jets', DATEFROMPARTS(@y, 9, 15), N'Scheduled'),
  
  (3, N'Kansas City Chiefs', N'Los Angeles Chargers', DATEFROMPARTS(@y, 9, 21), N'Scheduled'),
  (3, N'San Francisco 49ers', N'Los Angeles Rams', DATEFROMPARTS(@y, 9, 22), N'Scheduled'),
  (3, N'Buffalo Bills', N'Jacksonville Jaguars', DATEFROMPARTS(@y, 9, 22), N'Scheduled');

-- Insertar partidos
MERGE league.NFLGame AS T
USING (
  SELECT 
    @SeasonID_Current AS SeasonID,
    g.Week,
    ht.NFLTeamID AS HomeTeamID,
    at.NFLTeamID AS AwayTeamID,
    g.GameDate,
    g.GameStatus
  FROM @Games g
  JOIN ref.NFLTeam ht ON ht.TeamName = g.HomeTeamName
  JOIN ref.NFLTeam at ON at.TeamName = g.AwayTeamName
) AS S(SeasonID, Week, HomeTeamID, AwayTeamID, GameDate, GameStatus)
ON (T.SeasonID = S.SeasonID AND T.Week = S.Week AND T.HomeTeamID = S.HomeTeamID AND T.AwayTeamID = S.AwayTeamID)
WHEN NOT MATCHED BY TARGET THEN
  INSERT(SeasonID, Week, HomeTeamID, AwayTeamID, GameDate, GameStatus)
  VALUES(S.SeasonID, S.Week, S.HomeTeamID, S.AwayTeamID, S.GameDate, S.GameStatus);

DECLARE @GameCount INT = @@ROWCOUNT;
PRINT N'✓ ' + CAST(@GameCount AS NVARCHAR(10)) + N' partidos NFL insertados/actualizados';

/* ============================================================
   SECCIÓN 10: ROSTER - Asignar jugadores a equipos
   ============================================================ */
PRINT N'Poblando rosters de equipos fantasy...';

-- Obtener IDs de equipos fantasy
DECLARE
  @Team_Chiefs INT = (SELECT TeamID FROM league.Team WHERE LeagueID = @L_Prime AND TeamName = N'Kansas City Chiefs'),
  @Team_Bills INT  = (SELECT TeamID FROM league.Team WHERE LeagueID = @L_Prime AND TeamName = N'Buffalo Bills'),
  @Team_Cowboys INT = (SELECT TeamID FROM league.Team WHERE LeagueID = @L_Prime AND TeamName = N'Dallas Cowboys'),
  @Team_Eagles INT = (SELECT TeamID FROM league.Team WHERE LeagueID = @L_Prime AND TeamName = N'Philadelphia Eagles');

-- Tabla temporal para asignaciones de roster
DECLARE @RosterAssignments TABLE(
  TeamID INT,
  PlayerFirstName NVARCHAR(50),
  PlayerLastName NVARCHAR(50),
  AcquisitionType NVARCHAR(20)
);

-- Roster para Kansas City Chiefs (Admin)
INSERT INTO @RosterAssignments VALUES
  (@Team_Chiefs, N'Patrick', N'Mahomes', N'Draft'),
  (@Team_Chiefs, N'Travis', N'Kelce', N'Draft'),
  (@Team_Chiefs, N'Rashee', N'Rice', N'Draft'),
  (@Team_Chiefs, N'Josh', N'Jacobs', N'FreeAgent'),
  (@Team_Chiefs, N'Harrison', N'Butker', N'Draft'),
  (@Team_Chiefs, N'Chiefs', N'Defense', N'Draft');

-- Roster para Buffalo Bills (Alice)
INSERT INTO @RosterAssignments VALUES
  (@Team_Bills, N'Josh', N'Allen', N'Draft'),
  (@Team_Bills, N'Stefon', N'Diggs', N'Draft'),
  (@Team_Bills, N'Dalton', N'Kincaid', N'Draft'),
  (@Team_Bills, N'Jahmyr', N'Gibbs', N'FreeAgent'),
  (@Team_Bills, N'Tyler', N'Bass', N'Draft'),
  (@Team_Bills, N'Bills', N'Defense', N'Draft');

-- Roster para Dallas Cowboys (Bob)
INSERT INTO @RosterAssignments VALUES
  (@Team_Cowboys, N'Dak', N'Prescott', N'Draft'),
  (@Team_Cowboys, N'CeeDee', N'Lamb', N'Draft'),
  (@Team_Cowboys, N'Tony', N'Pollard', N'Draft'),
  (@Team_Cowboys, N'Brandon', N'Aubrey', N'Draft'),
  (@Team_Cowboys, N'Cowboys', N'Defense', N'Draft');

-- Roster para Philadelphia Eagles (Carol)
INSERT INTO @RosterAssignments VALUES
  (@Team_Eagles, N'Jalen', N'Hurts', N'Draft'),
  (@Team_Eagles, N'AJ', N'Brown', N'Draft'),
  (@Team_Eagles, N'Saquon', N'Barkley', N'Draft'),
  (@Team_Eagles, N'Dallas', N'Goedert', N'Draft'),
  (@Team_Eagles, N'Eagles', N'Defense', N'Draft');

-- Insertar asignaciones de roster
INSERT INTO league.TeamRoster(TeamID, PlayerID, AcquisitionType, AddedByUserID, IsActive)
SELECT 
  ra.TeamID,
  p.PlayerID,
  ra.AcquisitionType,
  t.OwnerUserID,
  1
FROM @RosterAssignments ra
JOIN league.Player p ON p.FirstName = ra.PlayerFirstName AND p.LastName = ra.PlayerLastName
JOIN league.Team t ON t.TeamID = ra.TeamID
WHERE NOT EXISTS (
  SELECT 1 FROM league.TeamRoster tr
  WHERE tr.TeamID = ra.TeamID AND tr.PlayerID = p.PlayerID
);

DECLARE @RosterCount INT = @@ROWCOUNT;
PRINT N'✓ ' + CAST(@RosterCount AS NVARCHAR(10)) + N' jugadores asignados a rosters';

/* ============================================================
   RESUMEN FINAL
   ============================================================ */
PRINT N'';
PRINT N'═══════════════════════════════════════════════════════════';
PRINT N'  POBLACIÓN DE DATOS COMPLETADA EXITOSAMENTE';
PRINT N'═══════════════════════════════════════════════════════════';
PRINT N'';
PRINT N'📊 Resumen de datos poblados:';
PRINT N'';

-- Variables para el resumen
DECLARE @cnt_systemroles INT, @cnt_roles INT, @cnt_formats INT, @cnt_slots INT, @cnt_nflteams INT,
        @cnt_schemas INT, @cnt_rules INT, @cnt_seasons INT, @cnt_users INT,
        @cnt_players INT, @cnt_leagues INT, @cnt_teams INT, @cnt_games INT,
        @cnt_roster INT, @cnt_admins INT, @cnt_brandmgrs INT, @cnt_regularusers INT;

SELECT @cnt_systemroles = COUNT(*) FROM auth.SystemRole;
SELECT @cnt_roles = COUNT(*) FROM ref.LeagueRole;
SELECT @cnt_formats = COUNT(*) FROM ref.PositionFormat;
SELECT @cnt_slots = COUNT(*) FROM ref.PositionSlot;
SELECT @cnt_nflteams = COUNT(*) FROM ref.NFLTeam;
SELECT @cnt_schemas = COUNT(*) FROM scoring.ScoringSchema;
SELECT @cnt_rules = COUNT(*) FROM scoring.ScoringRule;
SELECT @cnt_seasons = COUNT(*) FROM league.Season;
SELECT @cnt_users = COUNT(*) FROM auth.UserAccount;
SELECT @cnt_players = COUNT(*) FROM league.Player;
SELECT @cnt_leagues = COUNT(*) FROM league.League;
SELECT @cnt_teams = COUNT(*) FROM league.Team;
SELECT @cnt_games = COUNT(*) FROM league.NFLGame;
SELECT @cnt_roster = COUNT(*) FROM league.TeamRoster WHERE IsActive = 1;
SELECT @cnt_admins = COUNT(*) FROM auth.UserAccount WHERE SystemRoleCode = N'ADMIN';
SELECT @cnt_brandmgrs = COUNT(*) FROM auth.UserAccount WHERE SystemRoleCode = N'BRAND_MANAGER';
SELECT @cnt_regularusers = COUNT(*) FROM auth.UserAccount WHERE SystemRoleCode = N'USER';

PRINT N'  ✓ ' + CAST(@cnt_systemroles AS NVARCHAR(10)) + N' roles del sistema';
PRINT N'  ✓ ' + CAST(@cnt_roles AS NVARCHAR(10)) + N' roles de liga';
PRINT N'  ✓ ' + CAST(@cnt_formats AS NVARCHAR(10)) + N' formatos de posición';
PRINT N'  ✓ ' + CAST(@cnt_slots AS NVARCHAR(10)) + N' slots de posición';
PRINT N'  ✓ ' + CAST(@cnt_nflteams AS NVARCHAR(10)) + N' equipos NFL';
PRINT N'  ✓ ' + CAST(@cnt_schemas AS NVARCHAR(10)) + N' esquemas de puntuación';
PRINT N'  ✓ ' + CAST(@cnt_rules AS NVARCHAR(10)) + N' reglas de puntuación';
PRINT N'  ✓ ' + CAST(@cnt_seasons AS NVARCHAR(10)) + N' temporadas';
PRINT N'  ✓ ' + CAST(@cnt_users AS NVARCHAR(10)) + N' usuarios (' + 
         CAST(@cnt_admins AS NVARCHAR(10)) + N' ADMIN, ' + 
         CAST(@cnt_brandmgrs AS NVARCHAR(10)) + N' BRAND_MANAGER, ' + 
         CAST(@cnt_regularusers AS NVARCHAR(10)) + N' USER)';
PRINT N'  ✓ ' + CAST(@cnt_players AS NVARCHAR(10)) + N' jugadores NFL';
PRINT N'  ✓ ' + CAST(@cnt_leagues AS NVARCHAR(10)) + N' ligas';
PRINT N'  ✓ ' + CAST(@cnt_teams AS NVARCHAR(10)) + N' equipos fantasy';
PRINT N'  ✓ ' + CAST(@cnt_games AS NVARCHAR(10)) + N' partidos NFL programados';
PRINT N'  ✓ ' + CAST(@cnt_roster AS NVARCHAR(10)) + N' jugadores en rosters activos';
PRINT N'';
PRINT N'🎮 Usuarios demo (password: Secure123):';
PRINT N'  • admin@xnfldemo.com (ROL: ADMIN - Comisionado Principal)';
PRINT N'  • coco@xnfldemo.com (ROL: USER - Co-Comisionado)';
PRINT N'  • alice@xnfldemo.com (ROL: USER - Manager)';
PRINT N'  • bob@xnfldemo.com (ROL: USER - Manager)';
PRINT N'  • carol@xnfldemo.com (ROL: USER - Manager)';
PRINT N'  • brand@xnfldemo.com (ROL: BRAND_MANAGER - Gestor de Marca)';
PRINT N'';
PRINT N'🏈 Ligas disponibles:';
PRINT N'  • XNFL Prime League (Estado: Activa, 4 equipos)';
PRINT N'  • Rookies League (Estado: Pre-Draft, 2 equipos)';
PRINT N'';
PRINT N'🔐 Roles del Sistema:';
PRINT N'  • ADMIN: Control total, gestión de usuarios y equipos NFL';
PRINT N'  • USER: Usuario regular, puede ser manager/comisionado';
PRINT N'  • BRAND_MANAGER: Permisos especiales para gestión de marca';
PRINT N'';
PRINT N'═══════════════════════════════════════════════════════════';