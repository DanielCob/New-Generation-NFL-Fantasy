# Pull Request Workflow

> How we create, review, and merge Pull Requests for the XNFL Fantasy project.

---

## 1. Goals and Principles

Our Pull Request (PR) process exists to:

* Keep `master` always **release-ready** and `develop` **stable**.
* Make changes **reviewable**: small, focused PRs with clear description and evidence.
* Guarantee that every merged PR meets our **Definition of Done (DoD)** across:

  * **SQL** → 5-file model
  * **.NET API**
  * **Angular frontend**

**Golden rules**

* Prefer **small PRs** (ideal: ≤ 200–300 lines of real code changes).
* One PR = **one clearly identifiable feature or bugfix**.
* PR title follows the **same style as commit messages** (Conventional-like, in English).
* We use **Squash & Merge** from `feature/*` into `develop`.

---

## 2. Branches and PR Targets

* `master` → production. Updated only from:

  * **Release** PR from `develop`.
  * Specific **hotfix** PR.
* `develop` → integration of work ready for the next version.

  * Every feature/fix arrives via PR from `feature/*`.
* `feature/<short-topic>` → short-lived branch per feature or bug.

  * Examples: `feature/player-status-10-3`, `feature/league-filters`.

**Rules**

1. Always branch from the latest `develop`.
2. One `feature/*` branch per topic.
3. When the feature is ready according to the DoD → open PR to `develop`.

---

## 3. PR Title and Commit Style

### 3.1. PR Title

The PR title follows the same format as our commits:

```text
<type>(<scope>): short description
```

* **type**: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
* **scope** (optional): `sql`, `api`, `ui`, `sp`, `view`, `infra`, etc.
* **subject**: imperative, ≤ 72 chars, in English, no period at the end.

**Examples**

```text
feat(api): integrate player designation and news lifecycle
fix(sql): correct IR/PUP constraint for player status
refactor(ui): simplify player profile state badges
```

### 3.2. Commits inside the PR

* Commits also follow the same format.
* Keep commits **atomic** per layer when possible:

  * `feat(sql): ...`
  * `feat(api): ...`
  * `feat(ui): ...`

When we **Squash & Merge**, the PR title becomes the resulting commit in `develop`.

---

## 4. PR Template (to use in every PR)

Copy-paste this template into the PR description and fill it out.

