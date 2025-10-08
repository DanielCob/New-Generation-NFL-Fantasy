# SQL Workflow — Single-Source, Three-File Model (SQL Server)

> **Purpose**
> We **do not** run ad-hoc commands on a live database. All changes flow through **three canonical scripts**, edited over time and re-runnable. This prevents overwriting teammates’ work, keeps security intact, and avoids batch/variable pitfalls with `GO`.

---

## The Three Canonical Files (what each one owns)

1. **`CreateScriptDB.sql`** — *Schema + non-UDF programmable objects*

   * Creates **database**, **schemas**, **roles/permissions**, **tables**, **constraints**, **indexes**, **views**, **stored procedures** (that **don’t** require UDFs).
   * Contains a **post-UDF section** (see below) for objects that **depend on functions**; these blocks are guarded and safe to re-run after functions exist.

2. **`CreateFunctionsDB.sql`** — *All UDFs only (scalar and TVFs)*

   * Separated to avoid compile-order issues in SQL Server.
   * Re-runnable with `CREATE OR ALTER`.

3. **`PopulationScriptDB.sql`** — *Seed/reference/test data only*

   * Idempotent upserts (no duplicates), **no schema** here.
   * Avoid cross-`GO` variables; keep batches independent.
   * Optional verification queries at the end.

> **Execution in a clean environment**
> `CreateScriptDB.sql` → `CreateFunctionsDB.sql` → re-run the **post-UDF section** in `CreateScriptDB.sql` (or the whole file; it’s idempotent) → `PopulationScriptDB.sql`.

---

## Non-negotiable Principles

* **Idempotency**

  * Use `IF NOT EXISTS` for **tables/indexes/constraints**.
  * Use `CREATE OR ALTER` for **procedures/views/functions/triggers**.
* **Batch safety with `GO`**

  * Variables **do not** survive across `GO`.
  * Keep any logic that *needs* a variable’s value within a **single batch**.
  * If you must split, **persist lookups** via stable keys (e.g., unique names) and re-select in the next batch; do **not** rely on variables across batches.
* **Security first**

  * App connects via a **least-privilege role** (e.g., `app_executor`), granted `EXECUTE` on a schema or specific SPs; **no direct table DML**.
  * Expose reads via **views**; grant `SELECT` on views only to app role.
  * Keep permission DDL in `CreateScriptDB.sql` so re-runs preserve grants.
* **Backward-compatible changes**

  * Prefer **additive** table changes. Use **two-phase rename** (add new → migrate → switch consumers → remove old later).
* **One source of truth**

  * All schema/data changes live in these files; PRs update them—no side scripts.

---

## Standard File Layouts & Markers

### 1) `CreateScriptDB.sql`

