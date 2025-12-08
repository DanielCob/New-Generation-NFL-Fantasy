# Frontend Workflow (Angular) — WEB_WORKFLOW.md

```ts
$env:CHROME_BIN="C:\Users\adriel\AppData\Local\BraveSoftware\Brave-Browser\Application\brave.exe"
ng test
ng test --no-watch --code-coverage
ng test --include='**/nfl-player-service.spec.ts'
ng test --include='**/nfl-player-service.spec.ts' --no-watch --code-coverage
ng test --include='**/*dialog.spec.ts' --no-watch --code-coverage
```

> **Goal**
> Keep a clear and consistent **API ⇄ Frontend mapping** and a repeatable way to **add/extend features** (leagues, teams, NFL players, news, etc.) without breaking security, routes, or UI patterns.
> This document describes **where things live**, **how we structure models/services/components**, and **how to safely plug new functionality into the existing app**.

---

## 1. Tech Baseline

* **Angular** 17+ with:

  * **Standalone components** (no NgModules).
  * **Signals** (`signal`, `computed`, `toSignal`).
  * **Functional HTTP interceptors & guards** (`HttpInterceptorFn`, `authGuard`, `adminGuard`, etc.).
* **Angular Material** (M3 theme via `custom-theme.scss`).
* **RxJS** for HTTP and async flows.
* **Environment-based config**: `environment.ts` / `environment.prod.ts`.
* **Backend**:

  * Token-based auth (SessionID as Bearer).
  * Mixed casing in responses (`success`/`Success`, `data`/`Data`).

We intentionally **mirror backend casing** in our TS models (e.g. `NFLPlayerID`, `FirstName`) to avoid surprises.

---

## 2. Project Structure (Overview)

```txt
src/
├─ main.ts                        → bootstrap (standalone)
├─ index.html                     → root
├─ styles.css                     → global styles (snackbars, animations, layout tweaks)
├─ custom-theme.scss              → Angular Material theme (M3)
├─ environments/
│  ├─ environment.ts              → dev config (apiUrl, feature flags, image limits, ...)
│  └─ environment.prod.ts         → prod config
└─ app/
   ├─ app.config.ts               → router/http/animations/providers
   ├─ app.routes.ts               → root routes (lazy), guards & shells
   ├─ app.ts / app.component.ts   → app root (router-outlet)
   ├─ core/
   │  ├─ guards/
   │  │  ├─ auth-guard.ts         → requires authenticated session
   │  │  ├─ admin-guard.ts        → requires admin capabilities
   │  │  ├─ no-auth-guard.ts      → blocks /login & /register if logged in
   │  │  ├─ team-owner-guard.ts   → only team owners can modify some routes
   │  │  └─ redirect-stored-teams-guard.ts → uses stored team to redirect
   │  ├─ interceptors/
   │  │  ├─ auth-interceptor.ts   → attaches Bearer token, handles 401
   │  │  └─ error-interceptor.ts  → (optional) global HTTP error handling
   │  ├─ models/
   │  │  ├─ auth-model.ts
   │  │  ├─ user-model.ts
   │  │  ├─ league-model.ts
   │  │  ├─ nfl-team-model.ts
   │  │  ├─ nfl-player-model.ts   → DTOs, list/detail, batch & news
   │  │  └─ common-model.ts       → ApiResponse, common view models
   │  ├─ services/
   │  │  ├─ auth-service.ts       → session, login/logout, password reset, profile header
   │  │  ├─ user-service.ts       → full profile, user teams/leagues, etc.
   │  │  ├─ league-service.ts     → create/edit leagues, etc.
   │  │  ├─ nfl-team-service.ts   → CRUD/pagination for NFL teams
   │  │  ├─ nfl-player-service.ts → CRUD, batch, news, reports
   │  │  ├─ context/
   │  │  │  └─ league-context.service.ts → current league/team context
   │  │  └─ authz/
   │  │     ├─ authz.service.ts   → computed admin capabilities
   │  │     └─ role.service.ts    → role helpers (isAdminRole, mapping, etc.)
   │  └─ route-shells/
   │     └─ empty-redirect-shell.ts → shell used by redirectStoredTeamGuard
   ├─ layouts/
   │  └─ main-layout/
   │     ├─ main-layout.ts        → main shell (navbar + <router-outlet>)
   │     └─ main-layout.html/.css
   ├─ pages/
   │  ├─ auth/
   │  │  ├─ login/
   │  │  ├─ register/
   │  │  ├─ request-reset/
   │  │  └─ reset/
   │  ├─ profile/
   │  │  ├─ profile-header/
   │  │  ├─ sessions/
   │  │  └─ full-profile/
   │  ├─ league/
   │  │  ├─ create/
   │  │  ├─ edit-config/
   │  │  ├─ summary/
   │  │  ├─ members/
   │  │  ├─ teams/
   │  │  └─ league-actions/
   │  ├─ teams/
   │  │  ├─ my-team/
   │  │  ├─ edit-branding/
   │  │  └─ manage-roster/
   │  ├─ nfl-teams/
   │  │  ├─ list/
   │  │  ├─ create/
   │  │  └─ edit/
   │  ├─ players/
   │  │  └─ browser/
   │  ├─ seasons/
   │  │  └─ admin/
   │  ├─ admin/
   │  │  ├─ admin-nfl-player-actions/
   │  │  ├─ nfl-player-list/
   │  │  ├─ nfl-player-create/
   │  │  ├─ nfl-player-edit/
   │  │  ├─ nfl-player-batch-upload/
   │  │  ├─ batch-reports/
   │  │  └─ nfl-player-news/
   │  └─ _facades/
   │     └─ navbar.facade.ts      → aggregates auth/user/leagues for navbar
   └─ shared/
      ├─ components/
      │  ├─ navigation-bar/
      │  └─ table-simple/          → generic table w/ columns definition
      └─ pipes/
         └─ position-icon.pipe.ts  → maps NFL positions to Material icons
```

