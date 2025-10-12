// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
// import { roleGuard } from './core/guards/role-guard'; // (borra si no lo usas)

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.Login),
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then(m => m.Register),
  },

  // Área autenticada (usa tu MainLayout standalone)
  {
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then(m => m.MainLayout),
    canActivate: [authGuard],
    children: [
      // agrega aquí tus rutas hijas protegidas
      // { path: 'profile/header', loadComponent: () => import('./pages/profile/header').then(m => m.ProfileHeader) },
      // { path: 'leagues', loadComponent: () => import('./pages/leagues/leagues').then(m => m.Leagues) },
    ],
  },

  { path: '**', redirectTo: 'login' },
];