```sql
/* ============================================================
   CreateScriptDB.sql
   UserManagementDB — Schema + Non-UDF Programmable Objects
   Re-runnable. Do not remove teammates' sections.
   ============================================================ */

-- 0) Database & Options
IF DB_ID(N'UserManagementDB') IS NULL
BEGIN
  CREATE DATABASE UserManagementDB;
END
GO

ALTER DATABASE UserManagementDB SET RECOVERY SIMPLE;
-- (other db options as needed)
GO

USE UserManagementDB;
GO

-- 1) Security & Schemas
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'app')
    EXEC('CREATE SCHEMA app AUTHORIZATION dbo;');
GO

-- Roles (least privilege)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app_executor')
    CREATE ROLE app_executor AUTHORIZATION dbo;
GO

-- Grant policy: app can EXEC on schema app; SELECT only via views
GRANT EXECUTE ON SCHEMA::app TO app_executor;
-- Example: GRANT SELECT ON dbo.vw_* TO app_executor (applied after view creation)

-- 2) Reference Tables (create if missing)
IF OBJECT_ID('dbo.UserTypes','U') IS NULL
BEGIN
  CREATE TABLE dbo.UserTypes (
    UserTypeID  INT IDENTITY(1,1) CONSTRAINT PK_UserTypes PRIMARY KEY,
    TypeCode    NVARCHAR(50) NOT NULL CONSTRAINT UQ_UserTypes_TypeCode UNIQUE,
    CreatedAt   DATETIME NOT NULL CONSTRAINT DF_UserTypes_CreatedAt DEFAULT(GETDATE())
  );
END
GO

-- 3) Core Domain Tables (Users, Engineers, etc.) using IF NOT EXISTS + ALTER patterns
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
  CREATE TABLE dbo.Users (
    UserID       INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    Email        NVARCHAR(255) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    BirthDate    DATE NOT NULL,
    UserTypeID   INT NOT NULL,
    ProvinceID   INT NOT NULL,
    CantonID     INT NOT NULL,
    DistrictID   INT NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
    CreatedAt    DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(GETDATE()),
    UpdatedAt    DATETIME NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT(GETDATE())
  );

  CREATE INDEX IX_Users_UserTypeID ON dbo.Users(UserTypeID);
  -- FKs created after all parent tables exist (see section 4)
END
GO

-- 4) Constraints & FKs (guarded, add if missing; safe to re-run)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Users_UserTypes')
BEGIN
  ALTER TABLE dbo.Users WITH CHECK
  ADD CONSTRAINT FK_Users_UserTypes
    FOREIGN KEY (UserTypeID) REFERENCES dbo.UserTypes(UserTypeID) ON DELETE NO ACTION;
END
GO

-- 5) Views (that do NOT require UDFs)
CREATE OR ALTER VIEW dbo.vw_AllClients AS
SELECT /* columns */ FROM dbo.Users WHERE UserTypeID = 1;
GO
GRANT SELECT ON dbo.vw_AllClients TO app_executor;
GO

-- 6) Stored Procedures (that do NOT require UDFs)
CREATE OR ALTER PROCEDURE app.sp_CreateClient
  @Username NVARCHAR(100), @Email NVARCHAR(255), @BirthDate DATE,
  @ProvinceID INT, @CantonID INT, @DistrictID INT = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    BEGIN TRAN;
      -- validations + insert
    COMMIT;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO
GRANT EXECUTE ON OBJECT::app.sp_CreateClient TO app_executor;
GO

/* 7) POST-UDF DEPENDENT OBJECTS
   Keep any view/SP that calls a UDF here; guard with IF OBJECT_ID for the UDF.
   After running CreateFunctionsDB.sql, re-run ONLY this section or the whole file. */

IF OBJECT_ID('dbo.fn_ValidateLocationHierarchy','FN') IS NOT NULL
BEGIN
  CREATE OR ALTER PROCEDURE app.sp_CreateEngineer
    @Username NVARCHAR(100), @Email NVARCHAR(255), @BirthDate DATE,
    @ProvinceID INT, @CantonID INT, @DistrictID INT = NULL,
    @Career NVARCHAR(200), @Specialization NVARCHAR(200) = NULL
  AS
  BEGIN
    SET NOCOUNT ON;
    IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
      THROW 50000, 'Invalid location hierarchy', 1;

    BEGIN TRY
      BEGIN TRAN;
        -- insert Users + Engineers
      COMMIT;
    END TRY
    BEGIN CATCH
      IF @@TRANCOUNT > 0 ROLLBACK;
      THROW;
    END CATCH
  END;
  GRANT EXECUTE ON OBJECT::app.sp_CreateEngineer TO app_executor;
END
GO
```

> **Why this structure?**
>
> * Teammates append safely under the right section without deleting others’ blocks.
> * UDF-dependent objects won’t fail compile on a clean DB—they’re guarded and re-runnable.

---

### 2) `CreateFunctionsDB.sql`