**Key rules**

* **Standalone everywhere**: components, pipes, dialogs and route-shells all declare `standalone: true`.
* **Lazy-load components** in routes using `loadComponent: () => import(...).then(m => m.XxxComponent)`.
* **Strongly typed models** that mirror backend JSON (including PascalCase property names).
* **Services = one domain** (Auth, User, League, NFLPlayer, NFLTeam, etc.) using `HttpClient` and `environment.apiUrl`.
* **State** is handled with:

  * `BehaviorSubject` (session) in `AuthService`.
  * `signal` / `computed` in components & facades.
  * Context services for cross-page state (e.g. current league/team).
* **Auth** is applied via `authInterceptor`, not by services.

---

## 3. Routing & Guards

### 3.1. Root routes (`app.routes.ts`)

Public routes:

```ts
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  // ------- PUBLIC AUTH -------
  {
    path: 'login',
    loadComponent: () => import('./pages/auth/login/login').then(m => m.Login),
    canActivate: [noAuthGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/auth/register/register').then(m => m.Register),
    canActivate: [noAuthGuard]
  },
  {
    path: 'request-reset',
    loadComponent: () => import('./pages/auth/request-reset/request-reset').then(m => m.RequestReset)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./pages/auth/reset/reset').then(m => m.Reset)
  },

  // ------- PUBLIC REFERENCE CATALOGS -------
  {
    path: 'reference/position-formats',
    loadComponent: () => import('./pages/reference/position-formats/position-formats')
      .then(m => m.PositionFormats)
  },
  {
    path: 'reference/scoring-schemas',
    loadComponent: () => import('./pages/reference/scoring-schemas/scoring-schemas')
      .then(m => m.ScoringSchemas)
  },

  // ------- PROTECTED AREA (MainLayout) -------
  {
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then(m => m.MainLayout),
    canActivate: [authGuard],
    children: [
      // profile, leagues, teams, admin, etc. (see below)
    ]
  },

  { path: '**', redirectTo: 'login' }
];
```

**Protected section**: every route under `MainLayout`:

* Requires **authGuard**.
* May add **adminGuard**, **teamOwnerGuard**, or **redirectStoredTeamGuard**.
* Uses **feature-specific** lazy-loaded components (e.g. `league/create`, `teams/:id/manage-roster`, `admin/nfl-player-news`, etc.).

#### 3.2. Examples of protected routes

