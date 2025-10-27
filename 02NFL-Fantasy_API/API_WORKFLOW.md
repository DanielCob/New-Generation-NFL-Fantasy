# API Integration Workflow (README) 
# Image DataBase Added

> **Goal**
> Keep a strict 1:1 mapping between **Database objects** (Tables, Views, Stored Procedures) and our **.NET API** layers. This document explains **where** to put new files, **what** to edit, and **how** to keep security and organization consistent.

---

## Project layout (recap)

```
/Controllers/*.cs                     → Web endpoints (HTTP, validation, response codes)
/Data/DatabaseHelper.cs               → Direct SQL access (no EF); SP & view helpers
/Middleware/AuthenticationMiddleware.cs→ Bearer GUID token auth + role checks
/Models/DTOs/*.cs                     → Inbound/Outbound DTOs for API contracts
/Models/Entities/*.cs                 → Entity reference models (schema documentation)
/Models/ViewModels/*.cs               → View projections (shape of SQL Views)
/Services/Interfaces/*.cs             → Service contracts
/Services/Implementations/*.cs        → Business logic + DB calls via DatabaseHelper
appsettings.json                      → Connection string & config
Program.cs                            → DI container, Swagger, CORS, middleware
```

**Architecture rules**

* **Controllers** are *thin*: validate input, enforce result codes, call **Services**.
* **Services** are *where logic lives*: call **DatabaseHelper** for SPs/Views and map to DTOs/ViewModels.
* **DatabaseHelper** is the *only* place allowed to execute SQL.
* **DTOs** = request/response of controllers.
  **ViewModels** = exact shape of SQL **views**.
  **Entities** = schema reference (not used by EF; kept for clarity).
* **Auth**: `AuthenticationMiddleware` reads a **Bearer {GUID}** token, resolves `UserId`, `UserType`, adds them to `HttpContext.Items`, and enforces **ADMIN-only** routes.

---

## Adding a **new SQL table** to the API

> Example: you add table `Projects` and SPs `sp_CreateProject`, `sp_UpdateProject`, `sp_DeleteProject`, `sp_GetProjectById`.

### 1) Database (outside the API)

* Create the **table** and constraints.
* Create **SPs** for create/update/delete/get and (optionally) list.
* (Optional) Create **views** if you need projected read models.

### 2) API files to **create**

1. **DTOs** (API contracts) → `/Models/DTOs/ProjectDTOs.cs`

   * `CreateProjectDTO`, `UpdateProjectDTO` (request)
   * `ProjectResponseDTO` (response)

   ```csharp
   // /Models/DTOs/ProjectDTOs.cs
   public class CreateProjectDTO { /* properties + [Required] */ }
   public class UpdateProjectDTO { /* all optional fields */ }
   public class ProjectResponseDTO { /* what the API returns */ }
   ```

2. **Entity** (schema reference) → `/Models/Entities/Project.cs`
   Mirror columns (use `[Key]`, `[Required]`, etc.). This is documentation-only in our current stack.

   ```csharp
   // /Models/Entities/Project.cs
   public class Project { public int ProjectID {get;set;} /* ... */ }
   ```

3. **Service interface** → `/Services/Interfaces/IProjectService.cs`

   ```csharp
   public interface IProjectService {
       Task<ApiResponseDTO> CreateAsync(CreateProjectDTO dto);
       Task<ApiResponseDTO> UpdateAsync(int id, UpdateProjectDTO dto);
       Task<ApiResponseDTO> DeleteAsync(int id);
       Task<ProjectResponseDTO?> GetByIdAsync(int id);
   }
   ```

4. **Service implementation** → `/Services/Implementations/ProjectService.cs`
   Use `DatabaseHelper` to call each SP. Map **SqlParameter[]** carefully and convert DB rows → DTOs.

   ```csharp
   public class ProjectService : IProjectService {
       private readonly DatabaseHelper _db;
       public ProjectService(IConfiguration cfg) { _db = new DatabaseHelper(cfg); }

       public async Task<ApiResponseDTO> CreateAsync(CreateProjectDTO dto) {
           var p = new SqlParameter[] {
               new("@Name", dto.Name),
               new("@ProjectID", SqlDbType.Int){ Direction = ParameterDirection.Output }
           };
           var result = await _db.ExecuteStoredProcedureAsync<object>(
               "sp_CreateProject",
               p,
               r => new { NewProjectID = DatabaseHelper.GetSafeInt32(r,"NewProjectID"),
                          Message = DatabaseHelper.GetSafeString(r,"Message") });
           return new ApiResponseDTO { Success = true, Message = "Created", Data = result };
       }

       public async Task<ProjectResponseDTO?> GetByIdAsync(int id) {
           // Prefer SP if available. If you must read a table directly use ExecuteViewAsync against a FROM clause.
           return (await _db.ExecuteViewAsync<ProjectResponseDTO>(
               /* FROM */ "Projects",
               r => new ProjectResponseDTO { /* map columns */ },
               /* WHERE */ $"ProjectID = {id}"     // safe because id is int route param
           )).FirstOrDefault();
       }

       // UpdateAsync → call sp_UpdateProject with optional parameters (DBNull when null)
       // DeleteAsync → call sp_DeleteProject
   }
   ```

