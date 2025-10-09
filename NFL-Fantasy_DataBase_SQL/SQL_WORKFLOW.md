# SQL Workflow — Four-File Canonical Model (SQL Server)

> **Purpose**
> We **never** run ad-hoc SQL on live databases. Every schema/object/data change flows through **four canonical scripts**, designed to be **idempotent**, **re-runnable**, and **reviewable** in PRs. This keeps order-of-compile safe, preserves security, and avoids `GO`/batch pitfalls.

---

## The Four Canonical Files (ownership & responsibilities)

1. **`01CreateTablesDB.sql` — Database, Security, Schemas, Tables & Constraints**

   * Creates **database**, **schemas** (`auth`, `league`, `ref`, `scoring`, `audit`, and `app`), **roles/permissions** (e.g., `app_executor`), **tables**, **PKs**, **FKs**, **CHECK/UNIQUE** constraints, and **indexes** (including filtered unique indexes).
   * **No** procedures or views here. Keep it **pure DDL** + grants that belong to tables/schemas.

2. **`02CreateSPsDB.sql` — Application Stored Procedures (and optional UDF helpers)**

   * All **application APIs** live here under the **`app`** schema (e.g., `app.sp_RegisterUser`, `app.sp_Login`, `app.sp_CreateLeague`, …).
   * Use `CREATE OR ALTER`. Handle **transactions**, **TRY/CATCH**, **validation**, and **auditing**.
   * If we ever add UDFs, place them **at the top** of this file (guarded) so any dependent SP compiles after.

3. **`03CreateViewsDB.sql` — Read-only Views (front-end surfaces)**

   * Views that power the UI (e.g., `dbo.vw_CurrentSeason`, `dbo.vw_PositionFormats`, `dbo.vw_UserTeams`, …).
   * Use `CREATE OR ALTER`. **Grant `SELECT`** on views to `app_executor`. Do **not** grant tables to the app role.

4. **`04PopulationScriptDB.sql` — Seed / Reference / Demo Data**

   * Idempotent **upserts** (e.g., `MERGE` or `IF NOT EXISTS`), no schema DDL.
   * Prefer seeding **through SPs** to enforce business rules (e.g., create users/leagues via `app.*`).
   * Include **verification queries** at the end. Avoid cross-`GO` variables.

> **Execution order (clean environment)**
> `01CreateTablesDB.sql` → `02CreateSPsDB.sql` → `03CreateViewsDB.sql` → `04PopulationScriptDB.sql`.

---

## Non-Negotiable Principles

* **Idempotency**

  * `IF NOT EXISTS` for **tables/indexes/constraints**.
  * `CREATE OR ALTER` for **procedures/views/(optional) functions**.
  * Deterministic **names** for constraints & indexes (so re-runs are safe).

* **Batch safety (`GO`)**

  * Variables **do not** cross `GO`. Re-select by **stable keys** (natural or unique).
  * Keep each transaction **within one batch**.

* **Security first**

  * App uses **least-privilege** role (e.g., `app_executor`).
  * **EXECUTE** on `app` schema; **SELECT** on **views only**; **no direct table DML** to app role.
  * Keep **schema/role grants** where they belong: table/schema grants in **01**, SP/view grants in **02/03** right after object creation.

* **Backward-compatible changes**

  * Prefer additive changes. For renames: **two-phase** (add → backfill → switch consumers → drop later).

* **Single source of truth**

  * All changes live in these four files. PRs modify them—**no side scripts**.

---

## Standard File Layouts & Markers

### 1) `01CreateTablesDB.sql`  *(DDL only)*