```ts
// Profile
{
  path: 'profile/header',
  loadComponent: () => import('./pages/profile/profile-header/profile-header')
    .then(m => m.ProfileHeader)
},
{
  path: 'profile/sessions',
  loadComponent: () => import('./pages/profile/sessions/sessions')
    .then(m => m.Sessions)
},
{
  path: 'profile/full-profile',
  loadComponent: () => import('./pages/profile/full-profile/full-profile')
    .then(m => m.FullProfile)
},

// League actions using public ID
{
  path: 'league/:id/actions',
  loadComponent: () => import('./pages/league/league-actions/league-actions')
    .then(m => m.LeagueActionsComponent)
},

// NFL Teams (CRUD, admin-like)
{
  path: 'nfl-teams',
  children: [
    {
      path: '',
      loadComponent: () => import('./pages/nfl-teams/list/list').then(m => m.NflTeamsListComponent)
    },
    {
      path: 'create',
      loadComponent: () => import('./pages/nfl-teams/create/create').then(m => m.CreateNFLTeamComponent)
    },
    {
      path: ':id',
      loadComponent: () => import('./pages/nfl-teams/details/details').then(m => m.Details)
    },
    {
      path: ':id/edit',
      loadComponent: () => import('./pages/nfl-teams/edit/edit').then(m => m.EditNFLTeamComponent)
    }
  ]
},

// Admin area
{
  path: 'seasons/admin',
  canActivate: [adminGuard],
  loadComponent: () => import('./pages/seasons/admin/admin').then(m => m.SeasonsAdminComponent)
},
{
  path: 'admin/nfl-player-news',
  canActivate: [adminGuard],
  loadComponent: () =>
    import('./pages/admin/nfl-player-news/nfl-player-news').then(m => m.NflPlayerNews)
},
// ... other admin/nfl-player-* routes
```

### 3.3. Special guards & shells

* `noAuthGuard`
  Prevents logged-in users from entering `/login` and `/register`.

* `teamOwnerGuard`
  Used on routes that must only be accessible by the owner of a team (edit branding, manage roster, etc.).

* `redirectStoredTeamGuard` with `EmptyRedirectShell`
  Used for user-friendly “shortcuts” like `/my-team`:

  ```ts
  {
    path: 'my-team',
    canActivate: [redirectStoredTeamGuard],
    data: { dest: 'my-team' },
    loadComponent: () =>
      import('./core/route-shells/empty-redirect-shell').then(m => m.EmptyRedirectShell),
  }
  ```

  The guard reads the **stored team** from context or storage and redirects to the proper routed version:
  `/teams/:id/my-team`, `/teams/:id/edit-branding`, etc.

* `adminGuard`
  Uses **AuthzService / RoleService** to check if the current session has admin capabilities.

---

## 4. Authentication, Authorization & Session

### 4.1. AuthService: single source of truth

`core/services/auth-service.ts` holds the **Session + Auth API**.

```ts
export interface AuthSession {
  SessionID: string;
  Message: string;
  UserID: number;
  Email: string;
  Name: string;
  SystemRoleCode?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authUrl = `${environment.apiUrl}/Auth`;
  private readonly userUrl = `${environment.apiUrl}/User`;
  private readonly SESSION_KEY = 'xnf.session';

  private _session$ = new BehaviorSubject<AuthSession | null>(this.readSession());
  readonly session$ = this._session$.asObservable();

  constructor(private http: HttpClient) {}

  register(body: RegisterRequest): Observable<ApiResponse<string>> {
    return this.http.post(`${this.authUrl}/register`, body)
      .pipe(map(r => normalizeApi<string>(r)));
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.authUrl}/login`, body).pipe(
      tap(res => {
        if (res.Success && res.Data?.SessionID) {
          const s = res.Data as unknown as AuthSession;
          this.persistSession(s);
          this._session$.next(s);
        }
      })
    );
  }

  logout(): Observable<ApiResponse<string>> {
    return this.http.post(`${this.authUrl}/logout`, {}).pipe(
      map(r => normalizeApi<string>(r)),
      finalize(() => this.clearSession())
    );
  }

  logoutAll(): Observable<ApiResponse<string>> {
    return this.http.post(`${this.authUrl}/logout-all`, {}).pipe(
      map(r => normalizeApi<string>(r)),
      finalize(() => this.clearSession())
    );
  }

  getProfile(): Observable<UserProfile> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.userUrl}/header`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserProfile));
  }

  // session helpers...
}
```

**Key points**

* Session is stored as JSON in `localStorage` under `xnf.session`.
* `session$` is a `BehaviorSubject`, so components/facades can subscribe or convert to signals (`toSignal`).
* `normalizeApi<T>` standardizes backend responses (`success`/`Success`, `data`/`Data`) into a single `ApiResponse<T>`.

### 4.2. AuthzService & roles

`core/services/authz/authz.service.ts` centralizes **authorization decisions** (especially admin):