```sql
/* ============================================================
   CreateFunctionsDB.sql
   UserManagementDB — All UDFs (scalar/TVF)
   Re-runnable with CREATE OR ALTER
   ============================================================ */

USE UserManagementDB;
GO

CREATE OR ALTER FUNCTION dbo.fn_ValidateLocationHierarchy
(
  @ProvinceID INT,
  @CantonID   INT,
  @DistrictID INT = NULL
)
RETURNS BIT
AS
BEGIN
  DECLARE @ok BIT = 0;

  IF EXISTS (SELECT 1 FROM dbo.Cantons WHERE CantonID=@CantonID AND ProvinceID=@ProvinceID)
  BEGIN
    IF @DistrictID IS NULL
      SET @ok = 1;
    ELSE IF EXISTS (SELECT 1 FROM dbo.Districts WHERE DistrictID=@DistrictID AND CantonID=@CantonID)
      SET @ok = 1;
  END

  RETURN @ok;
END
GO
```

> Keep **all** function logic here. If you add/edit/remove a function, do it in this file only.

---

### 3) `PopulationScriptDB.sql`

```sql
/* ============================================================
   PopulationScriptDB.sql
   UserManagementDB — Seed / Reference / Test Data
   Idempotent. No schema DDL here.
   ============================================================ */

USE UserManagementDB;
GO
SET NOCOUNT ON;

-- A) Seed reference rows (use stable natural keys)
IF NOT EXISTS (SELECT 1 FROM dbo.UserTypes WHERE TypeCode = 'CLIENT')
  INSERT INTO dbo.UserTypes(TypeCode) VALUES ('CLIENT');
IF NOT EXISTS (SELECT 1 FROM dbo.UserTypes WHERE TypeCode = 'ENGINEER')
  INSERT INTO dbo.UserTypes(TypeCode) VALUES ('ENGINEER');
IF NOT EXISTS (SELECT 1 FROM dbo.UserTypes WHERE TypeCode = 'ADMIN')
  INSERT INTO dbo.UserTypes(TypeCode) VALUES ('ADMIN');
GO

-- B) Seed locations (sample)
IF NOT EXISTS (SELECT 1 FROM dbo.Provinces WHERE ProvinceName = N'San José')
  INSERT INTO dbo.Provinces(ProvinceName) VALUES (N'San José');
GO

-- C) Seed app accounts via SPs (business rules respected)
DECLARE @message NVARCHAR(500);

-- Keep each SP call self-contained in a single batch (no variable reuse across GO)
EXEC app.sp_CreateClient
  @Username = N'client1',
  @Email = N'client1@example.com',
  @BirthDate = '1990-01-10',
  @ProvinceID = (SELECT TOP 1 ProvinceID FROM dbo.Provinces WHERE ProvinceName=N'San José'),
  @CantonID   = (SELECT TOP 1 CantonID   FROM dbo.Cantons   WHERE CantonName=N'Central'),
  @DistrictID = (SELECT TOP 1 DistrictID FROM dbo.Districts WHERE DistrictName=N'Carmen');
GO

-- D) Verification (non-breaking)
SELECT TOP 5 UserID, Email, CreatedAt FROM dbo.Users ORDER BY UserID DESC;
```

> **Population rules**
>
> * **Never** assume variable values across `GO`.
> * Use **unique natural keys** for lookups between batches.
> * Prefer calling SPs (enforces validation & security) instead of raw INSERTs.

---

## How to Make Changes (by editing the three files)

> The goal is to **append or alter** the right section without removing colleagues’ blocks.

### A) Add a new **table**

1. **`CreateScriptDB.sql`**

   * Under **Core Domain Tables**, add `IF OBJECT_ID(...,'U') IS NULL CREATE TABLE ...`.
   * Under **Constraints & FKs**, add guarded FK/unique/check constraints and indexes.
   * If the table should be exposed read-only → add a **view** in the **Views** section and `GRANT SELECT` to `app_executor`.
   * If the table needs mutations from the app → add **SPs** in the **Stored Procedures** section and `GRANT EXECUTE`.

2. **`PopulationScriptDB.sql`**

   * Seed reference rows using **IF NOT EXISTS**; use stable natural keys.

3. **Security**

   * Only SPs get `EXECUTE` grants; no direct table grants to app role.

