Added image Storing Implentation.
# Frontend Workflow (Angular) — README

> **Goal**
> Keep a clear **API ⇄ Frontend** mapping and a repeatable way to **add features** without breaking security, routes, or styles. This README explains **where to create new files**, **what to modify**, and **how to surface API data** in the UI with Angular + Angular Material.

---

## Project Structure (overview)

```
src/
├─ styles.css                  → global styles (snackbars, animations)
├─ custom-theme.scss           → Material theme (M3)
├─ main.ts                     → bootstrap (standalone)
├─ index.html                  → root
├─ environments/
│  ├─ environment.ts
│  └─ environment.prod.ts      → apiUrl per environment
└─ app/
   ├─ app.ts                   → App root (router-outlet)
   ├─ app.config.ts            → Router, HttpClient + interceptors, Animations
   ├─ app.routes.ts            → Routes (lazy), guards and role data
   ├─ core/
   │  ├─ guards/
   │  │  ├─ auth-guard.ts
   │  │  └─ role-guard.ts
   │  ├─ interceptors/
   │  │  ├─ auth-interceptor.ts
   │  │  └─ error-interceptor.ts
   │  ├─ models/               → *TypeScript typings for DTOs/ViewModels*
   │  │  ├─ auth.model.ts
   │  │  ├─ location.model.ts
   │  │  └─ user.model.ts
   │  └─ services/
   │     ├─ api.ts             → Http wrapper (headers/tokens)
   │     ├─ auth.ts            → session, login, me, logout
   │     ├─ storage.ts         → localStorage (token/user)
   │     ├─ location.ts        → provinces/cantons/districts
   │     └─ user.ts            → registration, password change
   ├─ layouts/
   │  └─ main-layout/          → shell (navbar + outlet)
   ├─ pages/
   │  ├─ login/
   │  ├─ register/
   │  ├─ admin-dashboard/
   │  ├─ client-dashboard/
   │  └─ engineer-dashboard/
   └─ shared/components/
      └─ navigation-bar/
```

**Key rules**

* **Standalone components** (no NgModules), **lazy routes**, **Material 3** (custom-theme.scss).
* Backend **Controllers↔Services** are mirrored by frontend **services** and **models**.
* **Auth**: **auth-interceptor** adds `Authorization: Bearer {GUID}` except for public endpoints.
  **Guards**:

  * `authGuard` checks session.
  * `roleGuard` redirects to the correct dashboard if the role doesn’t match.
* **UX**: global errors via **error-interceptor** (MatSnackBar) + `.error-snackbar`/`.success-snackbar` classes.

---

## 🔧 Add a **new feature** (end-to-end)

Assume the API adds a **Projects** domain with endpoints:

* `POST /api/Project` (create)
* `PUT /api/Project/{id}` (update)
* `DELETE /api/Project/{id}` (delete)
* `GET /api/Project/{id}` (detail)
* `GET /api/Views/projects-summary` (ADMIN, view)

### 1) TS Types (Models) → `/src/app/core/models/project.model.ts`

```ts
export interface CreateProjectDTO {
  name: string;
  description?: string;
  startDate: Date;
  endDate?: Date;
}

export interface UpdateProjectDTO {
  name?: string;
  description?: string;
  startDate?: Date;
  endDate?: Date;
}

export interface ProjectResponse {
  projectID: number;
  name: string;
  description?: string;
  startDate: Date;
  endDate?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface ProjectSummaryView {
  projectID: number;
  name: string;
  isActive: boolean;
  createdAt: Date;
}
```

### 2) Domain Service → `/src/app/core/services/project.ts`