```ts
@Injectable({ providedIn: 'root' })
export class AuthzService {
  private auth  = inject(AuthService);
  private user  = inject(UserService);
  private roles = inject(RoleService);

  private reload$ = new BehaviorSubject<void>(undefined);

  private header$ = this.reload$.pipe(
    switchMap(() => defer(() => this.user.getHeader()).pipe(catchError(() => of(null)))),
    shareReplay(1)
  );

  readonly isAdmin$ = this.header$.pipe(
    map(h => {
      if (environment.enableAdmin === true) return true;
      const p = (h?.data ?? h?.Data ?? h) ?? {};
      const role = this.roles.fromHeader(p);
      return this.roles.isAdminRole(role, p);
    })
  );

  refresh() { this.reload$.next(); }
}
```

* `environment.enableAdmin` is an **override switch**; when `true` everyone is effectively admin (use only in dev).
* `RoleService` encapsulates role mapping (`SystemRoleCode` → internal role) and logic such as `isAdminRole(...)`.

### 4.3. Guards and redirects on 401

The **auth-interceptor** is responsible for:

* Adding the **Bearer** token.
* Handling **401** responses.

```ts
// core/interceptors/auth-interceptor.ts
function isPublicRequest(url: string): boolean {
  const whitelist = [
    '/api/Auth/login',
    '/api/Auth/register',
    '/api/Auth/request-reset',
    '/api/Auth/reset-with-token',
    '/api/Reference/',   // reference catalogs
    '/api/Scoring/'      // scoring schemas
  ];
  return whitelist.some(p => url.includes(p));
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const auth = inject(AuthService);
  const router = inject(Router);

  let request = req;

  if (req.method !== 'OPTIONS' && !isPublicRequest(req.url)) {
    const token = auth.session?.SessionID;
    if (token) {
      request = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
  }

  return next(request).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        auth.clearLocalSession();
        const returnUrl = location.pathname + location.search;
        router.navigate(['/login'], { queryParams: { returnUrl } });
      }
      return throwError(() => err);
    })
  );
};
```

**Public vs protected**

* Any endpoint in `isPublicRequest()` is considered **public** (no token).
* Everything else is **protected** and will get the `Authorization` header.
* Do **not** add protected endpoints to the whitelist.

---

## 5. Environment & Feature Flags

`environment.ts` exposes:

```ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7221/api',
  enableAdmin: false,

  // Image configuration
  maxImageSizeMB: 5,
  minImageDimension: 300,
  maxImageDimension: 1024,
  allowedImageTypes: ['image/jpeg', 'image/png'],

  // Default paging
  nflTeamsPageSize: 50,

  // Feature flags
  features: {
    teamBranding: true,        // Feature 3.1
    rosterManagement: true,    // Feature 3.1
    nflTeamsCRUD: true,        // Feature 10.1
    playerBrowser: true        // Player browser screen
  }
};
```

**Usage guidelines**

* Always use `environment.apiUrl` inside services to build URLs.
* Use `environment.features` to **gate experimental features** in components:

  * Hide navigation entries.
  * Avoid making calls for disabled features.
* Use the image config to validate uploads before sending to the backend.

---

## 6. State, Facades & Context Services

### 6.1. NavbarFacade

`pages/_facades/navbar.facade.ts` is a good example of a **facade**:

* Depends on `AuthService` and `UserService`.
* Converts `auth.session$` into a **signal**.
* Computes derived data (`isAdmin`) and manages remote state (`leagues` for which the user is commissioner).

```ts
@Injectable({ providedIn: 'root' })
export class NavbarFacade {
  private auth = inject(AuthService);
  private users = inject(UserService);

  session = toSignal(this.auth.session$, { initialValue: null });

  isAdmin = computed(() => {
    const s = this.session();
    return s?.SystemRoleCode === 'ADMIN' || s?.SystemRoleCode === 'ADMINISTRATOR';
  });

  leagues        = signal<LeagueRow[]>([]);
  leaguesLoading = signal(false);
  leaguesError   = signal<string | null>(null);

  loadMyLeagues(): void {
    if (this.leaguesLoading()) return;

    this.leaguesLoading.set(true);
    this.leaguesError.set(null);

    this.users.getProfile().subscribe({
      next: (p: any) => {
        const rows = (p?.CommissionedLeagues ?? []).map((x: any) => ({
          LeagueID: x.LeagueID,
          LeaguePublicID: x.LeaguePublicID,
          LeagueName: x.LeagueName,
          Status: x.Status
        }));
        this.leagues.set(rows);
        this.leaguesLoading.set(false);
      },
      error: () => {
        this.leagues.set([]);
        this.leaguesLoading.set(false);
        this.leaguesError.set('No se pudieron cargar tus ligas');
      }
    });
  }
}
```

**Pattern**

