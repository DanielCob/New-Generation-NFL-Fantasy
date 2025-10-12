USE XNFLFantasyDB;
GO

CREATE OR ALTER VIEW dbo.vw_UserActiveSessions
AS
SELECT
  s.UserID,
  s.SessionID,
  s.CreatedAt,
  s.LastActivityAt,
  s.ExpiresAt,
  s.IsValid
FROM auth.Session s
WHERE s.IsValid = 1
  AND s.ExpiresAt > SYSUTCDATETIME();
GO

GRANT SELECT ON dbo.vw_UserActiveSessions TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_CurrentSeason
AS
SELECT
  SeasonID, Label, Year, StartDate, EndDate, IsCurrent, CreatedAt
FROM league.Season
WHERE IsCurrent = 1;
GO

GRANT SELECT ON dbo.vw_CurrentSeason TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_PositionFormats
AS
SELECT
  PositionFormatID,
  Name,
  Description,
  CreatedAt
FROM ref.PositionFormat;
GO

GRANT SELECT ON dbo.vw_PositionFormats TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_PositionFormatSlots
AS
SELECT
  pf.PositionFormatID,
  pf.Name          AS FormatName,
  ps.PositionCode,
  ps.SlotCount
FROM ref.PositionFormat pf
JOIN ref.PositionSlot   ps ON ps.PositionFormatID = pf.PositionFormatID;
GO

GRANT SELECT ON dbo.vw_PositionFormatSlots TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_ScoringSchemas
AS
SELECT
  ScoringSchemaID,
  Name,
  Version,
  IsTemplate,
  Description,
  CreatedAt
FROM scoring.ScoringSchema;
GO

GRANT SELECT ON dbo.vw_ScoringSchemas TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_ScoringSchemaRules
AS
SELECT
  ss.ScoringSchemaID,
  ss.Name,
  ss.Version,
  r.MetricCode,
  r.PointsPerUnit,
  r.Unit,
  r.UnitValue,
  r.FlatPoints
FROM scoring.ScoringSchema ss
JOIN scoring.ScoringRule   r ON r.ScoringSchemaID = ss.ScoringSchemaID;
GO

GRANT SELECT ON dbo.vw_ScoringSchemaRules TO app_executor;
GO

-- ============================================================================
-- vw_NFLTeams
-- Vista de equipos NFL con información básica
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_NFLTeams
AS
SELECT
  nt.NFLTeamID,
  nt.TeamName,
  nt.City,
  nt.TeamImageUrl,
  nt.ThumbnailUrl,
  nt.IsActive,
  nt.CreatedAt,
  nt.UpdatedAt,
  creator.Name AS CreatedByName,
  updater.Name AS UpdatedByName,
  -- Contar jugadores activos del equipo
  (SELECT COUNT(*) FROM league.Player p WHERE p.NFLTeamID = nt.NFLTeamID AND p.IsActive = 1) AS ActivePlayersCount
FROM ref.NFLTeam nt
LEFT JOIN auth.UserAccount creator ON creator.UserID = nt.CreatedByUserID
LEFT JOIN auth.UserAccount updater ON updater.UserID = nt.UpdatedByUserID;
GO

GRANT SELECT ON dbo.vw_NFLTeams TO app_executor;
GO

-- ============================================================================
-- vw_NFLTeamDetails
-- Vista detallada de equipos NFL incluyendo dimensiones de imágenes
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_NFLTeamDetails
AS
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
  creator.Email AS CreatedByEmail,
  nt.UpdatedAt,
  updater.Name AS UpdatedByName,
  updater.Email AS UpdatedByEmail
FROM ref.NFLTeam nt
LEFT JOIN auth.UserAccount creator ON creator.UserID = nt.CreatedByUserID
LEFT JOIN auth.UserAccount updater ON updater.UserID = nt.UpdatedByUserID;
GO

-- ============================================================================
-- vw_ActiveNFLTeams
-- Vista filtrada de equipos NFL activos (para selección en formularios)
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_ActiveNFLTeams
AS
SELECT
  NFLTeamID,
  TeamName,
  City,
  TeamImageUrl,
  ThumbnailUrl
FROM ref.NFLTeam
WHERE IsActive = 1;
GO

GRANT SELECT ON dbo.vw_ActiveNFLTeams TO app_executor;
GO