```sql
/* ============================================================
   01CreateTablesDB.sql
   DB, Security, Schemas, Tables, Constraints, Indexes (DDL only)
   Re-runnable. Do not remove teammates' sections.
   ============================================================ */

-- 0) Database & Options
IF DB_ID(N'YourDB') IS NULL CREATE DATABASE YourDB;
GO
ALTER DATABASE YourDB SET RECOVERY SIMPLE;
GO
USE YourDB;
GO

-- 1) Schemas (auth, league, ref, scoring, audit, app)
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'auth')   EXEC('CREATE SCHEMA auth AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'league') EXEC('CREATE SCHEMA league AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'ref')    EXEC('CREATE SCHEMA ref AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'scoring')EXEC('CREATE SCHEMA scoring AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'audit')  EXEC('CREATE SCHEMA audit AUTHORIZATION dbo;');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'app')    EXEC('CREATE SCHEMA app AUTHORIZATION dbo;');
GO

-- 2) Roles & Base Grants (least-privilege)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'app_executor')
    CREATE ROLE app_executor AUTHORIZATION dbo;
GO
-- App gets EXEC on app schema later (02). Views grants live in 03.

-- 3) Reference Tables (guarded)
--    Example: ref.LeagueRole, ref.PositionFormat, scoring.ScoringSchema...
--    Use explicit PK/UK/CK names; deterministic index names.

-- 4) Core Domain Tables (guarded)
--    Example: auth.UserAccount, auth.Session, league.Season, league.League, etc.

-- 5) Constraints & FKs (guarded, name them explicitly)
--    Example:
-- IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Session_User')
--   ALTER TABLE auth.Session ADD CONSTRAINT FK_Session_User
--   FOREIGN KEY(UserID) REFERENCES auth.UserAccount(UserID) ON DELETE CASCADE;

-- 6) Indexes (including filtered uniques)
--    Example: unique current season, single primary commissioner, etc.
```

**Naming conventions (required):**

| Object             | Pattern (example)                     |
| ------------------ | ------------------------------------- |
| PK                 | `PK_<Table>`                          |
| FK                 | `FK_<ChildTable>_<ParentTable>_<Col>` |
| Unique constraint  | `UQ_<Table>_<Col or Meaning>`         |
| Check constraint   | `CK_<Table>_<Meaning>`                |
| Default constraint | `DF_<Table>_<Col>`                    |
| Index              | `IX_<Table>_<ColOrPurpose>`           |
| View               | `dbo.vw_<Subject>`                    |
| Procedure          | `app.sp_<VerbNoun>`                   |

---

### 2) `02CreateSPsDB.sql`  *(application API)*

```sql
/* ============================================================
   02CreateSPsDB.sql
   Application Stored Procedures (and optional UDF helpers)
   Re-runnable with CREATE OR ALTER
   ============================================================ */

USE YourDB;
GO

-- (Optional) UDF helpers (place BEFORE any SP that depends on them)
-- CREATE OR ALTER FUNCTION dbo.fn_Something(...) RETURNS ... AS BEGIN ... END
-- GO

-- Grants: ensure app schema exists (created in 01). Grant EXEC per object.
-- CREATE OR ALTER PROCEDURE app.sp_RegisterUser ... AS BEGIN ... END
-- GO
-- GRANT EXECUTE ON OBJECT::app.sp_RegisterUser TO app_executor;
-- GO

-- Transaction & error pattern (required):
-- BEGIN TRY BEGIN TRAN ... COMMIT; END TRY
-- BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH
```

**What lives here**

* **Auth & Session** SPs (register, login, validate/refresh, logout/all, password reset).
* **League lifecycle** SPs (create league, set status, edit config).
* **Read endpoints** that return shaped result sets (e.g., summaries) when they require logic/validation beyond a view.

**Granting**
Grant right after each object definition:

```sql
GRANT EXECUTE ON OBJECT::app.sp_MyProc TO app_executor;
```

---

### 3) `03CreateViewsDB.sql`  *(read surfaces)*

```sql
/* ============================================================
   03CreateViewsDB.sql
   Read-only Views for the Front-end
   Re-runnable with CREATE OR ALTER
   ============================================================ */

USE YourDB;
GO

-- Examples (minimal, UI-focused projections; no side effects):
-- CREATE OR ALTER VIEW dbo.vw_CurrentSeason AS SELECT ... FROM league.Season WHERE IsCurrent=1;
-- GO
-- GRANT SELECT ON dbo.vw_CurrentSeason TO app_executor;
-- GO

-- Keep one GRANT per view; no table grants to app role.
```