* Components (e.g. `NavigationBar`) remain **thin**:

  * No direct HTTP calls.
  * They consume `facade.session`, `facade.isAdmin`, `facade.leagues`, etc.
  * Trigger behavior via methods like `facade.loadMyLeagues()`.

### 6.2. LeagueContextService

* Stores the **current league** and **current team** (IDs) selected by the user.
* Used by:

  * NavigationBar (`selectLeague`, `goLeague()`).
  * Guards (`redirectStoredTeamGuard`).
  * Pages that need current league/team context by ID.

---

## 7. UI Standards

### 7.1. Angular Material & standalone components

* Every page/dialog/component is `standalone: true` and explicitly declares `imports: [...]`.
* Use **Material modules** as needed (`MatToolbarModule`, `MatCardModule`, `MatFormFieldModule`, `MatTableModule`, `MatDialogModule`, etc.).
* Global styles (snackbars, fade-in, layout) go into `styles.css`.
* Theme colors and typography are defined in `custom-theme.scss`.

### 7.2. Navigation bar (`NavigationBar`)

Key patterns in `shared/components/navigation-bar`:

* Injects:

  * `NavbarFacade` for state,
  * `LeagueContextService` for context,
  * `AuthService` for real logout,
  * `Router`, `MatSnackBar`, `MatDialog`.

* Derives UI state with **computed signals**:

  ```ts
  isLoggedIn = computed(() => !!this.session()?.SessionID);
  userName   = computed(() => this.session()?.Name ?? 'User');
  ```

* Uses typed navigation methods:

  ```ts
  navigateToNFLTeamsList() { this.router.navigate(['/nfl-teams']); }
  navigateToMyTeam()       { this.router.navigate(['/my-team']); }
  // ...
  ```

* Uses context-aware helpers like `goLeague('summary' | 'edit' | 'members' | 'teams')`.

### 7.3. Tables & reusable components

* When listing collections (leagues, teams, profile info), use `TableSimple` with typed `TableColumn<T>` definitions.
* Keep the **domain-specific mapping** in the page, not inside the table component.

### 7.4. Pipes & icons

* Example: `PositionIconPipe` maps NFL positions to Material icons:

  ```ts
  @Pipe({ name: 'positionIcon', standalone: true })
  export class PositionIconPipe implements PipeTransform {
    transform(position: string): string {
      const icons: { [key: string]: string } = {
        'QB': 'sports_football',
        'RB': 'directions_run',
        'WR': 'airline_stops',
        'TE': 'sports_kabaddi',
        'K': 'sports_soccer',
        'DEF': 'shield'
      };
      return icons[position] || 'person';
    }
  }
  ```

* When adding new domain-specific visual helpers, implement them as **standalone pipes** in `shared/pipes`.

---

## 8. Models & API Contracts

### 8.1. Naming and casing

* Interfaces live in `core/models`.
* Filenames follow `kebab-case` + `-model.ts` (e.g., `nfl-player-model.ts`).
* Properties mirror backend casing (e.g., `NFLPlayerID`, `FirstName`, `CreatedAt`).
* We rarely rename properties (no mapping layers) to keep simple 1:1 mapping.

### 8.2. Example: NFL Player model

```ts
export interface CreateNFLPlayerDTO {
  FirstName: string;
  LastName: string;
  Position: string;
  NFLTeamID: number;
  InjuryStatus?: string;
  InjuryDescription?: string;
  PhotoUrl?: string;
  // ...
}

export interface ListNFLPlayersRequest {
  PageNumber: number;
  PageSize: number;
  SearchTerm?: string;
  FilterPosition?: string;
  FilterNFLTeamID?: number;
  FilterIsActive?: boolean;
}

export interface NFLPlayerListItem {
  NFLPlayerID: number;
  FirstName: string;
  LastName: string;
  FullName: string;
  Position: string;
  NFLTeamID: number;
  InjuryStatus?: string;
  IsActive: boolean;
  PhotoUrl?: string;
  ThumbnailUrl?: string;
  CreatedAt?: string;
  UpdatedAt?: string;
}

export interface ListNFLPlayersResponse {
  Players: NFLPlayerListItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}
```

### 8.3. Domain constants

Domain-specific constants live in the same model file when tightly related. Example:

