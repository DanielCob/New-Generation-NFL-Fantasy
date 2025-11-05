# SQL Workflow — Five-File Canonical Model

**Never run ad-hoc SQL.** All changes flow through these 5 files in order:

```
01CreateTablesDB.sql → 02CreateFunctionsDB.sql → 03CreateSPsDB.sql → 04CreateViewsDB.sql → 05PopulationScriptDB.sql
```

---

## The Five Files

### 1. `01CreateTablesDB.sql` — Database, Schemas, Tables, Constraints, Indexes

**Contains:**
- `CREATE DATABASE` + options
- Schemas: `auth`, `league`, `ref`, `scoring`, `audit`, `app`
- Role: `app_executor`
- All tables with computed columns
- PKs, FKs, UQs, CKs (named explicitly)
- Indexes (including filtered unique)
- Schema-level grants

**Pattern:**
```sql
IF DB_ID(N'XNFLFantasyDB') IS NULL CREATE DATABASE XNFLFantasyDB;
GO
USE XNFLFantasyDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'auth')
  EXEC('CREATE SCHEMA auth AUTHORIZATION dbo;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'app_executor')
  CREATE ROLE app_executor AUTHORIZATION dbo;
GO

IF OBJECT_ID('auth.UserAccount','U') IS NULL
CREATE TABLE auth.UserAccount (
  UserID INT IDENTITY(1,1),
  Email NVARCHAR(255) NOT NULL,
  -- ...
  CONSTRAINT PK_UserAccount PRIMARY KEY(UserID)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UQ_UserAccount_Email')
  ALTER TABLE auth.UserAccount ADD CONSTRAINT UQ_UserAccount_Email UNIQUE(Email);
GO
```

**Grant schemas:**
```sql
GRANT EXECUTE ON SCHEMA::app TO app_executor;
GO
```

---

### 2. `02CreateFunctionsDB.sql` — Scalar & Table-Valued Functions

**Contains:**
- Helper functions used by SPs/Views
- Use `CREATE OR ALTER`

**Pattern:**
```sql
CREATE OR ALTER FUNCTION dbo.fn_HashPassword(@Password NVARCHAR(255))
RETURNS NVARCHAR(255)
AS
BEGIN
  RETURN CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password), 2);
END
GO

GRANT EXECUTE ON OBJECT::dbo.fn_HashPassword TO app_executor;
GO
```

---

### 3. `03CreateSPsDB.sql` — Application Stored Procedures

**Contains:**
- All business logic under `app` schema
- Use `CREATE OR ALTER`
- Always wrap in `BEGIN TRY/CATCH` + `BEGIN TRAN/COMMIT`
- Always audit significant actions to `audit.*` tables
- Grant EXECUTE immediately after each SP

**Required pattern:**
```sql
CREATE OR ALTER PROCEDURE app.sp_RegisterUser
  @Name NVARCHAR(100),
  @Email NVARCHAR(255),
  @Password NVARCHAR(255),
  @SourceIp NVARCHAR(45) = NULL,
  @UserAgent NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validation
    IF @Email IS NULL OR @Email = N''
      THROW 50001, 'Email is required', 1;

    BEGIN TRAN;
      -- Business logic
      INSERT INTO auth.UserAccount(Name, Email, PasswordHash)
      VALUES(@Name, @Email, dbo.fn_HashPassword(@Password));

      -- Audit
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, ActionCode, SourceIp, UserAgent)
      VALUES(SCOPE_IDENTITY(), N'USER', N'REGISTER', @SourceIp, @UserAgent);
    COMMIT;

    SELECT N'User registered successfully' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_RegisterUser TO app_executor;
GO
```

---

### 4. `04CreateViewsDB.sql` — Read-Only Views

**Contains:**
- UI-focused projections
- Use `CREATE OR ALTER`
- Grant SELECT immediately after each view
- **Never grant SELECT on tables to app_executor**

**Pattern:**
```sql
CREATE OR ALTER VIEW dbo.vw_CurrentSeason
AS
SELECT SeasonID, Label, Year, StartDate, EndDate, IsCurrent, CreatedAt
FROM league.Season
WHERE IsCurrent = 1;
GO

GRANT SELECT ON dbo.vw_CurrentSeason TO app_executor;
GO
```

---

### 5. `05PopulationScriptDB.sql` — Seed & Demo Data

**Contains:**
- Reference data (roles, formats, schemas)
- Demo users/leagues
- Verification queries at the end