-- ============================================================================
-- vw_Players
-- Vista de jugadores NFL con información de equipo
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_Players
AS
SELECT
  p.PlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  p.NFLTeamID,
  nt.TeamName AS NFLTeamName,
  nt.City AS NFLTeamCity,
  p.InjuryStatus,
  p.InjuryDescription,
  p.PhotoUrl,
  p.PhotoThumbnailUrl,
  p.IsActive,
  p.CreatedAt,
  p.UpdatedAt,
  -- Indicador si está en algún roster de fantasy
  CASE WHEN EXISTS (
    SELECT 1 FROM league.TeamRoster tr 
    WHERE tr.PlayerID = p.PlayerID AND tr.IsActive = 1
  ) THEN 1 ELSE 0 END AS IsOnFantasyRoster
FROM league.Player p
LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID;
GO

GRANT SELECT ON dbo.vw_Players TO app_executor;
GO

-- ============================================================================
-- vw_AvailablePlayers
-- Jugadores que NO están en ningún roster activo (disponibles para draft/FA)
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_AvailablePlayers
AS
SELECT
  p.PlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  p.NFLTeamID,
  nt.TeamName AS NFLTeamName,
  nt.City AS NFLTeamCity,
  p.InjuryStatus,
  p.PhotoThumbnailUrl
FROM league.Player p
LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID
WHERE p.IsActive = 1
  AND NOT EXISTS (
    SELECT 1 
    FROM league.TeamRoster tr 
    WHERE tr.PlayerID = p.PlayerID 
      AND tr.IsActive = 1
  );
GO

GRANT SELECT ON dbo.vw_AvailablePlayers TO app_executor;
GO

-- ============================================================================
-- vw_PlayersByNFLTeam
-- Vista de jugadores agrupados por equipo NFL
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_PlayersByNFLTeam
AS
SELECT
  nt.NFLTeamID,
  nt.TeamName,
  nt.City,
  p.PlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  p.InjuryStatus,
  p.IsActive AS PlayerIsActive
FROM ref.NFLTeam nt
LEFT JOIN league.Player p ON p.NFLTeamID = nt.NFLTeamID
WHERE nt.IsActive = 1;
GO

GRANT SELECT ON dbo.vw_PlayersByNFLTeam TO app_executor;
GO

-- ============================================================================
-- vw_FantasyTeamDetails
-- Vista detallada de equipos fantasy con todas las imágenes y metadatos
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_FantasyTeamDetails
AS
SELECT
  t.TeamID,
  t.LeagueID,
  l.Name AS LeagueName,
  l.Status AS LeagueStatus,
  t.OwnerUserID,
  u.Name AS ManagerName,
  u.Email AS ManagerEmail,
  u.Alias AS ManagerAlias,
  t.TeamName,
  t.TeamImageUrl,
  t.TeamImageWidth,
  t.TeamImageHeight,
  t.TeamImageBytes,
  t.ThumbnailUrl,
  t.ThumbnailWidth,
  t.ThumbnailHeight,
  t.ThumbnailBytes,
  t.IsActive,
  t.CreatedAt,
  t.UpdatedAt,
  -- Estadísticas del roster
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1) AS RosterCount,
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1 AND tr.AcquisitionType = N'Draft') AS DraftedCount,
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1 AND tr.AcquisitionType = N'Trade') AS TradedCount,
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1 AND tr.AcquisitionType = N'FreeAgent') AS FreeAgentCount,
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1 AND tr.AcquisitionType = N'Waiver') AS WaiverCount
FROM league.Team t
JOIN league.League l ON l.LeagueID = t.LeagueID
JOIN auth.UserAccount u ON u.UserID = t.OwnerUserID;
GO

GRANT SELECT ON dbo.vw_FantasyTeamDetails TO app_executor;
GO

-- ============================================================================
-- vw_TeamRoster
-- Vista del roster de equipos fantasy con información completa de jugadores
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_TeamRoster
AS
SELECT
  tr.RosterID,
  tr.TeamID,
  t.TeamName,
  t.LeagueID,
  l.Name AS LeagueName,
  tr.PlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  p.NFLTeamID,
  nt.TeamName AS NFLTeamName,
  nt.City AS NFLTeamCity,
  nt.ThumbnailUrl AS NFLTeamLogo,
  p.InjuryStatus,
  p.InjuryDescription,
  p.PhotoUrl,
  p.PhotoThumbnailUrl,
  tr.AcquisitionType,
  tr.AcquisitionDate,
  tr.IsActive AS IsOnRoster,
  tr.DroppedDate,
  tr.AddedByUserID,
  adder.Name AS AddedByName