### B) Modify a **table** (integration-safe)

* **Add a column**
  In `CreateScriptDB.sql`, after the table block:

  ```sql
  IF COL_LENGTH('dbo.Users','Phone') IS NULL
    ALTER TABLE dbo.Users ADD Phone NVARCHAR(50) NULL;
  ```

  * If you need NOT NULL, **backfill** in `PopulationScriptDB.sql`, then:

    ```sql
    ALTER TABLE dbo.Users ALTER COLUMN Phone NVARCHAR(50) NOT NULL;
    ```

* **Two-phase rename** (safe)

  1. Add new column; 2) backfill & switch SPs/views; 3) drop old column in a later PR.

* **Change FK behavior**
  Drop named FK then add new one (guard each with `IF EXISTS`/`IF NOT EXISTS`).

* **Remember** to update dependent **views/SPs** (either directly here, or in the **post-UDF** section if they call a UDF).

### C) Add/Edit/Delete a **stored procedure** or **view**

* Do it in `CreateScriptDB.sql`. Always **`CREATE OR ALTER`**.

* If it depends on a UDF, place it in the **POST-UDF** section with
  `IF OBJECT_ID('dbo.fn_X','FN') IS NOT NULL BEGIN ... END`.

* Apply grants right after definition:

  ```sql
  GRANT EXECUTE ON OBJECT::app.sp_MyProc TO app_executor;
  -- or:
  GRANT SELECT ON dbo.vw_MyView TO app_executor;
  ```

* **Deleting** an SP/view: wrap in guarded drop:

  ```sql
  IF OBJECT_ID('app.sp_OldProc','P') IS NOT NULL DROP PROCEDURE app.sp_OldProc;
  ```

### D) Add/Edit/Delete a **function (UDF)**

* Only in `CreateFunctionsDB.sql`, with **`CREATE OR ALTER FUNCTION`**.
* If signature/behavior changes, update dependent SPs/views in the **post-UDF** section and re-run that section (or the entire create script).

### E) Update **seed data**

* Only in `PopulationScriptDB.sql`. Use **IF NOT EXISTS** (or safe upsert) to stay idempotent.
* Never rely on variables across `GO`. Split batches or use stable re-selects by unique keys.

---

## `GO` Usage — What’s Safe vs Risky

| Pattern                                                          | Safe? | Notes                                                                                        |
| ---------------------------------------------------------------- | ----- | -------------------------------------------------------------------------------------------- |
| Re-running `CREATE OR ALTER` objects across multiple `GO` blocks | ✅     | Server stores the latest definition                                                          |
| Using variables across `GO`                                      | ❌     | Variables reset; use stable key lookups instead                                              |
| Temp tables across `GO`                                          | ⚠️    | Survive in the same connection; deployment tools may split connections—avoid relying on this |
| Transactions across `GO`                                         | ❌     | `GO` ends the batch; keep each transaction within a batch                                    |
| `SCOPE_IDENTITY()` across `GO`                                   | ❌     | Use unique keys and re-select in next batch                                                  |

---

## Security Patterns to Keep

* **Role-based access**

  * Create and use `app_executor`; **grant EXEC on schema** (or specific SPs).
  * For reads, **grant SELECT on views** only.
* **No table grants** to app; DML goes through SPs.
* **Schema separation**

  * Keep app-facing procedures in `app` schema (easy to grant/revoke).
* **Auditing hooks** (optional)

  * Add `UpdatedAt` default to `GETDATE()` and set in SP updates.
* **Sensitive data**

  * Hash/seal secrets in SPs (never raw in scripts).

---

## Safe Dependency Checks (before dropping/renaming)

```sql
-- Who references this object?
SELECT OBJECT_SCHEMA_NAME(referencing_id) AS SchemaName,
       OBJECT_NAME(referencing_id)       AS ObjectName,
       o.type_desc
FROM sys.sql_expression_dependencies d
JOIN sys.objects o ON o.object_id = d.referencing_id
WHERE d.referenced_id = OBJECT_ID('dbo.YourObjectName');
```

