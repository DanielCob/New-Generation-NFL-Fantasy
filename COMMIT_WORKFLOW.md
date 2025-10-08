# Commit & Branch Workflow

> **Purpose**
> Keep a clean, searchable history using a **lightweight Conventional Commits** style and a simple **Git Flow** with 3 branch “tracks”.

---

## Branch Model

* **master** → **only** release-quality code. Contains **tags** for versions (`vX.Y.Z`).
* **develop** → integration of completed work for the current sprint.
* **feature/*** → short-lived branches for individual features/fixes.

### Branch naming

* `feature/<short-topic>` (optionally include ticket):
  `feature/login-session-token`, `feature/UMS-142-add-phone`
* `hotfix/<short-topic>` for urgent prod fixes:
  `hotfix/fix-null-district-check`

---

## Commit Message Format

We follow a compact Conventional-Commits-style header, optional body, optional footer.

```
<type>(<scope>): <subject>

<body>

<footer>
```

* **type** (required): one of `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
* **scope** (optional, lowercase): `api`, `sql`, `sp`, `view`, `func`, `ui`, `infra`, `auth`, etc.
* **subject** (required): imperative, ≤ 72 chars, no period at end
* **body** (optional): what changed and why (wrap at ~72 chars)
* **footer** (optional): `BREAKING CHANGE:` and/or references (`Refs #123`, `Closes #123`)

> **Language:** write commits in **English**.

### Quick rules

* One commit = one logical change (atomic).
* Prefer **squash merge** from feature branches so `develop` stays clean.
* Use **`BREAKING CHANGE:`** (or `type!:` in header) when a public contract changes.

---

## Commit Types (short guide)

* **feat**: new end-user or API capability
* **fix**: bug fix
* **docs**: documentation only (e.g., README, ADRs)
* **style**: formatting/whitespace; no code behavior change
* **refactor**: code change without new features or fixes
* **perf**: performance improvements
* **test**: add/adjust tests
* **build**: build system or external deps (npm, nuget)
* **ci**: CI/CD config (pipelines, workflows)
* **chore**: maintenance (rename folders, script scaffolding)
* **revert**: reverts a previous commit

### Suggested scopes for this project

* `sql` (DDL/migrations), `sp` (stored procedures), `view`, `func` (UDFs),
  `api` (backend services), `ui` (Angular), `infra` (scripts, docker, pipelines), `docs`.

---

## Examples

**Simple (like your example)**

```
chore: setup project structure and branch organization for NFL Fantasy
```

**Feature adding a column and SP support**

```
feat(sql): add Users.Phone and backfill

Added nullable Phone to Users with later NOT NULL enforcement after backfill.
Updated sp_CreateClient and sp_UpdateClient to accept @Phone.
Refs UMS-142.
```

**Procedure refactor**

```
refactor(sp): normalize error handling in sp_UserLogin

Replaced RAISERROR with THROW inside TRY/CATCH blocks and ensured
transaction safety. No behavior change expected.
```

**View enhancement**

```
feat(view): expose HasActiveSession in vw_AllEngineers

Adds a computed flag to help UI filter active accounts without extra queries.
```

**Fix with reference**

```
fix(func): validate canton-province relationship in fn_ValidateLocationHierarchy

Added early return when province-canton mismatch occurs.
Closes #231.
```

**Breaking change**

```
feat!(sp): change sp_CreateEngineer to require @Career

BREAKING CHANGE: sp_CreateEngineer now rejects NULL @Career to enforce data quality.
Update callers to provide a value.
```

**Revert**

```
revert: revert "feat(sql): add Users.Phone and backfill"

This reverts commit abcdef1 due to regression in view projections.
```

---

## Daily Workflow

### 1) Start a feature

```bash
git checkout develop
git pull
git checkout -b feature/<short-topic>
```

### 2) Commit as you work

* Small, atomic commits with the format above.
* If you touch multiple layers, prefer **separate commits**:

  * `feat(sql): ...`
  * `feat(sp): ...`
  * `feat(view): ...`

### 3) Open a PR → **develop**

* Title should follow the **same commit format** (semantic title).
* Checklist:

  * Tests updated/passing (when applicable)
  * DB scripts reviewed (if DDL)
  * Population script adjusted if needed
  * No breaking change unless clearly declared

> **Merge strategy:** **Squash & merge** into `develop` to produce one clean, semantic commit.

### 4) Release to **master**

* Create a **release PR** from `develop` → `master`
* Bump version (SemVer) and tag on merge:

  ```bash
  git checkout master
  git pull
  git tag -a v1.3.0 -m "Release v1.3.0"
  git push --tags
  ```
* `master` is always **tagged**, production-ready code.

### 5) Hotfixes

* Branch from `master`, then merge back to both `master` and `develop`.

```bash
git checkout master
git pull
git checkout -b hotfix/<short-topic>
# commit fix
git push && PR to master (fast-forward or merge)
git checkout develop
git merge --no-ff hotfix/<short-topic>
```

---

## SQL-Specific Notes (quick)

* DDL changes should use **additive** patterns when possible.
* For programmable objects use `CREATE OR ALTER` so diffs stay reviewable.
* If a DDL change requires seed updates, include a **separate** commit for `PopulationScriptDB.sql`:

  ```
  chore(population): backfill phone numbers for existing users
  ```
* If you introduce a breaking DB contract, **mark the commit as breaking** and add migration/rollback notes in the body.

---

## Minimal PR Checklist

* [ ] Commit messages follow format
* [ ] Tests/verification done (SQL and/or app)
* [ ] Affected SPs/views/functions updated
* [ ] Population script updated (if needed)
* [ ] No accidental changes (lockfiles, binaries)
* [ ] Issue linked (Refs/Closes)

---

## TL;DR

* Work on `feature/*` → **squash** into `develop`.
* Cut releases from `develop` into `master` → **tag** `vX.Y.Z`.
* Use **Conventional-like** commits with optional scopes: `feat(sql): ...`, `fix(sp): ...`.
* Declare **BREAKING CHANGE** explicitly.