FROM league.TeamRoster tr
JOIN league.Team t ON t.TeamID = tr.TeamID
JOIN league.League l ON l.LeagueID = t.LeagueID
JOIN league.Player p ON p.PlayerID = tr.PlayerID
LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID
LEFT JOIN auth.UserAccount adder ON adder.UserID = tr.AddedByUserID;
GO

GRANT SELECT ON dbo.vw_TeamRoster TO app_executor;
GO

-- ============================================================================
-- vw_TeamRosterActive
-- Vista filtrada del roster solo con jugadores activos
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_TeamRosterActive
AS
SELECT
  tr.RosterID,
  tr.TeamID,
  t.TeamName,
  t.LeagueID,
  tr.PlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  nt.TeamName AS NFLTeamName,
  p.InjuryStatus,
  p.PhotoThumbnailUrl,
  tr.AcquisitionType,
  tr.AcquisitionDate
FROM league.TeamRoster tr
JOIN league.Team t ON t.TeamID = tr.TeamID
JOIN league.Player p ON p.PlayerID = tr.PlayerID
LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID
WHERE tr.IsActive = 1;
GO

GRANT SELECT ON dbo.vw_TeamRosterActive TO app_executor;
GO

-- ============================================================================
-- vw_TeamRosterByPosition
-- Vista del roster organizado por posición con orden lógico
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_TeamRosterByPosition
AS
SELECT
  tr.RosterID,
  tr.TeamID,
  t.TeamName,
  tr.PlayerID,
  p.FullName AS PlayerName,
  p.Position,
  nt.TeamName AS NFLTeamName,
  p.InjuryStatus,
  tr.AcquisitionType,
  -- Orden lógico de posiciones
  CASE p.Position
    WHEN 'QB' THEN 1
    WHEN 'RB' THEN 2
    WHEN 'WR' THEN 3
    WHEN 'TE' THEN 4
    WHEN 'K' THEN 5
    WHEN 'DEF' THEN 6
    WHEN 'DL' THEN 7
    WHEN 'LB' THEN 8
    WHEN 'CB' THEN 9
    ELSE 10
  END AS PositionOrder
FROM league.TeamRoster tr
JOIN league.Team t ON t.TeamID = tr.TeamID
JOIN league.Player p ON p.PlayerID = tr.PlayerID
LEFT JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID
WHERE tr.IsActive = 1;
GO

GRANT SELECT ON dbo.vw_TeamRosterByPosition TO app_executor;
GO

-- ============================================================================
-- vw_TeamRosterDistribution
-- Vista de distribución porcentual de jugadores por tipo de adquisición
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_TeamRosterDistribution
AS
SELECT
  t.TeamID,
  t.TeamName,
  t.LeagueID,
  tr.AcquisitionType,
  COUNT(*) AS PlayerCount,
  CAST(ROUND(
    COUNT(*) * 100.0 / NULLIF((
      SELECT COUNT(*) 
      FROM league.TeamRoster tr2 
      WHERE tr2.TeamID = t.TeamID AND tr2.IsActive = 1
    ), 0), 2
  ) AS DECIMAL(5,2)) AS Percentage
FROM league.Team t
LEFT JOIN league.TeamRoster tr ON tr.TeamID = t.TeamID AND tr.IsActive = 1
GROUP BY t.TeamID, t.TeamName, t.LeagueID, tr.AcquisitionType;
GO

GRANT SELECT ON dbo.vw_TeamRosterDistribution TO app_executor;
GO

-- ============================================================================
-- vw_NFLGames
-- Vista de partidos NFL con información de equipos
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_NFLGames
AS
SELECT
  g.NFLGameID,
  g.SeasonID,
  s.Label AS SeasonLabel,
  g.Week,
  g.HomeTeamID,
  ht.TeamName AS HomeTeamName,
  ht.City AS HomeTeamCity,
  ht.ThumbnailUrl AS HomeTeamLogo,
  g.AwayTeamID,
  at.TeamName AS AwayTeamName,
  at.City AS AwayTeamCity,
  at.ThumbnailUrl AS AwayTeamLogo,
  g.GameDate,
  g.GameTime,
  g.GameStatus,
  g.CreatedAt,
  g.UpdatedAt