---

## PR / Change Checklist (don’t skip)

* [ ] Changes placed in the **correct file & section** (schema vs UDF vs data).
* [ ] **Idempotent** guards present (`IF NOT EXISTS`, `CREATE OR ALTER`).
* [ ] **No reliance** on variables across `GO` in population script.
* [ ] **Grants** updated (EXEC on SPs, SELECT on views) and **no table grants** to app role.
* [ ] **UDF dependencies** placed in **post-UDF** section (or properly guarded).
* [ ] **Population script** updated to reflect new/changed reference data.
* [ ] Added/updated **verification queries** at the end of population.
* [ ] Ran **dependency check** before any DROP or breaking change.
* [ ] Documented **two-phase renames** and deferred drops where needed.
* [ ] Smoke-tested: full run on empty DB in this order: Create → Functions → (re-run post-UDF) → Populate.

---

## Example: Add “Projects” with an SP and a view

**`CreateScriptDB.sql`**

```sql
-- Table (guarded)
IF OBJECT_ID('dbo.Projects','U') IS NULL
BEGIN
  CREATE TABLE dbo.Projects(
    ProjectID  INT IDENTITY(1,1) CONSTRAINT PK_Projects PRIMARY KEY,
    Name       NVARCHAR(200) NOT NULL CONSTRAINT UQ_Projects_Name UNIQUE,
    OwnerUserID INT NOT NULL,
    CreatedAt  DATETIME NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT(GETDATE()),
    UpdatedAt  DATETIME NOT NULL CONSTRAINT DF_Projects_UpdatedAt DEFAULT(GETDATE())
  );
  CREATE INDEX IX_Projects_OwnerUserID ON dbo.Projects(OwnerUserID);
END
GO

-- FK
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Projects_Users_OwnerUserID')
BEGIN
  ALTER TABLE dbo.Projects
    ADD CONSTRAINT FK_Projects_Users_OwnerUserID
    FOREIGN KEY (OwnerUserID) REFERENCES dbo.Users(UserID) ON DELETE NO ACTION;
END
GO

-- View (non-UDF)
CREATE OR ALTER VIEW dbo.vw_Projects AS
SELECT p.ProjectID, p.Name, p.CreatedAt, u.UserID, u.Email
FROM dbo.Projects p JOIN dbo.Users u ON u.UserID = p.OwnerUserID;
GO
GRANT SELECT ON dbo.vw_Projects TO app_executor;
GO

-- SP (non-UDF)
CREATE OR ALTER PROCEDURE app.sp_CreateProject
  @Name NVARCHAR(200), @OwnerUserID INT
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    BEGIN TRAN;
      IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserID=@OwnerUserID AND IsActive=1)
        THROW 50000, 'Owner not found or inactive.', 1;

      INSERT INTO dbo.Projects(Name, OwnerUserID) VALUES (@Name, @OwnerUserID);
    COMMIT;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO
GRANT EXECUTE ON OBJECT::app.sp_CreateProject TO app_executor;
GO
```

**`PopulationScriptDB.sql`**

```sql
-- Seed a project via SP (idempotent by unique Name)
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE Name = N'Onboarding Revamp')
BEGIN
  EXEC app.sp_CreateProject
       @Name = N'Onboarding Revamp',
       @OwnerUserID = (SELECT TOP 1 UserID FROM dbo.Users WHERE Email='admin.super@admin.com');
END
GO

-- Verify
SELECT TOP 5 * FROM dbo.vw_Projects ORDER BY ProjectID DESC;
```

---

## Final Notes

* We build by **editing these three scripts only**—never out-of-band.
* Keep changes **incremental, guarded, and re-runnable**.
* Use the **post-UDF** pattern to decouple compile order.
* Respect **security boundaries** (SPs + views + roles; no table grants).

Follow this, and we’ll evolve the database safely without stepping on each other’s toes—and the Angular app will always have a predictable, secure contract to consume.
