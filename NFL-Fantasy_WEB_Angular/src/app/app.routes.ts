// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { noAuthGuard } from './core/guards/no-auth-guard';
import { teamOwnerGuard } from './core/guards/team-owner.guard';
import { redirectStoredTeamGuard } from './core/guards/redirect-stored-teams.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  // ------- RUTAS PÚBLICAS -------
  { path: 'login',
    loadComponent: () => import('./pages/auth/login/login').then(m => m.Login),
    canActivate: [noAuthGuard] // opcional
  },
  { path: 'register',
    loadComponent: () => import('./pages/auth/register/register').then(m => m.Register),
    canActivate: [noAuthGuard] // opcional
  },
  { path: 'request-reset',
    loadComponent: () => import('./pages/auth/request-reset/request-reset').then(m => m.RequestReset)
  },
  { path: 'reset-password',
    loadComponent: () => import('./pages/auth/reset/reset').then(m => m.Reset)
  },

{
    path: 'reference/position-formats',
    loadComponent: () => import('./pages/reference/position-formats/position-formats').then(m => m.PositionFormats)
  },
  {
    path: 'reference/scoring-schemas',
    loadComponent: () => import('./pages/reference/scoring-schemas/scoring-schemas').then(m => m.ScoringSchemas)
  },


  // (OPCIONAL) Directorio público. Si lo quieres protegido, muévelo a la sección de abajo.
  { path: 'directory',
    loadComponent: () => import('./pages/league/directory/directory').then(m => m.Directory)
  },

  // ------- ZONA PROTEGIDA BAJO LAYOUT -------
  {
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then(m => m.MainLayout),
    canActivate: [authGuard],
    children: [
      // Profile
      { path: 'profile/header',
        loadComponent: () => import('./pages/profile/profile-header/profile-header').then(m => m.ProfileHeader)
      },
      { path: 'profile/sessions',
        loadComponent: () => import('./pages/profile/sessions/sessions').then(m => m.Sessions)
      },
      { path: 'profile/full-profile',
        loadComponent: () => import('./pages/profile/full-profile/full-profile').then(m => m.FullProfile)
      },

      // League
      { path: 'league/create',
        loadComponent: () => import('./pages/league/create/create').then(m => m.Create)
      },
      { path: 'league/:id/summary',
        loadComponent: () => import('./pages/league/summary/summary').then(m => m.Summary)
      },
      { path: 'league/:id/edit',
        loadComponent: () => import('./pages/league/edit-config/edit-config').then(m => m.EditConfig)
      },
      { path: 'league/:id/members',
        loadComponent: () => import('./pages/league/members/members').then(m => m.Members)
      },
      { path: 'league/:id/teams',
        loadComponent: () => import('./pages/league/teams/teams').then(m => m.Teams)
      },

            // 🆕 TEAMS (Feature 3.1)
      {
  path: 'my-team',
    canActivate: [redirectStoredTeamGuard],
    data: { dest: 'my-team' },
    loadComponent: () =>
      import('./core/route-shells/empty-redirect-shell').then(m => m.EmptyRedirectShell),
  },
  {
    path: 'teams/edit-branding',
    canActivate: [redirectStoredTeamGuard],
    data: { dest: 'edit-branding' },
    loadComponent: () =>
      import('./core/route-shells/empty-redirect-shell').then(m => m.EmptyRedirectShell),
  },
  {
    path: 'teams/manage-roster',
    canActivate: [redirectStoredTeamGuard],
    data: { dest: 'manage-roster' },
    loadComponent: () =>
      import('./core/route-shells/empty-redirect-shell').then(m => m.EmptyRedirectShell),
  },

      {
        path: 'teams',
        children: [
          {
            path: ':id/my-team',
            loadComponent: () => import('./pages/teams/my-team/my-team').then(m => m.MyTeamComponent)
          },
          {
            path: ':id/edit-branding',
            canActivate: [teamOwnerGuard],
            loadComponent: () => import('./pages/teams/edit-branding/edit-branding').then(m => m.EditBrandingComponent)
          },
          {
            path: ':id/manage-roster',
            canActivate: [teamOwnerGuard],
            loadComponent: () => import('./pages/teams/manage-roster/manage-roster').then(m => m.ManageRosterComponent)
          }
        ]
      },

      // 🆕 NFL TEAMS (Feature 10.1)
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

      // 🆕 PLAYERS (opcional)
      {
        path: 'players',
        children: [
          {
            path: 'browser',
            loadComponent: () => import('./pages/players/browser/browser').then(m => m.Browser)
          }
        ]
      },

      // DIRECTORY (existente)
      {
        path: 'directory',
        loadComponent: () => import('./pages/directory/directory').then(m => m.Directory)
      },



      // Admin-only (si lo usas)
      {
        path: 'views',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/views/league-summary-admin/league-summary-admin').then(m => m.LeagueSummaryAdmin)
        // o loadChildren si vas a colgar más subrutas de admin
      },
    ]
  },

  { path: '**', redirectTo: 'login' }
];