**Guidelines**

* Views should be **stable contracts** for the UI and **contain no business logic** that belongs in SPs.
* Prefer **narrow, purposeful** views (e.g., `vw_UserActiveSessions`, `vw_PositionFormatSlots`, `vw_LeagueSummary`).

---

### 4) `04PopulationScriptDB.sql`  *(seed & verification)*

```sql
/* ============================================================
   04PopulationScriptDB.sql
   Seed / Reference / Demo Data (idempotent; no DDL)
   ============================================================ */

USE YourDB;
GO
SET NOCOUNT ON;

-- A) Catalogs & reference (MERGE or IF NOT EXISTS)
-- MERGE ref.LeagueRole AS T USING (...) AS S(...) ON (...) WHEN MATCHED ... WHEN NOT MATCHED ...

-- B) Business templates (e.g., scoring schemas & rules)

-- C) Seasons (ensure single current via filtered unique index in 01)

-- D) Demo users/leagues through SPs (enforces validation)
--   INSERT INTO @tmp EXEC app.sp_RegisterUser ...;
--   EXEC app.sp_CreateLeague ...;

-- E) Verification queries (read-only, safe to re-run)
--   SELECT TOP 5 * FROM dbo.vw_LeagueDirectory ORDER BY CreatedAt DESC;
```

**Population rules**

* **Never** rely on variables across `GO`.
* Use **stable natural keys** for lookups between batches.
* Prefer **calling SPs** to respect validations and security.

---

## How to Make Changes (by editing the four files)

### A) Add a new **table**

1. **`01CreateTablesDB.sql`**

   * Add guarded `CREATE TABLE` with **explicit** PK/UK/CK and deterministic names.
   * Add **FKs** and **indexes** (guarded).
   * Do **not** add SPs/views here.

2. **`02CreateSPsDB.sql`**

   * Add SPs to mutate/read the table as needed; wrap in TRY/CATCH + TRAN; grant EXEC.

3. **`03CreateViewsDB.sql`**

   * If the table should be exposed read-only, add a **view** and `GRANT SELECT` to `app_executor`.

4. **`04PopulationScriptDB.sql`**

   * Seed reference rows via **MERGE/IF NOT EXISTS**; prefer **SPs** for demo data.

### B) Modify a **table** safely

* **Add a column**

  ```sql
  IF COL_LENGTH('schema.Table','NewCol') IS NULL
    ALTER TABLE schema.Table ADD NewCol NVARCHAR(100) NULL;
  ```

  If `NOT NULL` is required, **backfill** in `04` first, then `ALTER` to `NOT NULL`.

* **Two-phase rename**

  1. Add `NewCol` → 2) backfill & switch SPs/views to `NewCol` → 3) drop `OldCol` in a later PR.

* **Change FK behavior**
  Guarded drop/add:

  ```sql
  IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Child_Parent_Col')
    ALTER TABLE child DROP CONSTRAINT FK_Child_Parent_Col;
  IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Child_Parent_Col')
    ALTER TABLE child ADD CONSTRAINT FK_Child_Parent_Col FOREIGN KEY(Col) REFERENCES parent(Col) ON DELETE CASCADE;
  ```

* Update dependent **SPs/views** accordingly (in **02/03**).

### C) Add/Edit/Delete a **stored procedure**

* Do it in **`02CreateSPsDB.sql`** with `CREATE OR ALTER`.
* Apply **grant** immediately after:

  ```sql
  GRANT EXECUTE ON OBJECT::app.sp_Name TO app_executor;
  ```
* Deleting:

  ```sql
  IF OBJECT_ID('app.sp_Old','P') IS NOT NULL DROP PROCEDURE app.sp_Old;
  ```

### D) Add/Edit/Delete a **view**

* Do it in **`03CreateViewsDB.sql`** with `CREATE OR ALTER`.
* Grant:

  ```sql
  GRANT SELECT ON dbo.vw_MyView TO app_executor;
  ```

### E) Update **seed data**

