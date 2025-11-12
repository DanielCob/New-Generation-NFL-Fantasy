# API Integration Workflow

**Simple rule:** Every database change flows through **5 SQL files** first, then gets exposed through the .NET API layers.

```
Database (5 files) → DataAccess → Service → Controller → Client
```

---

## Project Structure (Current)

```
/Controllers/
  /Auth/           → AuthController
  /Fantasy/        → LeagueController, TeamController
  /NflDetails/     → NFLTeamController, NFLPlayerController
  /System/         → ReferenceController, SeasonController, ScoringController
  /Audit/          → AuditController
  /Views/          → ViewsController (admin reports)

/DataAccessLayer/
  /SqlDatabase/
    /Interfaces/
      IDatabaseHelper.cs
    /Implementations/
      DatabaseHelper.cs              → Core SQL execution
      /Auth/                         → AuthDataAccess, UserDataAccess, SystemRolesDataAccess
      /Fantasy/                      → LeagueDataAccess, TeamDataAccess
      /NflDetails/                   → NFLTeamDataAccess, NFLPlayerDataAccess, ScoringDataAccess
      /Audit/                        → AuditDataAccess
      ReferenceDataAccess.cs
      SeasonDataAccess.cs
  /StorageDatabase/
    /Implementations/
      MinIODataAccess.cs

/LogicLayer/
  /SqlLogic/Services/
    /Interfaces/                     → I*Service contracts
    /Implementations/
      /Auth/                         → AuthService, UserService, SystemRolesService
      /Fantasy/                      → LeagueService, TeamService
      /NflDetails/                   → NFLTeamService, NFLPlayerService, ScoringService
      /Audit/                        → AuditService
      SeasonService.cs
      ReferenceService.cs
  /StorageLogic/Services/
    /Implementations/
      StorageService.cs
  /EmailLogic/Services/
    /Implementations/
      SmtpEmailSender.cs

/Models/
  /DTOs/
    /Auth/                           → Login, Register, Password DTOs
    /Fantasy/                        → League, Team DTOs
    /NflDetails/                     → NFLTeam, NFLPlayer DTOs
  /Entities/
    /Auth/                           → UserAccount, Session
    /Fantasy/                        → League, Team, TeamRoster
    /NflDetails/                     → NFLTeam, NFLPlayer
  /ViewModels/
    /Auth/, /Fantasy/, /NflDetails/  → View projections

/SharedSystems/
  /Middleware/
    AuthenticationMiddleware.cs      → Bearer token validation
  /Validators/                       → Reusable validation logic
  /EmailConfig/, /StorageConfig/     → Configuration classes

/Helpers/
  /Extensions/                       → ControllerBase extensions (UserId, ClientIp, etc.)
  /Filters/                          → ModelStateValidation, GlobalException, AuthLogging

Program.cs                           → DI container, policies, middleware pipeline
appsettings.json                     → Connection strings, SMTP, MinIO config
```

---

## Database-First Workflow (5 SQL Files)

All schema changes happen in these files **in order**:

1. **`01CreateTablesDB.sql`** — Tables, schemas, roles, constraints, indexes
2. **`02CreateFunctionsDB.sql`** — Helper functions (optional, used by SPs)
3. **`03CreateSPsDB.sql`** — Business logic stored procedures (`app.*`)
4. **`04CreateViewsDB.sql`** — Read-only views (`dbo.vw_*`)
5. **`05PopulationScriptDB.sql`** — Seed/demo data (idempotent)

**Execution order on fresh DB:**
```
01 → 02 → 03 → 04 → 05
```

---

## Adding a New Feature (End-to-End)

### Example: Add "NFLPlayer Management"

#### 1. Database (SQL Files)

**01CreateTablesDB.sql:**
```sql
IF OBJECT_ID('ref.NFLPlayer','U') IS NULL
CREATE TABLE ref.NFLPlayer (
  NFLPlayerID INT IDENTITY(1,1),
  FirstName NVARCHAR(50) NOT NULL,
  LastName NVARCHAR(50) NOT NULL,
  -- FullName computed column
  Position NVARCHAR(20) NOT NULL,
  NFLTeamID INT NOT NULL,
  -- image fields (PhotoUrl, PhotoWidth, PhotoHeight, PhotoBytes, etc.)
  IsActive BIT DEFAULT 1,
  CreatedByUserID INT,
  UpdatedByUserID INT,
  CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_NFLPlayer PRIMARY KEY(NFLPlayerID),
  CONSTRAINT FK_NFLPlayer_NFLTeam FOREIGN KEY(NFLTeamID) REFERENCES ref.NFLTeam(NFLTeamID)
);
GO
```