```ts
export const INJURY_DESIGNATIONS = [
  { value: 'O',  label: 'Out (O)',          description: 'No jugarán' },
  { value: 'D',  label: 'Doubtful (D)',     description: 'Muy poco probable (~25%)' },
  { value: 'Q',  label: 'Questionable (Q)', description: 'Probabilidad ~50%' },
  { value: 'P',  label: 'Probable (P)',     description: 'Casi seguro que juega' },
  { value: 'FP', label: 'Full Practice',    description: 'Participación plena' },
  { value: 'IR', label: 'Injured Reserve',  description: 'Fuera por periodo extendido' },
  { value: 'PUP',label: 'PUP',              description: 'Físicamente incapaz de jugar' },
  { value: 'SUS',label: 'Suspended (SUS)',  description: 'No elegible por sanción' }
] as const;
```

---

## 9. Services & HTTP Patterns

### 9.1. General rules

* One **service per domain** under `core/services`.
* Service methods:

  * Are typed with our **model interfaces**.
  * Use `environment.apiUrl`.
  * Accept typed request objects (DTOs, filters, etc.).
  * Return either raw `ApiResponse<T>` or the typed DTO directly, depending on backend contract.

### 9.2. Example: NFLPlayerService

```ts
@Injectable({ providedIn: 'root' })
export class NFLPlayerService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/NFLPlayer`;

  create(dto: CreateNFLPlayerDTO)
    : Observable<ApiResponse<{ nflPlayerID: number; fullName?: string }>> {
    return this.http.post<ApiResponse<{ nflPlayerID: number; fullName?: string }>>(
      `${this.baseUrl}`,
      dto
    );
  }

  createBatch(players: CreateNFLPlayerDTO[]): Observable<BatchCreatePlayersResponse> {
    return this.http.post<BatchCreatePlayersResponse>(`${this.baseUrl}/batch`, players);
  }

  list(request: ListNFLPlayersRequest)
    : Observable<ApiResponse<ListNFLPlayersResponse>> {
    let params = new HttpParams()
      .set('PageNumber', String(request.PageNumber))
      .set('PageSize', String(request.PageSize));

    if (request.SearchTerm?.trim())     params = params.set('SearchTerm', request.SearchTerm.trim());
    if (request.FilterPosition?.trim()) params = params.set('FilterPosition', request.FilterPosition.trim());
    if (typeof request.FilterNFLTeamID === 'number')
      params = params.set('FilterNFLTeamID', String(request.FilterNFLTeamID));
    if (typeof request.FilterIsActive === 'boolean')
      params = params.set('FilterIsActive', String(request.FilterIsActive));

    return this.http.get<ApiResponse<ListNFLPlayersResponse>>(`${this.baseUrl}`, { params });
  }

  getDetails(id: number): Observable<ApiResponse<NFLPlayerDetails>> {
    return this.http.get<ApiResponse<NFLPlayerDetails>>(`${this.baseUrl}/${id}`);
  }

  update(id: number, dto: UpdateNFLPlayerDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${id}`, dto);
  }

  deactivate(id: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  reactivate(id: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${id}/reactivate`, {});
  }

  // Batch reports, news, etc.
}
```

**Patterns**

* For endpoints returning `ApiResponse<T>`:

  * Services typically **return the entire ApiResponse** (and the component extracts `data/Data`).
* For endpoints that already conform to `ApiResponse<T>` but with inconsistent casing:

  * Use helper functions (`normalizeApi<T>`, `pickData`) to standardize.

---

## 10. Adding a New Feature (End-to-End)

Below is a **canonical workflow** for adding new functionality following the current methodology.
Use it for new domains or for extending existing ones (e.g. adding “Player News” to NFL players).

### Step 1 — Extend / Create Models

* Location: `src/app/core/models/<domain>-model.ts`.

Example (Player News added to `nfl-player-model.ts`):

```ts
export interface CreatePlayerNewsDTO {
  NFLPlayerID: number;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: 'O' | 'D' | 'Q' | 'P' | 'FP' | 'IR' | 'PUP' | 'SUS';
}

export interface PlayerNewsItem {
  NewsID: number;
  NFLPlayerID: number;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: string;
  CreatedByUserID: number;
  CreatedByName: string;
  CreatedAt: string;
}

export interface ListPlayerNewsRequest {
  PageNumber: number;
  PageSize?: number;
}

export interface ListPlayerNewsResponse {
  News: PlayerNewsItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}

export interface PlayerNewsDetails {
  NewsID: number;
  NFLPlayerID: number;
  PlayerFirstName: string;
  PlayerLastName: string;
  PlayerFullName: string;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: string;
  CreatedByUserID: number;
  CreatedByName: string;
  CreatedAt: string;
  IsDeleted: boolean;
}

