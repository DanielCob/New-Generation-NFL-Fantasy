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

CREATE OR ALTER VIEW dbo.vw_LeagueDirectory
AS
SELECT
  l.LeagueID,
  s.Label       AS SeasonLabel,
  l.Name,
  l.Status,
  l.TeamSlots,
  TeamsCount     = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
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

CREATE OR ALTER VIEW dbo.vw_LeagueTeams
AS
SELECT
  t.TeamID,
  t.LeagueID,
  t.TeamName,
  t.OwnerUserID,
  u.Name     AS OwnerName,
  t.CreatedAt
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

CREATE OR ALTER VIEW dbo.vw_UserTeams
AS
SELECT
  t.OwnerUserID  AS UserID,
  t.TeamID,
  t.LeagueID,
  l.Name         AS LeagueName,
  t.TeamName,
  t.CreatedAt    AS TeamCreatedAt,
  l.Status       AS LeagueStatus
FROM league.Team t
JOIN league.League l ON l.LeagueID = t.LeagueID;
GO

GRANT SELECT ON dbo.vw_UserTeams TO app_executor;
GO

CREATE OR ALTER VIEW dbo.vw_LeagueSummary
AS
SELECT
  l.LeagueID,
  l.Name,
  l.Description,
  l.Status,
  l.TeamSlots,
  TeamsCount      = (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  AvailableSlots  = l.TeamSlots - (SELECT COUNT(*) FROM league.Team t WHERE t.LeagueID = l.LeagueID),
  l.PlayoffTeams,
  l.AllowDecimals,
  l.TradeDeadlineEnabled,
  l.TradeDeadlineDate,
  l.MaxRosterChangesPerTeam,
  l.MaxFreeAgentAddsPerTeam,
  l.PositionFormatID,
  pf.Name         AS PositionFormatName,
  l.ScoringSchemaID,
  ss.Name         AS ScoringSchemaName,
  ss.Version      AS ScoringVersion,
  l.SeasonID,
  s.Label         AS SeasonLabel,
  s.Year,
  s.StartDate,
  s.EndDate,
  l.CreatedByUserID,
  u.Name          AS CreatedByName,
  l.CreatedAt,
  l.UpdatedAt
FROM league.League l
JOIN ref.PositionFormat    pf ON pf.PositionFormatID = l.PositionFormatID
JOIN scoring.ScoringSchema ss ON ss.ScoringSchemaID  = l.ScoringSchemaID
JOIN league.Season         s  ON s.SeasonID          = l.SeasonID
JOIN auth.UserAccount      u  ON u.UserID            = l.CreatedByUserID;
GO