```ts
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Api } from './api';
import {
  CreateProjectDTO, UpdateProjectDTO,
  ProjectResponse, ProjectSummaryView
} from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class Project {
  private readonly api = inject(Api);

  create(dto: CreateProjectDTO) {
    return this.api.post<{ success: boolean; message: string; data?: any }>('/Project', dto);
  }

  update(id: number, dto: UpdateProjectDTO) {
    return this.api.put<{ success: boolean; message: string }>('/Project/' + id, dto);
  }

  delete(id: number) {
    return this.api.delete<{ success: boolean; message: string }>('/Project/' + id);
  }

  getById(id: number): Observable<ProjectResponse> {
    return this.api.get<ProjectResponse>('/Project/' + id);
  }

  // ADMIN-only view at /api/views/*
  getSummary(): Observable<ProjectSummaryView[]> {
    return this.api.get<ProjectSummaryView[]>('/Views/projects-summary');
  }
}
```

> **Important:** any new method on `Project` will **receive the token automatically** via `auth-interceptor` unless you whitelist it as public.

### 3) Page (UI) → `/src/app/pages/projects/projects.ts/.html/.css`

* **List** and **detail** (use Angular Material `mat-card`, `mat-table`/`mat-form-field`, etc.)
* **Snackbars** using classes defined in `styles.css`.
* Initial load from `Project.getSummary()` (ADMIN) or `getById()`.

```ts
// projects.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Project } from '../../core/services/project';
import { ProjectSummaryView } from '../../core/models/project.model';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule],
  templateUrl: './projects.html',
  styleUrl: './projects.css'
})
export class Projects {
  private readonly svc = inject(Project);
  data = signal<ProjectSummaryView[]>([]);
  ngOnInit() { this.svc.getSummary().subscribe(r => this.data.set(r)); }
}
```

### 4) Route (lazy) and **role** → `app.routes.ts`

Add **child** routes under the protected layout with role guard if needed:

```ts
{
  path: '',
  loadComponent: () => import('./layouts/main-layout/main-layout').then(m => m.MainLayout),
  canActivate: [authGuard],
  children: [
    // ...
    {
      path: 'admin/projects',
      loadComponent: () => import('./pages/projects/projects').then(m => m.Projects),
      canActivate: [roleGuard],
      data: { role: 'ADMIN' }                // ← protect the view
    }
  ]
}
```

### 5) Navbar

Insert the item for the appropriate role:

```ts
// inside getNavigationItems() when userType === 'ADMIN'
{ label: 'Projects', route: '/admin/projects', icon: 'work' },
```

### 6) Error interceptor

No changes required: any 401/403/500 shows a snackbar and, for 401, clears session and redirects to `/login`.

---

## Exposing new SQL **views** from the backend

1. **Create a ViewModel TS** matching the columns.
2. **Add a service method** that calls `/api/views/...`.
3. **Create a page** under ADMIN routes (`/admin/...`) or the right section.
4. **Do not** whitelist view endpoints in **auth-interceptor** (they’re protected).

---

## Authentication, authorization & security

* **auth-interceptor** adds `Authorization: Bearer {GUID}` to all except public endpoints:

  * Login & registration (`/Auth/login`, `/User/clients`, `/User/engineers`) and **GET** locations.
* **Guards** cover the rest:

  * `authGuard` → if no session, redirect to `/login`.
  * `roleGuard` → requires `data.role` on the route and redirects to the user’s dashboard if it doesn’t match.
* **Auth service** maintains state with **signals** (current user, role) and does:

  * `login` → saves token + basic user, then `fetchUserDetails()` from `/user/me/details`.
  * `logout` → calls `/auth/logout`, clears storage, navigates to `/login`.

> **Tip:** any **new page** requiring a role should be declared under the protected layout with `data: { role: '...' }`.

---

## UI Standard

* **Angular Material** (theme in `custom-theme.scss`), global classes in `styles.css`, animation `.animate-fade-in`.
* **Reactive forms** with clear validations (`mat-error`).
* **Consistent feedback**: `MatSnackBar` with `success-snackbar` / `error-snackbar`.
* **Layout**: everything lives under `MainLayout` (navbar + `<router-outlet>`).