* Only in **`04PopulationScriptDB.sql`** with idempotent patterns.
* Keep **verification queries** at the end.

---

## `GO` Usage — Safe vs Risky

| Pattern                                                          | Safe? | Notes                                                        |
| ---------------------------------------------------------------- | :---: | ------------------------------------------------------------ |
| Re-running `CREATE OR ALTER` objects across multiple `GO` blocks |   ✅   | Server stores latest definition                              |
| Using variables across `GO`                                      |   ❌   | Variables reset; re-select via stable keys                   |
| Temp tables across `GO`                                          |   ⚠️  | Don’t rely on connection persistence in deployment tools     |
| Transactions across `GO`                                         |   ❌   | `GO` ends batch; keep each transaction inside a single batch |
| `SCOPE_IDENTITY()` across `GO`                                   |   ❌   | Re-select by unique key in next batch                        |

---

## Security Model (keep)

* **Role-based access**

  * `app_executor` exists in **01**.
  * **02** grants `EXECUTE` on **`app`** procedures.
  * **03** grants `SELECT` on **views** only.
  * **No table grants** to the app role.

* **Schema separation**

  * Procedures under `app` (easy grant/revoke).
  * Domain tables under `auth`, `league`, `ref`, `scoring`, `audit`.

* **Auditing**

  * SPs should write to `audit.*` tables for significant actions.

* **Sensitive data**

  * Hash/salt secrets **inside SPs**; never inline secrets in scripts.

---

## Dependency Inspection (before breaking changes)

```sql
SELECT OBJECT_SCHEMA_NAME(referencing_id) AS SchemaName,
       OBJECT_NAME(referencing_id)       AS ObjectName,
       o.type_desc
FROM sys.sql_expression_dependencies d
JOIN sys.objects o ON o.object_id = d.referencing_id
WHERE d.referenced_id = OBJECT_ID('schema.ObjectName');
```

---

## PR / Change Checklist

* [ ] Placed changes in the **correct file** (01 DDL, 02 SPs, 03 Views, 04 Data).
* [ ] **Idempotent** guards present (`IF NOT EXISTS`, `CREATE OR ALTER`).
* [ ] No reliance on variables across `GO` in **04**.
* [ ] **Grants** updated (EXEC on SPs in 02; SELECT on views in 03; no table grants to app).
* [ ] Deterministic **constraint/index names** added for new objects.
* [ ] Backward-compatible path considered (two-phase rename/backfill as needed).
* [ ] **Verification queries** present in 04 for new seeds.
* [ ] **Dependency check** run before drops/renames.
* [ ] Full smoke test on empty DB in order: **01 → 02 → 03 → 04**.

---

## Worked Example (end-to-end pattern)

> **Goal:** Introduce a new read surface for “user teams”.

1. **01**: *(no change — data already lives in `league.Team`)*
2. **02**: *(no change — read is simple)*
3. **03**:

```sql
CREATE OR ALTER VIEW dbo.vw_UserTeams AS
SELECT
  t.OwnerUserID  AS UserID,
  t.TeamID,
  t.LeagueID,
  l.Name       AS LeagueName,
  t.TeamName,
  t.CreatedAt  AS TeamCreatedAt,
  l.Status     AS LeagueStatus
FROM league.Team t
JOIN league.League l ON l.LeagueID = t.LeagueID;
GO
GRANT SELECT ON dbo.vw_UserTeams TO app_executor;
GO
```

4. **04**: *(optional)* add a verification query:

```sql
SELECT TOP 5 * FROM dbo.vw_UserTeams ORDER BY TeamCreatedAt DESC;
```

---

## Final Notes

* These **four files are the only sources of truth** for DB creation/evolution.
* Keep objects **guarded, named, and re-runnable**.
* Maintain **strict boundaries**: 01 (DDL) → 02 (SPs) → 03 (Views) → 04 (Data).
* Favor SPs for business rules & auditing; Views for clean UI shapes; Seeds for predictable demos/tests.

Following this structure lets us evolve the database safely, keep the API contract predictable, and make deployments repeatable across environments.