5. **Controller** → `/Controllers/ProjectController.cs`
   Route pattern: `api/project`. Validate `ModelState`, use try/catch, return `ActionResult`.

   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class ProjectController : ControllerBase {
       private readonly IProjectService _svc;
       public ProjectController(IProjectService svc) { _svc = svc; }

       [HttpPost] // public or protected? see security section
       public async Task<ActionResult<ApiResponseDTO>> Create([FromBody] CreateProjectDTO dto) { /* ... */ }

       [HttpPut("{id}")]
       public async Task<ActionResult<ApiResponseDTO>> Update(int id, [FromBody] UpdateProjectDTO dto) { /* ... */ }

       [HttpDelete("{id}")]
       public async Task<ActionResult<ApiResponseDTO>> Delete(int id) { /* ... */ }

       [HttpGet("{id}")]
       public async Task<ActionResult<ProjectResponseDTO>> GetById(int id) { /* ... */ }
   }
   ```

6. **DI registration** → add to `Program.cs`

   ```csharp
   builder.Services.AddScoped<IProjectService, ProjectService>();
   ```

### 3) Security & routing

* **Public or protected?** If endpoints must be public (like registration), add their paths to `ShouldSkipAuthentication` in `/Middleware/AuthenticationMiddleware.cs`.
* **Role restrictions**: If endpoints are **ADMIN-only**, ensure the path triggers the ADMIN gate in `HasRequiredRole` or put them under `/api/admin/...`.

> **Tip**: For new “admin tools”, prefer adding endpoints to **`AdminController`** or create `/Controllers/Admin<Project>Controller.cs` with route prefix `/api/admin/...`.

### 4) Tests & Swagger

* Swagger is pre-configured. For protected endpoints, click **Authorize**, paste `Bearer {GUID}` from `/api/auth/login`.

---

## Adding a **SQL View** (and exposing it)

> Example: you create `vw_ProjectsSummary`.

### 1) Database

* Create the **view** and verify its column names.

### 2) API files to **create/modify**

1. **ViewModel** → `/Models/ViewModels/ProjectSummaryViewModel.cs`
   Match **exact** view columns (names & types you will read).

   ```csharp
   public class ProjectSummaryViewModel {
       public int ProjectID {get;set;}
       public string Name {get;set;} = string.Empty;
       public DateTime CreatedAt {get;set;}
       public bool IsActive {get;set;}
       // etc.
   }
   ```

2. **Service method** (existing domain service or a new one)
   Use `DatabaseHelper.ExecuteViewAsync<T>("vw_ProjectsSummary", mapper)`.

   ```csharp
   public async Task<IEnumerable<ProjectSummaryViewModel>> GetProjectsSummaryAsync() {
       return await _db.ExecuteViewAsync<ProjectSummaryViewModel>(
           "vw_ProjectsSummary",
           r => new ProjectSummaryViewModel {
               ProjectID = DatabaseHelper.GetSafeInt32(r,"ProjectID"),
               Name      = DatabaseHelper.GetSafeString(r,"Name"),
               CreatedAt = DatabaseHelper.GetSafeDateTime(r,"CreatedAt"),
               IsActive  = DatabaseHelper.GetSafeBool(r,"IsActive")
           }
       );
   }
   ```

3. **Controller endpoint**
   If it is an **admin report**, add to `/Controllers/ViewsController.cs` or a new controller under `/api/views/...`.
   The middleware already restricts `/api/views/*` to **ADMIN**.

   ```csharp
   [HttpGet("projects-summary")]
   public async Task<ActionResult<IEnumerable<ProjectSummaryViewModel>>> GetProjectsSummary() {
       // ADMIN enforced by middleware on /api/views/*
       var data = await _userService.GetProjectsSummaryAsync();
       return Ok(data);
   }
   ```

> **Security note:** `ExecuteViewAsync` composes a raw `SELECT`. Only use `whereClause` with **trusted values** (route ints, enums). If you need user-provided filters, **wrap the view in a stored procedure** and call it via `ExecuteStoredProcedureListAsync` with parameters.

---

## Integrating a **Stored Procedure** (SP)

We use two patterns:

1. **SP that returns a result set** (possibly with a `Message` column):

   * Use `ExecuteStoredProcedureAsync<T>` to read the first row (or `ExecuteStoredProcedureListAsync<T>` for multiple).
   * Map columns via the provided `mapper(SqlDataReader)`.

   ```csharp
   var res = await _db.ExecuteStoredProcedureAsync<string>(
       "sp_DeleteProject",
       new[] { new SqlParameter("@ProjectID", id) },
       r => r["Message"].ToString() ?? "OK"
   );
   ```

2. **SP that uses OUTPUT parameters** (like `sp_UserLogin`):

   * Use `ExecuteStoredProcedureWithOutputAsync` and read the returned dictionary.

   ```csharp
   var p = new SqlParameter[] {
       new("@Email", dto.Email),
       new("@Password", dto.Password),
       new("@Success", SqlDbType.Bit){ Direction = ParameterDirection.Output },
       new("@Message", SqlDbType.NVarChar, 500){ Direction = ParameterDirection.Output }
   };
   var (ok, err, outVals) = await _db.ExecuteStoredProcedureWithOutputAsync("sp_SomeAction", p);
   var success = (bool)(outVals["@Success"] ?? false);
   var message = outVals["@Message"]?.ToString() ?? (ok ? "Success" : err);
   ```

**Where to put things**

* **New SP “feature”** for a domain = add methods on the corresponding `*Service`, declare them on the matching interface, and expose endpoints on an existing controller (e.g., `AdminController`, `UserController`) or a new one.
* **Utility/maintenance SPs** (like token cleanup) belong in `AdminService` + `AdminController`.

---

## Modifying an existing table (columns & contracts)

When changing a table or SP signature (e.g., add `Users.Phone`):

1. **Database**: apply DDL and adjust SPs/views.
2. **API updates**:

   * **DTOs**: add fields to `Create*DTO`, `Update*DTO`, and `*ResponseDTO` as needed (`/Models/DTOs`).
   * **Services**: pass the new parameters (use `DBNull.Value` for optional input), and **map** any new output columns.
   * **ViewModels**: if views expose the new column, add it to the corresponding `ViewModel`.
   * **Controllers**: the action signatures remain the same; model binding will include the new property automatically.
   * **Validation**: add `[Required]`, `[StringLength]`, etc. if the DB enforces it.
   * **Security**: if the change affects **who** can call (**role**), update `HasRequiredRole` and/or routes.

> **Breaking changes**: If an SP starts requiring a field that used to be optional, update API validation and document in the PR body as **BREAKING CHANGE**.

---

## Removing a table / feature

1. **Database**: drop SPs, views, and the table (or mark as deprecated first).
2. **API**:

   * Delete or deprecate **controller endpoints** that expose it.
   * Remove **service methods** + **interface** signatures.
   * Remove related **DTOs**, **ViewModels**, and **Entity**.
   * Update **middleware** if route prefixes are no longer used.
   * Clean Swagger examples or docs referencing it.

---

## Security workflow (very important)

* **Authentication** (global): `AuthenticationMiddleware` enforces:

  * public endpoints (login, selected POST registrations, and GET locations) are whitelisted in `ShouldSkipAuthentication`.
  * for everything else, require `Authorization: Bearer {GUID}` header.
  * resolves `UserId`, `UserType`, `SessionToken` into `HttpContext.Items`.
* **Authorization** (role-based):

  * **ADMIN-only**: `/api/admin/*`, `/api/auth/reset-password`, `/api/views/*`
  * **Any authenticated user**: logout, change-password, `/api/user/*`, `/api/location/*` (non-public verbs)
  * When you add a new admin/report route, prefer a path that starts with `/api/admin/...` or `/api/views/...` so **no extra code** is needed.
* **Controller double-checks**: Controllers still check `HttpContext.Items["UserType"]` for ADMIN to fail fast and return proper HTTP codes (403 vs 401).
* **Self-protection**: Example exists to **prevent admin self-deletion**.

> **Input safety**: Our `ExecuteViewAsync` builds raw `SELECT` text. Continue to use it **only** with trusted route values (ints, enums). For free-form filters or user-provided strings, **wrap the view in an SP** and call it with parameters.

---

## Patterns & snippets

### Passing optional parameters to SPs

```csharp
new("@SecondSurname", (object?)dto.SecondSurname ?? DBNull.Value)
```

### Reading safe values

```csharp
DatabaseHelper.GetSafeInt32(reader, "CantonID");
DatabaseHelper.GetSafeString(reader, "ProvinceName");
DatabaseHelper.GetSafeNullableString(reader, "DistrictName");
DatabaseHelper.GetSafeDateTime(reader, "CreatedAt");
DatabaseHelper.GetSafeBool(reader, "IsActive");
DatabaseHelper.GetSafeIntToBool(reader, "HasActiveSession");
```

### Creating a new domain module (end-to-end checklist)

* [ ] DB: Table + SPs (CRUD) (+ views if needed)
* [ ] `/Models/DTOs/<Domain>DTOs.cs`
* [ ] `/Models/Entities/<Domain>.cs`
* [ ] `/Models/ViewModels/<Domain>*.cs` (for views only)
* [ ] `/Services/Interfaces/I<Domain>Service.cs`
* [ ] `/Services/Implementations/<Domain>Service.cs`
* [ ] `/Controllers/<Domain>Controller.cs` (or extend `AdminController` if admin-only)
* [ ] DI: `Program.cs` → `AddScoped<I<Domain>Service, <Domain>Service>()`
* [ ] Security: adjust `AuthenticationMiddleware` public/admin lists if needed
* [ ] Swagger sanity check

---

## How we expose **existing DB objects** already in the repo (examples)

* **Login / Logout / Password** → `AuthController` → `AuthService` → SPs:

  * `sp_UserLogin` (OUTPUT params)
  * `sp_UserLogout`, `sp_ChangePassword`, `sp_ResetPasswordByAdmin`
* **Admin utilities** → `AdminController` → `AdminService` → SPs:

  * `sp_CleanExpiredTokens`, `sp_SyncIsActiveWithTokens`
* **Users (Create/Update/Delete/Get)** → `UserController`/`AdminController` → `UserService`/`AdminService` → SPs:

  * `sp_CreateClient`, `sp_CreateEngineer`, `sp_CreateAdministrator`
  * `sp_UpdateClient`, `sp_UpdateEngineer`, `sp_UpdateAdministrator`
  * `sp_DeleteClient`, `sp_DeleteEngineer`, `sp_DeleteAdministrator`
* **Locations** → `LocationController` → `LocationService` → SPs/Views:

  * `sp_GetProvinces`, `sp_GetCantonsByProvince`, `sp_GetDistrictsByCanton`
  * `sp_AddProvince`, `sp_AddCanton`, `sp_AddDistrict`
* **Views (reports)** → `ViewsController` → `UserService` → **views**:

  * `vw_ActiveClients`, `vw_ActiveEngineers`, `vw_ActiveAdministrators`
  * `vw_AllClients`, `vw_AllEngineers`, `vw_AllAdministrators`

---

## Configuration & environment

* **Connection string**: `appsettings.json > ConnectionStrings:DefaultConnection`
* **Swagger security**: preconfigured with **Bearer** header.
* **CORS**: `"AllowAllOrigins"` policy (tune for production).
* **HTTPS**: enabled by default.
* **DI registrations**: in `Program.cs` (add your new service here).

---

## Troubleshooting

* **401 Unauthorized**

  * Missing or malformed `Authorization` header; token is not a GUID; token expired/invalidated.
* **403 Forbidden**

  * Route is **ADMIN-only** and requester’s `UserType` isn’t `ADMIN`.
* **500 errors**

  * SP raised an error → check message returned by `ExecuteStoredProcedureWithOutputAsync` / SQL logs.
* **Empty results from views**

  * View columns don’t match **ViewModel** mapping names; or the WHERE clause filters out everything; or `HasActiveSession` typed mismatch (use `GetSafeIntToBool`).

---

## Final notes

* Always keep **SP contracts** (names & parameters) and **DTO/ViewModel** shapes in sync with DB.
* Favor **SPs** for any operation that needs filtering, validation, or multi-table logic.
* Keep **controllers thin**, **services explicit**, and **middleware authoritative** for security.
* Stick to these directories and naming conventions so the whole team finds things instantly.