---

## Environments & API URL (⚠️ recommended tweak)

Currently `Api` uses a fixed URL:

```ts
// core/services/api.ts (current)
private readonly apiUrl = 'https://localhost:7221/api';
```

**Recommended:** read from `environment.apiUrl` for dev/prod:

```ts
// core/services/api.ts
import { environment } from '../../../environments/environment';
private readonly apiUrl = environment.apiUrl;
```

> This avoids code changes when switching environments.

---

## Public vs. protected endpoints

* If you add a **public** endpoint (no token), include it in the `auth-interceptor` whitelist so it **won’t** add the header:

  ```ts
  const publicEndpoints = [
    '/api/Auth/login',
    '/api/User/clients',
    // ...add new public endpoints here
  ];
  ```
* **Do not** whitelist endpoints that require a token (you’d get silent 401s otherwise).

---

## Patterns & useful snippets

### Clean payload before POST

```ts
private cleanNullValues(data: any) {
  return Object.fromEntries(Object.entries(data).filter(([_, v]) => v !== null && v !== undefined && v !== ''));
}
```

### Signals (page state)

```ts
isLoading = signal(false);
data = signal<any[]>([]);
```

### Consistent snackbars

```ts
snackBar.open('Saved!', 'Close', { duration: 3000, panelClass: ['success-snackbar'] });
```

### Role-based nested routes

* **ADMIN**: `/admin/...`
* **ENGINEER**: `/engineer/...`
* **CLIENT**: `/client/...`

> Keep this convention to leverage `roleGuard` and navbar logic.

---

## Checklist for a **new CRUD**

* [ ] **API ready** (SPs/Views + endpoints).
* [ ] **TS models** in `/core/models/<domain>.model.ts`.
* [ ] **Service** in `/core/services/<domain>.ts` with typed methods.
* [ ] **Page** (standalone) in `/pages/<domain>/...` (HTML/CSS/TS).
* [ ] **Route** in `app.routes.ts` under the layout, with **guards** and `data.role` if needed.
* [ ] **Navbar** updated for the corresponding role.
* [ ] **Snackbars** and validations in place.
* [ ] **No hardcoded** API URL (use `environment.apiUrl`).
* [ ] **(If public)** add to **auth-interceptor** whitelist.

---

## Troubleshooting

* **401 after login** → invalid/missing token or endpoint mistakenly set as public (no `Authorization`).
  Check the `auth-interceptor` whitelist.
* **403** → route `data.role` ≠ `userType`. `roleGuard` will redirect to the user’s dashboard.
* **MatSnackBar: NullInjectorError** in interceptor → ensure global provider if needed:

  ```ts
  // app.config.ts
  import { importProvidersFrom } from '@angular/core';
  import { MatSnackBarModule } from '@angular/material/snack-bar';

  providers: [
    // ...
    importProvidersFrom(MatSnackBarModule)
  ]
  ```
* **Material theme not applied** → verify `custom-theme.scss` is included in project `styles` (angular.json) or imported from `styles.css`.

---

## Naming standards

* **Components/Pages**: `PascalCase` class, folder in `kebab-case` (e.g., `projects/`).
* **Services**: singular domain name (`Project`, `User`, `Auth`).
* **TS interfaces**: `PascalCase` with `DTO` / `Response` / `View` suffix as appropriate.

---

## Where to **create / modify / remove**

* **New domain**: `/core/models`, `/core/services`, `/pages/<domain>`, `app.routes.ts`, `navigation-bar`.
* **API contract changes**:

  * Adjust **TS models** and mappings in **services**.
  * Review pages’ forms/validations impacted by changes.
* **Remove feature**:

  * Delete page(s) and corresponding **navbar** entries.
  * Remove service methods (and the service file if empty).
  * Clean routes in `app.routes.ts`.
  * Drop TS models if no longer used.

---