export interface CreatePlayerNewsResponse {
  Success: boolean;
  Message: string;
  Data: {
    NewsID: number;
    Message: string;
  };
}

export interface DeletePlayerNewsResponse {
  Success: boolean;
  Message: string;
  Data: {
    Message: string;
  };
}
```

**Checklist**

* [ ] Name DTOs clearly (`CreateXxxDTO`, `UpdateXxxDTO`).
* [ ] Define List requests/responses for pagination.
* [ ] If the backend returns `Success/Message/Data`, capture them in proper interfaces.

### Step 2 — Extend / Create Service

Example (extending `NFLPlayerService`):

```ts
// ===== PLAYER NEWS =====
createPlayerNews(dto: CreatePlayerNewsDTO): Observable<CreatePlayerNewsResponse> {
  return this.http.post<CreatePlayerNewsResponse>(`${this.baseUrl}/news`, dto);
}

deletePlayerNews(newsId: number): Observable<DeletePlayerNewsResponse> {
  return this.http.delete<DeletePlayerNewsResponse>(`${this.baseUrl}/news/${newsId}`);
}

getPlayerNews(playerId: number, req: ListPlayerNewsRequest)
  : Observable<ApiResponse<ListPlayerNewsResponse>> {
  let params = new HttpParams().set('pageNumber', String(req.PageNumber));
  if (req.PageSize) params = params.set('pageSize', String(req.PageSize));

  return this.http.get<ApiResponse<ListPlayerNewsResponse>>(
    `${this.baseUrl}/${playerId}/news`,
    { params }
  );
}

getPlayerNewsDetails(newsId: number): Observable<ApiResponse<PlayerNewsDetails>> {
  return this.http.get<ApiResponse<PlayerNewsDetails>>(`${this.baseUrl}/news/${newsId}`);
}
```

**Guidelines**

* Use `HttpParams` for filters/pagination.
* Keep base path at service level (`baseUrl`).
* Console logging is okay in early phases (`console.log('📤 [NFLPlayerService] ...')`), but should be removed or behind environment flags for production.

### Step 3 — Pages & Dialogs

Create **standalone** components for screens and dialogs.

Example: `pages/admin/nfl-player-news/nfl-player-news.ts`:

* Uses:

  * Filters form (`FormBuilder`, `ReactiveFormsModule`).
  * `NFLPlayerService` to list players.
  * `NFLTeamService` to translate `NFLTeamID → TeamName`.
  * `MatDialog` to open dialogs for view/create/delete news.
  * Signals for `loading`, `rows`, `total`, `page`, `teams`.

Example: `PlayerNewsListDialog` under `pages/admin/nfl-player-news/player-news-list-dialog/`:

* Accepts `MAT_DIALOG_DATA` with `NFLPlayerListItem`.
* Uses `NFLPlayerService.getPlayerNews(...)`.
* Stores data in signals (`news`, `total`, `error`, `loading`).
* Provides view helpers (`getDesignationColor`, `getDesignationLabel`).

### Step 4 — Routes

Add routes under the correct section in `app.routes.ts`.

Example (Admin-only):

```ts
{
  path: 'admin/nfl-player-news',
  canActivate: [adminGuard],
  loadComponent: () =>
    import('./pages/admin/nfl-player-news/nfl-player-news')
      .then(m => m.NflPlayerNews)
}
```

**Important**

* Use `adminGuard` for admin screens.
* Use `authGuard` implicitly via the protected layout (all children under the `MainLayout` section).

### Step 5 — Navigation

Update the **NavigationBar** if the screen should be reachable from the navbar:

* Add methods:

  ```ts
  navigateToNFLPlayerNews() {
    this.router.navigate(['/admin/nfl-player-news']);
  }
  ```

* Wire them from template via `click` handlers or menus.

* Optionally use `environment.features` to show/hide entries.

---

## 11. Error Handling & UX

### 11.1. Snackbars

Use `MatSnackBar` for:

* Success messages (`panelClass: ['success-snackbar']`).
* Error messages (`panelClass: ['error-snackbar']`).

Example:

```ts
this.snack.open('League created (#' + id + ') – ' + name, 'OK', {
  duration: 3500
});
```

When processing validation errors from ASP.NET or similar:

```ts
const e = err?.error ?? err;
let msg = '';

if (e?.errors && typeof e.errors === 'object') {
  const errorMessages: string[] = [];
  for (const [field, messages] of Object.entries(e.errors)) {
    if (Array.isArray(messages)) errorMessages.push(...messages);
  }
  msg = errorMessages.join(' | ');
}