**03CreateSPsDB.sql:**
```sql
CREATE OR ALTER PROCEDURE app.sp_CreateNFLPlayer
  @ActorUserID INT,
  @FirstName NVARCHAR(50),
  @LastName NVARCHAR(50),
  @Position NVARCHAR(20),
  @NFLTeamID INT,
  -- optional image fields
  @SourceIp NVARCHAR(45) = NULL,
  @UserAgent NVARCHAR(300) = NULL
AS
BEGIN
  SET NOCOUNT ON;
  BEGIN TRY
    -- Validation
    IF @FirstName IS NULL OR @LastName IS NULL
      THROW 50001, 'Name required', 1;

    BEGIN TRAN;
      INSERT INTO ref.NFLPlayer(FirstName, LastName, Position, NFLTeamID, CreatedByUserID)
      VALUES(@FirstName, @LastName, @Position, @NFLTeamID, @ActorUserID);

      DECLARE @NewID INT = SCOPE_IDENTITY();

      -- Audit
      INSERT INTO audit.UserActionLog(ActorUserID, EntityType, EntityID, ActionCode, SourceIp, UserAgent)
      VALUES(@ActorUserID, N'NFL_PLAYER', CAST(@NewID AS NVARCHAR(50)), N'CREATE', @SourceIp, @UserAgent);
    COMMIT;

    SELECT @NewID AS NFLPlayerID, N'Player created' AS Message;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
  END CATCH
END
GO

GRANT EXECUTE ON OBJECT::app.sp_CreateNFLPlayer TO app_executor;
GO
```

**04CreateViewsDB.sql:**
```sql
CREATE OR ALTER VIEW dbo.vw_NFLPlayers
AS
SELECT
  p.NFLPlayerID,
  p.FirstName,
  p.LastName,
  p.FullName,
  p.Position,
  p.NFLTeamID,
  nt.TeamName AS NFLTeamName,
  p.IsActive,
  p.CreatedAt
FROM ref.NFLPlayer p
JOIN ref.NFLTeam nt ON nt.NFLTeamID = p.NFLTeamID;
GO

GRANT SELECT ON dbo.vw_NFLPlayers TO app_executor;
GO
```

#### 2. API Integration

**a) DTOs** (`/Models/DTOs/NflDetails/NFLPlayerDTOs.cs`):
```csharp
public class CreateNFLPlayerDTO
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public string Position { get; set; } = string.Empty;
    
    [Required]
    public int NFLTeamID { get; set; }
    
    // Optional image fields
    public string? PhotoUrl { get; set; }
    public short? PhotoWidth { get; set; }
    // ...
}

public class NFLPlayerResponseDTO
{
    public int NFLPlayerID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

**b) DataAccess** (`/DataAccessLayer/.../NFLPlayerDataAccess.cs`):
```csharp
public class NFLPlayerDataAccess
{
    private readonly IDatabaseHelper _db;
    
    public NFLPlayerDataAccess(IDatabaseHelper db) { _db = db; }
    
    public async Task<NFLPlayerResponseDTO?> CreateAsync(
        CreateNFLPlayerDTO dto,
        int actorUserId,
        string? sourceIp,
        string? userAgent)
    {
        var parameters = new SqlParameter[]
        {
            SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
            SqlParameterExtensions.CreateParameter("@FirstName", dto.FirstName),
            SqlParameterExtensions.CreateParameter("@LastName", dto.LastName),
            SqlParameterExtensions.CreateParameter("@Position", dto.Position),
            SqlParameterExtensions.CreateParameter("@NFLTeamID", dto.NFLTeamID),
            SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
            SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
        };
        
        return await _db.ExecuteStoredProcedureAsync(
            "app.sp_CreateNFLPlayer",
            parameters,
            reader => new NFLPlayerResponseDTO
            {
                NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                Message = reader.GetSafeString("Message")
            }
        );
    }
}
```

**c) Service Interface** (`/LogicLayer/.../INFLPlayerService.cs`):
```csharp
public interface INFLPlayerService
{
    Task<ApiResponseDTO> CreateAsync(CreateNFLPlayerDTO dto, int actorUserId, string? sourceIp, string? userAgent);
    Task<List<NFLPlayerListDTO>> ListAsync(string? position = null);
}
```

**d) Service Implementation** (`/LogicLayer/.../NFLPlayerService.cs`):
```csharp
public class NFLPlayerService : INFLPlayerService
{
    private readonly NFLPlayerDataAccess _dataAccess;
    private readonly ILogger<NFLPlayerService> _logger;
    