FROM league.NFLGame g
JOIN league.Season s ON s.SeasonID = g.SeasonID
JOIN ref.NFLTeam ht ON ht.NFLTeamID = g.HomeTeamID
JOIN ref.NFLTeam at ON at.NFLTeamID = g.AwayTeamID;
GO

GRANT SELECT ON dbo.vw_NFLGames TO app_executor;
GO

-- ============================================================================
-- vw_NFLTeamSchedule
-- Vista del calendario de un equipo NFL (como local y visitante)
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_NFLTeamSchedule
AS
SELECT
  nt.NFLTeamID,
  nt.TeamName,
  g.NFLGameID,
  g.SeasonID,
  s.Label AS SeasonLabel,
  g.Week,
  g.GameDate,
  g.GameTime,
  g.GameStatus,
  CASE 
    WHEN g.HomeTeamID = nt.NFLTeamID THEN 'Home'
    ELSE 'Away'
  END AS HomeAway,
  CASE 
    WHEN g.HomeTeamID = nt.NFLTeamID THEN opp.TeamName
    ELSE home.TeamName
  END AS OpponentName,
  CASE 
    WHEN g.HomeTeamID = nt.NFLTeamID THEN opp.City
    ELSE home.City
  END AS OpponentCity
FROM ref.NFLTeam nt
JOIN league.NFLGame g ON (g.HomeTeamID = nt.NFLTeamID OR g.AwayTeamID = nt.NFLTeamID)
JOIN league.Season s ON s.SeasonID = g.SeasonID
LEFT JOIN ref.NFLTeam home ON home.NFLTeamID = g.HomeTeamID
LEFT JOIN ref.NFLTeam opp ON opp.NFLTeamID = g.AwayTeamID;
GO

GRANT SELECT ON dbo.vw_NFLTeamSchedule TO app_executor;
GO

GRANT SELECT ON dbo.vw_NFLTeamDetails TO app_executor;
GO

-- ============================================================================
-- vw_LeagueDirectory - VERSIÓN ACTUALIZADA
-- Incluye conteo de equipos activos
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_LeagueDirectory
AS
SELECT
  l.LeagueID,
  s.Label AS SeasonLabel,
  l.Name,
  l.Status,
  l.TeamSlots,
  TeamsCount = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  ActiveTeamsCount = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID AND t.IsActive = 1),
  AvailableSlots = l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  l.CreatedByUserID,
  l.CreatedAt
FROM league.League l
JOIN league.Season s ON s.SeasonID = l.SeasonID;
GO

GRANT SELECT ON dbo.vw_LeagueDirectory TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_LeagueMembers
AS
SELECT
  lm.LeagueID,
  lm.UserID,
  lm.RoleCode,
  lm.IsPrimaryCommissioner,
  lm.JoinedAt,
  lm.LeftAt,
  u.Name  AS UserName,
  u.Email AS UserEmail
FROM league.LeagueMember lm
JOIN auth.UserAccount   u ON u.UserID = lm.UserID;
GO

GRANT SELECT ON dbo.vw_LeagueMembers TO app_executor;
GO

-- ============================================================================
-- vw_LeagueTeams - VERSIÓN ACTUALIZADA
-- Incluye imágenes, thumbnails y estado de equipos
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_LeagueTeams
AS
SELECT
  t.TeamID,
  t.LeagueID,
  t.TeamName,
  t.OwnerUserID,
  u.Name AS OwnerName,
  u.Email AS OwnerEmail,
  u.Alias AS OwnerAlias,
  -- NUEVOS: Imágenes y thumbnails
  t.TeamImageUrl,
  t.TeamImageWidth,
  t.TeamImageHeight,
  t.ThumbnailUrl,
  t.ThumbnailWidth,
  t.ThumbnailHeight,
  -- NUEVO: Estado
  t.IsActive,
  t.CreatedAt,
  t.UpdatedAt,
  -- Estadísticas del roster
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1) AS RosterCount
FROM league.Team t
JOIN auth.UserAccount u ON u.UserID = t.OwnerUserID;
GO

GRANT SELECT ON dbo.vw_LeagueTeams TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_UserProfileHeader
AS
SELECT
  u.UserID,
  u.Email,
  u.Name,
  u.Alias,
  u.LanguageCode,
  u.ProfileImageUrl,
  u.AccountStatus,
  u.CreatedAt,
  u.UpdatedAt