if (!msg) {
  msg = e?.message ?? e?.Message ?? e?.suggestedAction ?? 'Could not create league';
}

this.snack.open(msg, 'OK', { duration: 4000 });
```

### 11.2. Loading & error signals

Pages typically have:

```ts
loading = signal(true);
error   = signal<string | null>(null);
data    = signal<Something | null>(null);
```

Update them explicitly in subscribe handlers:

* `loading.set(true)` before call.
* `loading.set(false)` in both `next` and `error`.
* `error.set(msg)` in case of error.

---

## 12. Public vs Protected Endpoints (Recap)

**Public endpoints** (no token; whitelisted in `auth-interceptor`):

* `/api/Auth/login`
* `/api/Auth/register`
* `/api/Auth/request-reset`
* `/api/Auth/reset-with-token`
* `/api/Reference/*` (e.g. position formats)
* `/api/Scoring/*`  (e.g. scoring schemas)

**Protected endpoints** (require token; do *not* whitelist):

* Everything else:

  * `/api/Auth/logout`, `/api/Auth/logout-all`
  * `/api/User/*`
  * `/api/League/*`
  * `/api/NFLTeam/*`
  * `/api/NFLPlayer/*`
  * `/api/Views/*`
  * Admin endpoints (`/api/Admin/...`, `/api/Seasons/...`)

Whenever you introduce a new **public** endpoint, add its path fragment to `isPublicRequest()`.

---

## 13. Checklist for a New Domain / CRUD

Use this checklist when introducing a completely new domain (e.g. “Stadiums”, “Sponsors”) or a new sub-CRUD under an existing domain.

1. **Backend ready**

   * [ ] Endpoints defined and documented (Swagger).
   * [ ] Clear contract (`Success/Message/Data` vs direct objects).

2. **Models**

   * [ ] `core/models/<domain>-model.ts` created or extended.
   * [ ] DTOs (`CreateXxxDTO`, `UpdateXxxDTO`).
   * [ ] `List<Request>` and `List<Response>` for pagination.

3. **Service**

   * [ ] `core/services/<domain>-service.ts` created or extended.
   * [ ] Base URL uses `environment.apiUrl`.
   * [ ] Methods are fully typed and expose `ApiResponse<T>` or DTOs as appropriate.

4. **Pages & Components**

   * [ ] Standalone pages under `pages/<area>/<domain>/`.
   * [ ] Reactive forms with validators where applicable.
   * [ ] Uses signals for `loading`, `error`, `rows`, etc.
   * [ ] Uses Angular Material components consistent with the rest of the app.

5. **Dialogs / Shared components**

   * [ ] Dialogs created under the same folder when needed.
   * [ ] Standalone with proper Material imports.
   * [ ] Receive typed `MAT_DIALOG_DATA`.

6. **Routing**

   * [ ] Routes added in `app.routes.ts` under the correct area (`admin`, `league`, `teams`, etc.).
   * [ ] Guards applied correctly (`authGuard`, `adminGuard`, `teamOwnerGuard`).

7. **Navigation**

   * [ ] NavigationBar methods + template hooks if feature is user-facing.
   * [ ] Features toggled via `environment.features` if applicable.

8. **Auth / Interceptors**

   * [ ] Any truly public endpoint added to `isPublicRequest` in `auth-interceptor.ts`.
   * [ ] No protected endpoints whitelisted by mistake.

9. **UX & Errors**

   * [ ] Snackbars for success and error cases.
   * [ ] Loading spinners displayed while fetching data.
   * [ ] Validation errors surfaced in a user-friendly way.

10. **Cleanup**

    * [ ] Remove stray `console.log`s or gate them behind dev checks.
    * [ ] Ensure there are no hardcoded URLs or magic strings that should live in `environment` or constants.

---

## 14. Removing or Refactoring a Feature

When deprecating features:

* **Routes**:

  * Remove entries from `app.routes.ts`.
* **Navbar**:

  * Remove navigation methods and menu items from `NavigationBar`.
* **Services**:

  * Delete methods not used anywhere.
  * Eventually delete entire service if domain is removed.
* **Models**:

  * Remove interfaces/constants no longer referenced.
* **Shared components/pipes**:

  * Remove only after confirming no references remain.

Always run the app and ensure there are no runtime errors related to missing routes, services, or components.

---

This document reflects the **current architecture & methodology** of the Angular frontend, including **standalone components, signals, functional interceptors/guards, environment-based configuration, and domain-aligned models/services**. Use it as the source of truth when creating or modifying frontend features.