    public NFLPlayerService(NFLPlayerDataAccess dataAccess, ILogger<NFLPlayerService> logger)
    {
        _dataAccess = dataAccess;
        _logger = logger;
    }
    
    public async Task<ApiResponseDTO> CreateAsync(
        CreateNFLPlayerDTO dto,
        int actorUserId,
        string? sourceIp,
        string? userAgent)
    {
        try
        {
            // Business validation (if needed)
            
            var result = await _dataAccess.CreateAsync(dto, actorUserId, sourceIp, userAgent);
            
            if (result != null)
            {
                _logger.LogInformation("Player created: {Name}", $"{dto.FirstName} {dto.LastName}");
                return ApiResponseDTO.SuccessResponse(result.Message, result);
            }
            
            return ApiResponseDTO.ErrorResponse("Failed to create player");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error creating player");
            return ApiResponseDTO.ErrorResponse(ex.Message);
        }
    }
}
```

**e) Controller** (`/Controllers/NflDetails/NFLPlayerController.cs`):
```csharp
[ApiController]
[Route("api/nflplayer")]
[Authorize]
public class NFLPlayerController : ControllerBase
{
    private readonly INFLPlayerService _service;
    private readonly ILogger<NFLPlayerController> _logger;
    
    public NFLPlayerController(INFLPlayerService service, ILogger<NFLPlayerController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponseDTO>> Create([FromBody] CreateNFLPlayerDTO dto)
    {
        var userId = this.UserId();
        var sourceIp = this.ClientIp();
        var userAgent = this.UserAgent();
        
        var result = await _service.CreateAsync(dto, userId, sourceIp, userAgent);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetById), new { id = ((NFLPlayerResponseDTO?)result.Data)?.NFLPlayerID }, result);
        }
        
        return BadRequest(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<NFLPlayerDetailsDTO>> GetById(int id)
    {
        // Implementation
    }
}
```

**f) Register in Program.cs:**
```csharp
// DataAccess
builder.Services.AddScoped<NFLPlayerDataAccess>();

// Service
builder.Services.AddScoped<INFLPlayerService, NFLPlayerService>();
```

---

## Layers & Responsibilities

| Layer | Responsibility | Example |
|-------|---------------|---------|
| **Controller** | HTTP routing, validation, status codes | `NFLPlayerController.Create()` |
| **Service** | Business logic, orchestration, logging | `NFLPlayerService.CreateAsync()` |
| **DataAccess** | SQL parameter building, SP execution, mapping | `NFLPlayerDataAccess.CreateAsync()` |
| **DatabaseHelper** | Raw SQL execution (SPs, views, queries) | `ExecuteStoredProcedureAsync()` |

**Rules:**
- Controllers are **thin** — validate, call service, return HTTP status
- Services contain **business logic** — validation, orchestration, error handling
- DataAccess builds **SQL calls** — parameters, execution, row mapping
- **Never** write SQL outside DatabaseHelper or DataAccess

---

## Security Model

### Authentication
**Middleware:** `AuthenticationMiddleware.cs`
- Validates `Bearer {GUID}` token from `Authorization` header
- Resolves session → adds `UserId`, `UserRole` to `HttpContext.Items`
- Public endpoints (login, register) skip auth via whitelist

### Authorization
**Policies** (defined in Program.cs):
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("ADMIN"));
    options.AddPolicy("BrandOrAdmin", p => p.RequireRole("ADMIN", "BRAND_MANAGER"));
});
```

**Usage:**
```csharp
[Authorize(Policy = "AdminOnly")]  // ADMIN only
public async Task<ActionResult> AdminEndpoint() { }

[Authorize]  // Any authenticated user
public async Task<ActionResult> UserEndpoint() { }
```

### Role Hierarchy
- **ADMIN** — Full system access (users, teams, players, seasons)
- **BRAND_MANAGER** — Brand assets only (team images, etc.)
- **USER** — Regular user (leagues, teams they own)

---

## Common Patterns

### Calling a Stored Procedure
```csharp
var parameters = new SqlParameter[]
{
    SqlParameterExtensions.CreateParameter("@Param1", value1),
    SqlParameterExtensions.CreateParameter("@Param2", value2 ?? (object)DBNull.Value)
};

var result = await _db.ExecuteStoredProcedureAsync(
    "app.sp_Something",
    parameters,
    reader => new ResultDTO
    {
        ID = reader.GetSafeInt32("ID"),
        Name = reader.GetSafeString("Name")
    }
);
```

