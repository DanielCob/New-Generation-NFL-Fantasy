# New Generation NFL Fantasy — Monorepo

A modern, end-to-end NFL Fantasy platform designed for flexibility, transparency, and scale. This repository contains the three coordinated codebases that power the system: the REST API, the SQL Server database scripts, and the Angular web client. It is organized to support iterative delivery in two-week sprints with strong engineering practices, CI/CD, and traceable requirements.  

---

## Repository Structure

```
.
├── NFL-Fantasy_API/             # Backend REST API (business/domain services)
├── NFL-Fantasy_DataBase_SQL/    # Database creation, functions, and seed scripts
└── NFL-Fantasy_WEB_Angular/     # Front-end web app (Angular + Material)
```

Each folder is self-contained (build, run, test), but the three layers integrate through **versioned contracts** (SQL schema ↔ API DTOs ↔ UI models) and **automated pipelines**.

---

## What We’re Building (at a glance)

* **A next-generation Fantasy platform** with multiple draft modes (e.g., snake, auction, auto-rank), transparent pick logging, and role-based league management. 
* **Owned and controlled by the league** (no vendor lock-in) with clear non-functional requirements around availability, performance, security, and observability. 
* **Project governance & delivery** run in **Azure DevOps** (work items, boards, pipelines) with Git/GitHub as the centralized VCS; any cloud services are deployed to **Azure**. 

---

## High-Level Architecture

* **Data layer (SQL Server)**
  Authoritative schema (tables, constraints, views); programmable objects (SPs, UDFs); robust seed data for repeatable local and CI spins.
* **Service layer (API)**
  Stateless REST services exposing versioned endpoints (`/api/v1/...`). Encodes business rules (draft invariants, league rules, idempotency) and shields the database behind stored procedures when appropriate.
* **Presentation layer (Angular)**
  Responsive Material UI for login/registration, role-based dashboards (Admin, Engineer, Client placeholders), and draft/league features. It consumes the API using typed models and environment-specific base URLs.

---

## Standards & Operating Model

* **Work tracking & cadence**: Scrum, 2-week sprints; instructor acts as client representative; objectives and deliverables per sprint are defined and validated in advance. 
* **Source control**: Git (GitHub), trunk-based with short-lived feature branches.
* **CI/CD**: Azure DevOps pipelines; blue-green/canary encouraged for API; DB scripts applied in order with guards; UI built and deployed as static assets behind CDN. 
* **Security & compliance**: TLS 1.3 in transit; encryption at rest; RBAC; OWASP Top-10 hardening; zero secrets in repo; audited actions. 
* **Quality gates**: Unit test coverage ≥ 80% on changed code; static analysis clean; API contract (OpenAPI) updated per release; ADRs for key decisions. 

---

## Getting Started (Local)

> Prereqs (typical): SQL Server (Developer/Container), .NET SDK (for API), Node + Angular CLI (for web). Adjust versions per each subfolder’s README.

1. **Clone**

```bash
git clone <repo-url>
cd <repo-root>
```

2. **Database (SQL Server)**

```text
cd NFL-Fantasy_DataBase_SQL
-- Run in SQL Server (SSMS/sqlcmd):
--   1) CreateScriptDB.sql     -> creates DB, tables, constraints, core views/SPs
--   2) CreateFunctionsDB.sql  -> creates UDFs (kept separate to avoid logic load issues)
--   3) PopulationScriptDB.sql -> seeds reference and sample data
```

*Notes*: We deliberately separate creation, seeding, and functions. `GO` batches reset variable scope—keep that in mind when editing/adding statements. See the SQL workflow in this folder for safe alteration patterns (additive DDL, `CREATE OR ALTER` for programmable objects, dependency checks).

3. **API**

```bash
cd NFL-Fantasy_API
# set ConnectionStrings__UserManagementDB to your SQL instance
# set ASPNETCORE_ENVIRONMENT=Development
dotnet restore
dotnet run
```

The API should boot with `/api/v1` routes. Verify health/status and a simple read endpoint.

4. **Web (Angular)**

```bash
cd NFL-Fantasy_WEB_Angular
npm install
# set environment.ts apiUrl to your API (e.g., https://localhost:7221/api)
npm start
```

Open `http://localhost:4200`. You should see the login/register flows and role-based dashboards (placeholders wired to the auth endpoints).

---

## Environments & Configuration

* **Secrets & config** come from environment variables or the platform’s secret store (Azure Key Vault, pipeline secrets). Never commit secrets. 
* **API**: `appsettings.Development.json` for local defaults; connection strings injected via environment at runtime in higher envs.
* **Web**: `src/environments/environment*.ts` control `apiUrl` and flags per environment.
* **DB**: Scripts are **idempotent where possible** and guarded by existence checks; seeding is repeatable.

---

## Integration Contracts

* **SQL ↔ API**

  * Use **stored procedures** for critical mutations (e.g., create/update flows) to centralize validation and auditing.
  * Expose read models via **views** when needed for constrained access.
* **API ↔ Web**

  * Versioned endpoints (`/api/v1`).
  * Error payloads include code, cause, suggested action to meet UX error standards. 
  * Idempotency keys for actions like **draft picks** and **bids** to prevent duplicates. 

---

## Database Change Workflow (Summary)

All DB changes are performed by **editing these three scripts** (never ad-hoc changes in production):

1. **`CreateScriptDB.sql`** — Full creation (tables, PK/FK, indexes, baseline views/SPs).

   * Additive `ALTER TABLE` (avoid destructive drops).
   * Name constraints/indexes deterministically.

2. **`CreateFunctionsDB.sql`** — All UDFs (kept separate to avoid dependency load-order issues and recompilation pitfalls in SQL Server).

3. **`PopulationScriptDB.sql`** — All seed/reference data plus verification queries.

   * Use variables and keep `GO` batch boundaries in mind (they clear local variable scope).

> The goal is to **evolve** these files safely (add tables, alter columns, add/adjust SPs/UDFs/views) while preserving teammates’ work and ensuring repeatable deployments.

---

## Quality, Reliability & Observability (Targets)

* **Availability**: ≥ 99.95% monthly; ≥ 99.99% for draft windows.
* **Pick latency**: p95 ≤ 500 ms.
* **Disaster recovery**: RPO ≤ 1 min; RTO ≤ 15 min (critical).
* **Operational excellence**: tracing for critical flows, structured logs, dashboards before production. 

---

## Contributing

1. Create a **short-lived branch** off `main`: `feat/<scope>-<short-desc>`.
2. Commit using **Conventional Commits** (`feat:`, `fix:`, `docs:`, `refactor:`, etc.).
3. Open a PR with:

   * Tests updated/added, coverage ≥ 80% on changed code.
   * Updated OpenAPI (if API), UI models (if Web), and SQL scripts (if DB).
   * ADR if a significant decision was made. 

---

## Roadmap (Guided by Course Requirements)

Initial milestones emphasize **User & League Management** and the **Draft Engine** (real-time sessions, multiple draft modes, validation of unique picks), followed by team/player management, live views, community features, analytics/BI, and security & compliance hardening. Details and prioritization align with the provided requirement packs and will be refined per sprint objectives in Azure DevOps.  

---

## Support & Communications

Internal and client communications occur in the designated collaboration channels; all requests and decisions are tracked to maintain traceability (work items, attachments, confirmations). Sessions with the client are requested with sufficient notice and confirmed prior to execution. 

---

## License

MIT License.

---

**References**
Course overview & delivery model, tooling constraints, and governance come from the official project brief and requirement specification provided by the client/instructor.  