```md
# Summary

Briefly describe the feature or fix being introduced. Mention the domain
(e.g., player status 10.3) and the main user-facing behavior.

# Rationale

Why is this change needed? What problem does it solve or what capability
does it add (for managers, admins, projections, alerts, etc.)?

# Design Documentation

- [ ] Link to design doc / spec / ticket (if exists)
- [ ] Mention impacted layers:
  - [ ] SQL (5-file model)
  - [ ] .NET API
  - [ ] Angular

# Changes

List the major changes in this PR, grouped by layer:

- **SQL**
  - Tables/columns added or modified
  - New/updated SPs or views
- **API**
  - New endpoints or parameters
  - Validation, auth, logging, Swagger updates
- **Angular**
  - New pages/components/routes/services
  - States (loading/empty/error) and validations

# Impact

Discuss potential impacts on existing functionalities:

- Eligibility rules, lineups, projections, alerts, BI, audit
- Backward compatibility (are existing clients affected?)
- Performance or security considerations

# Testing

Describe how this has been tested:

- Manual tests (happy path + at least 1 negative case)
- Automated tests (unit/integration) if applicable
- Environment & dataset used (local DB, seed data, etc.)

Include the **5-min smoke test** evidence:

- [ ] JSON request/response from the main endpoint(s)
- [ ] SQL query results used to verify behavior
- [ ] Short explanation of the negative test performed

# Screenshots/Video

Include screenshots or a short GIF if the UI changed:

- [ ] Player profile / listing
- [ ] Error / validation states (if relevant)

# Definition of Done (DoD) Checklist

- [ ] **Compiles and starts**  
      .NET and Angular compile without errors and the app runs locally
      with the feature enabled.

- [ ] **SQL migrations ready**  
      UP/DOWN scripts versioned, idempotent, tested on local DB with
      minimal dataset. Rollback verified: DOWN executed and system left
      in a consistent state.

- [ ] **API functional**  
      Endpoints implemented according to design with basic validations
      and correct HTTP codes (2xx/4xx/401). Swagger/OpenAPI updated
      including request/response examples.

- [ ] **UI connected**  
      Angular components call the API. Minimum states implemented:
      loading / empty / error + basic form validations. No errors in
      browser console.

- [ ] **Smoke test E2E (5-min)**  
      One happy-path flow executed end-to-end (SQL → API → UI) plus one
      negative case (validation or 401). Evidence attached above
      (screens/gif + JSON or SQL screenshot).

- [ ] **Handoff by stage respected**  
      Each stage checked (Design → SQL → API → Angular → MinIO if
      applicable) before handing off. No stage was skipped.

- [ ] **Minimum security applied**  
      Protected endpoints require Bearer token and a 401 was verified.
      Public endpoints are justified here. No secrets in repo and new
      variables documented in `.env.sample`.

- [ ] **Minimum observability**  
      API handles errors/logs without exposing raw stacktraces. SQL
      queries avoid obvious full table scans on large tables when
      applicable.

- [ ] **Lightweight documentation**  
      Short notes in this PR (≤ 10 lines) explaining: what it does, how
      to test it, data preconditions, test routes, and links to Swagger
      and SQL migration.

- [ ] **Version control hygiene**  
      Branch `feature/*`, small PR, clear commit messages, no temp files
      or secrets committed.

# Checklist

- [ ] Code follows the project's coding standards
- [ ] Unit/integration tests for the new feature have been added (if applicable)
- [ ] All existing tests pass
- [ ] Documentation/README updated when needed

# Additional Notes

Any extra context relevant to this PR (known limitations, follow-up
tasks, feature flags, etc.).
```

---

## 5. Layered Workflow Inside a PR

Each PR should make it clear what happened in each layer. We rely on the SQL, API and Frontend workflows.

### 5.1. SQL Changes (5-File Canonical Model)

We **never** run ad-hoc SQL in production. Every modification must:

1. Touch the right files, in this logical order:

   ```text
   01CreateTablesDB.sql
   02CreateFunctionsDB.sql
   03CreateSPsDB.sql
   04CreateViewsDB.sql
   05PopulationScriptDB.sql
   ```

2. Follow the model rules:

   * `IF OBJECT_ID(...) IS NULL` for tables/indexes.
   * `CREATE OR ALTER` for SPs, functions and views.
   * Grants (`GRANT EXECUTE/SELECT`) immediately after each object.
   * `MERGE` or `IF NOT EXISTS` in data population.
   * No variables or transactions across `GO`.

3. In the PR, **explain**:

   * Which tables/columns/indexes are added or changed.
   * Which SPs/views are created/updated and why.
   * How UP and DOWN were tested (rollback).
   * Any impact on views or BI reports.

### 5.2. API Changes (.NET)

For every feature that touches the API, the PR must verify that the flow:

```text
Stored Procedure / View → DataAccess → Service → Controller → Swagger
```

is complete and consistent.

It should include:

* New DTOs or contract changes in `/Models/DTOs/...`.
* Corresponding DataAccess calling SPs/views (no inline SQL).
* Services with business rules, logging and error handling.
* **Thin** Controllers focused on HTTP (status codes + ModelState).
* Registration of services and DataAccess in `Program.cs`.
* Correct authentication and authorization (`[Authorize]`, policies).
* Swagger/OpenAPI updates with examples.

The PR description should clarify:

* New routes (`GET/POST/...`) and their parameters/roles.
* What validations are added or changed.
* Whether it affects existing clients (breaking or not).

### 5.3. Frontend Changes (Angular)

Each UI change must follow the **Frontend Workflow**:

* New types in `core/models`.
* New methods in `core/services` aligned with the API.
* Standalone components, lazy routes and guards (`authGuard`, `roleGuard`).
* Minimum states: loading / empty / error + form validations.
* No new warnings or errors in the browser console.
* `MatSnackBar` with standard styles for feedback.

In the PR:

* Specify new routes (`/admin/...`, `/client/...`, etc.).
* Describe UX changes (for example, how player states are displayed).
* Attach screenshots or a GIF showing the happy path.

---

## 6. Before You Open a PR (Self-Review Checklist)

As a **developer**, before asking for review:

1. **Review your own code.**

2. Verify that the branch:

   * Compiles .NET and Angular without errors.
   * Runs the SQL scripts (UP and DOWN) on a fresh database.
   * Passes the E2E smoke test with evidence.

3. Keep the PR **small and focused** (ideal ≤ 200–300 lines).

4. Include the right people: someone for SQL when DB is touched, someone for API, someone for UI as needed.

5. Cover key questions:

   * Is the interaction with existing code clear (**design**)?
   * Can the solution be simplified (**complexity**)?
   * Does it follow our conventions and patterns (**style**)?
   * Does the code actually do what it claims, without obvious logic bugs (**functionality**)?
   * Are there reasonable manual and/or automated tests (**testing**)?

---

## 7. How to Review a PR

### 7.1. What to Look For

Based on the review guide:

* **Design**

  * How do the new parts interact with existing ones?
  * Does it break any established contract or pattern?
* **Complexity**

  * Could this be implemented in a simpler way?
  * Is it easy to understand when reading the code?
* **Style**

  * Does it follow project conventions (names, layers, patterns)?
  * Is separation SQL / DataAccess / Service / Controller / UI respected?
* **Functionality**

  * Does the code really do what it is supposed to?
  * Are there obvious logic flaws or uncovered edge cases?
* **Testing**

  * Were tests run before submitting the change?
  * Is there at least one happy path and one negative case?
  * Can you easily repeat the tests from the PR description?

### 7.2. Reviewer Guidelines

* Reserve daily time to help teammates by reviewing PRs.
* Be **explicit** in comments:

  * Avoid “this could be better”.
  * Prefer “this function could be extracted because it is repeated in X and Y”.
* Critique the **code**, not the person:

  * Focus on quality and risks of the change.
* Lead by example:

  * Use a friendly, constructive tone and assume good intent.
* Double-check comments:

  * Remember the author has usually spent a lot of time thinking about the problem.

### 7.3. Developer Guidelines (receiving feedback)

* Be open to suggestions; review improves the product and the team.
* Answer reviewers’ questions as soon as possible.
* Keep PRs at a manageable size:

  * If it grows too much, consider splitting the work into multiple PRs.
* Include everyone in the review process:

  * Members with more and less experience to foster learning.

---

## 8. PR Lifecycle

1. **Start feature branch**

   ```bash
   git checkout develop
   git pull
   git checkout -b feature/<short-topic>
   ```

2. **Work and commit**

   * Apply the SQL, API and Angular workflows.
   * Make small, clear commits.

3. **Self-review + DoD**

   * Verify the DoD and layer checklists.
   * Add test evidence and links to Swagger, migrations, etc.

4. **Open PR to `develop`**

   * Title using Conventional style (`feat(api): ...`).
   * Description using the **PR template** in full.
   * Add SQL/API/UI reviewers according to impact.

5. **Review and iterations**

   * Address comments, adjust code, answer questions.
   * Check DoD and checklist items as they are verified.

6. **Approval**

   * Minimal requirements (can be tuned by team policy):

     * 1 approval from someone on backend (SQL/API) when server code changes.
     * 1 approval from someone on frontend when UI changes.
   * For sensitive schema changes (large tables, core business rules):

     * Requires explicit review from a database-focused person.

7. **Merge**

   * Use **Squash & Merge** into `develop`.
   * The final merge message should be the PR title (or an improved version).
   * Delete the `feature/*` branch after merge (local and remote).

8. **Release**

   * When several PRs are in `develop` and we’re ready to release:

     * Create a `develop` → `master` release PR with summary of changes.
     * Tag the version (`vX.Y.Z`) on `master`.

---

## 9. Summary

* Every change begins in **SQL (5 files)**, moves up through **DataAccess → Service → Controller → Angular**, and ends in a **small, well-described, well-tested PR**.
* The **PR template** and the **DoD** are mandatory.
* Reviewer and developer work together with clear, respectful feedback.
* The result: a clean history, easy search, and features that reach production with solid design, basic tests, and full traceability.