### Reading a View
```csharp
var results = await _db.ExecuteViewAsync(
    "vw_SomethingList",
    reader => new SomethingVM
    {
        ID = reader.GetSafeInt32("ID"),
        Name = reader.GetSafeString("Name")
    },
    whereClause: "IsActive = 1",
    orderBy: "Name"
);
```

### Controller Helpers (Extension Methods)
```csharp
var userId = this.UserId();           // from HttpContext.Items["UserId"]
var userRole = this.UserRole();       // from HttpContext.Items["SystemRoleCode"]
var sourceIp = this.ClientIp();       // from connection
var userAgent = this.UserAgent();    // from headers
```

### Service Error Handling
```csharp
try
{
    // Call DataAccess
    var result = await _dataAccess.DoSomething();
    
    if (result != null)
    {
        _logger.LogInformation("Success");
        return ApiResponseDTO.SuccessResponse("Done", result);
    }
    
    return ApiResponseDTO.ErrorResponse("Failed");
}
catch (SqlException ex)
{
    _logger.LogError(ex, "SQL error");
    return ApiResponseDTO.ErrorResponse(ex.Message);
}
```

---

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| DTO | `{Action}{Entity}DTO` | `CreateNFLPlayerDTO` |
| Response DTO | `{Entity}ResponseDTO` | `NFLPlayerResponseDTO` |
| List DTO | `{Entity}ListItemDTO` | `NFLPlayerListItemDTO` |
| ViewModel | `{Entity}VM` | `NFLPlayerVM` |
| DataAccess | `{Entity}DataAccess` | `NFLPlayerDataAccess` |
| Service | `{Entity}Service` | `NFLPlayerService` |
| Interface | `I{Entity}Service` | `INFLPlayerService` |
| Controller | `{Entity}Controller` | `NFLPlayerController` |

---

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=XNFLFantasyDB;..."
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "...",
    "Password": "...",
    "FromEmail": "...",
    "FromName": "X-NFL Fantasy"
  },
  "MinIO": {
    "Endpoint": "127.0.0.1:9000",
    "AccessKey": "...",
    "SecretKey": "...",
    "BucketName": "nfl-fantasy-images",
    "UseSSL": false
  }
}
```

---

## Checklist: Adding a New Feature

- [ ] **Database:**
  - [ ] Table in 01 (with constraints, indexes)
  - [ ] Functions in 02 (if needed)
  - [ ] SPs in 03 (CRUD operations)
  - [ ] Views in 04 (read projections)
  - [ ] Seed data in 05 (if applicable)

- [ ] **API:**
  - [ ] DTOs in `/Models/DTOs/{Domain}/`
  - [ ] Entity in `/Models/Entities/{Domain}/`
  - [ ] ViewModels in `/Models/ViewModels/{Domain}/` (if using views)
  - [ ] DataAccess in `/DataAccessLayer/.../Implementations/{Domain}/`
  - [ ] Service Interface in `/LogicLayer/.../Interfaces/{Domain}/`
  - [ ] Service Implementation in `/LogicLayer/.../Implementations/{Domain}/`
  - [ ] Controller in `/Controllers/{Domain}/`
  - [ ] Register DataAccess + Service in `Program.cs`

- [ ] **Security:**
  - [ ] Apply `[Authorize]` or `[Authorize(Policy = "...")]`
  - [ ] Test with admin and regular user tokens

- [ ] **Testing:**
  - [ ] Smoke test all endpoints via Swagger
  - [ ] Verify audit logs are created

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `DatabaseHelper.cs` | Core SQL execution engine |
| `AuthenticationMiddleware.cs` | Bearer token validation |
| `SqlParameterExtensions.cs` | Helper for creating SQL parameters |
| `ControllerBaseExtensions.cs` | UserId(), ClientIp(), UserAgent() helpers |
| `ModelStateValidationFilter.cs` | Auto-validate ModelState |
| `GlobalExceptionFilter.cs` | Catch unhandled exceptions |
| `Program.cs` | DI container, policies, middleware |

---

## Troubleshooting

**401 Unauthorized:**
- Missing/invalid Bearer token
- Session expired or invalidated

**403 Forbidden:**
- Endpoint requires ADMIN role
- User role doesn't match policy

**500 Internal Server Error:**
- SP threw exception (check SQL logs)
- Mapping error (column name mismatch)

**Empty results from view:**
- Column names don't match ViewModel properties
- WHERE clause filters everything out

---

**That's it.** Follow the 5 SQL files → DataAccess → Service → Controller pattern. Keep layers separated, validate inputs, audit significant actions, and enforce security policies.