FROM auth.UserAccount u;
GO

GRANT SELECT ON dbo.vw_UserProfileHeader TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_UserProfileBasic
AS
SELECT
  u.UserID,
  u.Email,
  u.Name,
  u.Alias,
  u.LanguageCode,
  u.ProfileImageUrl,
  u.ProfileImageWidth,
  u.ProfileImageHeight,
  u.ProfileImageBytes,
  u.AccountStatus,
  u.CreatedAt,
  u.UpdatedAt,
  CAST(N'MANAGER' AS NVARCHAR(20)) AS [Role] -- rol global inicial requerido por la historia
FROM auth.UserAccount u;
GO

GRANT SELECT ON dbo.vw_UserProfileBasic TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_UserCommissionedLeagues
AS
SELECT
  lm.UserID,
  lm.LeagueID,
  l.Name        AS LeagueName,
  l.Status,
  l.TeamSlots,
  (l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = lm.LeagueID)) AS AvailableSlots,
  lm.RoleCode,
  lm.IsPrimaryCommissioner,
  lm.JoinedAt,
  l.CreatedAt   AS LeagueCreatedAt
FROM league.LeagueMember lm
JOIN league.League l ON l.LeagueID = lm.LeagueID
WHERE lm.RoleCode IN (N'COMMISSIONER', N'CO_COMMISSIONER');
GO

GRANT SELECT ON dbo.vw_UserCommissionedLeagues TO app_executor;
GO

-- ============================================================================
-- vw_UserTeams - VERSIÓN ACTUALIZADA
-- Incluye imágenes y thumbnails de equipos del usuario
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_UserTeams
AS
SELECT
  t.OwnerUserID AS UserID,
  t.TeamID,
  t.LeagueID,
  l.Name AS LeagueName,
  l.Status AS LeagueStatus,
  t.TeamName,
  -- NUEVOS: Imágenes y thumbnails
  t.TeamImageUrl,
  t.ThumbnailUrl,
  -- NUEVO: Estado
  t.IsActive,
  t.CreatedAt AS TeamCreatedAt,
  t.UpdatedAt AS TeamUpdatedAt,
  -- Estadísticas
  (SELECT COUNT(*) FROM league.TeamRoster tr WHERE tr.TeamID = t.TeamID AND tr.IsActive = 1) AS RosterCount
FROM league.Team t
JOIN league.League l ON l.LeagueID = t.LeagueID;
GO

GRANT SELECT ON dbo.vw_UserTeams TO app_executor;
GO

-- ============================================================================
-- vw_LeagueSummary - VERSIÓN ACTUALIZADA
-- Incluye información mejorada de equipos fantasy
-- ============================================================================
CREATE OR ALTER VIEW dbo.vw_LeagueSummary
AS
SELECT
  l.LeagueID,
  l.Name,
  l.Description,
  l.Status,
  l.TeamSlots,
  TeamsCount = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  AvailableSlots = l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  ActiveTeamsCount = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID AND t.IsActive = 1),
  l.PlayoffTeams,
  l.AllowDecimals,
  l.TradeDeadlineEnabled,
  l.TradeDeadlineDate,
  l.MaxRosterChangesPerTeam,
  l.MaxFreeAgentAddsPerTeam,
  l.PositionFormatID,
  pf.Name AS PositionFormatName,
  l.ScoringSchemaID,
  ss.Name AS ScoringSchemaName,
  ss.Version AS ScoringVersion,
  l.SeasonID,
  s.Label AS SeasonLabel,
  s.Year,
  s.StartDate,
  s.EndDate,
  l.CreatedByUserID,
  u.Name AS CreatedByName,
  u.Email AS CreatedByEmail,
  l.CreatedAt,
  l.UpdatedAt
FROM league.League l
JOIN ref.PositionFormat pf ON pf.PositionFormatID = l.PositionFormatID
JOIN scoring.ScoringSchema ss ON ss.ScoringSchemaID = l.ScoringSchemaID
JOIN league.Season s ON s.SeasonID = l.SeasonID
JOIN auth.UserAccount u ON u.UserID = l.CreatedByUserID;
GO

GRANT SELECT ON dbo.vw_LeagueSummary TO app_executor;
GO