**Rules:**
- Use `MERGE` or `IF NOT EXISTS`
- **Never use variables across `GO`** — re-select by stable keys
- Prefer calling SPs for complex data (enforces validation)
- No DDL here

**Pattern:**
```sql
USE XNFLFantasyDB;
GO
SET NOCOUNT ON;

-- Reference data
MERGE auth.SystemRole AS T
USING (VALUES
  (N'ADMIN', N'Administrator'),
  (N'USER', N'Regular User')
) AS S(RoleCode, Display)
ON T.RoleCode = S.RoleCode
WHEN NOT MATCHED THEN INSERT(RoleCode, Display) VALUES(S.RoleCode, S.Display);
GO

-- Demo users (through SP)
DECLARE @tmp TABLE(UserID INT, Message NVARCHAR(200));
IF NOT EXISTS (SELECT 1 FROM auth.UserAccount WHERE Email=N'admin@demo.com')
BEGIN
  INSERT INTO @tmp EXEC app.sp_RegisterUser
    @Name=N'Admin', @Email=N'admin@demo.com', @Password=N'Pass123';
END
GO

-- Verification
SELECT COUNT(*) AS TotalUsers FROM auth.UserAccount;
GO
```

---

## Naming Conventions (Mandatory)

| Object    | Pattern                           | Example                        |
|-----------|-----------------------------------|--------------------------------|
| PK        | `PK_TableName`                    | `PK_UserAccount`               |
| FK        | `FK_Child_Parent`                 | `FK_Session_UserAccount`       |
| UQ        | `UQ_TableName_Column`             | `UQ_UserAccount_Email`         |
| CK        | `CK_TableName_Meaning`            | `CK_UserAccount_StatusValid`   |
| IX        | `IX_TableName_Column`             | `IX_Session_ExpiresAt`         |
| View      | `dbo.vw_SubjectName`              | `dbo.vw_UserTeams`             |
| SP        | `app.sp_VerbNoun`                 | `app.sp_CreateLeague`          |
| Function  | `dbo.fn_FunctionName`             | `dbo.fn_HashPassword`          |

---

## Making Changes

### Add a table
1. **01**: Add guarded `CREATE TABLE` + constraints + indexes
2. **03**: Add SPs to interact with it
3. **04**: Add view if needed for read-only access
4. **05**: Seed reference data if applicable

### Add/modify a column
```sql
-- Add nullable first
IF COL_LENGTH('auth.UserAccount','NewCol') IS NULL
  ALTER TABLE auth.UserAccount ADD NewCol NVARCHAR(100) NULL;
GO

-- Backfill in 05, then make NOT NULL in next PR
```

### Add/modify SP/Function/View
- Edit in **02/03/04** with `CREATE OR ALTER`
- Grant immediately after

### Delete SP/Function/View
```sql
IF OBJECT_ID('app.sp_OldProc','P') IS NOT NULL
  DROP PROCEDURE app.sp_OldProc;
GO
```

---

## Security Model

- **`app_executor`** has:
  - `EXECUTE` on `app` schema (granted in 01)
  - `EXECUTE` on individual functions (granted in 02)
  - `SELECT` on views only (granted in 04)
  - **NO access to tables**

- All SPs audit to `audit.*` tables
- Passwords hashed via functions, never stored plain

---

## `GO` Rules

| Pattern                          | Safe? | Notes                                    |
|----------------------------------|:-----:|------------------------------------------|
| `CREATE OR ALTER` across `GO`    | ✅     | Server stores latest definition          |
| Variables across `GO`            | ❌     | Variables reset — re-select by key       |
| Transactions across `GO`         | ❌     | Keep each transaction in single batch    |
| `SCOPE_IDENTITY()` across `GO`   | ❌     | Re-select by unique key in next batch    |

---

## PR Checklist

- [ ] Changes in correct file (01=DDL, 02=Functions, 03=SPs, 04=Views, 05=Data)
- [ ] Idempotent guards (`IF NOT EXISTS`, `CREATE OR ALTER`)
- [ ] Named constraints/indexes explicitly
- [ ] Grants added immediately after objects
- [ ] No variables across `GO` in 05
- [ ] SPs have `TRY/CATCH` + transactions + auditing
- [ ] Smoke test: run all 5 files in order on empty DB

---

**That's it.** Follow the 5-file structure, use guards, grant immediately, never cross `GO` with variables.