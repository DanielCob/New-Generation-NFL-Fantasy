// src/app/app.routes.ts - REPLACE existing content
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then(m => m.Register)
  },
  {
    path: '',
    loadComponent: () => import('./layouts/main-layout/main-layout').then(m => m.MainLayout),
    canActivate: [authGuard],
    children: [
      {
        path: 'admin',
        loadComponent: () => import('./pages/admin-dashboard/admin-dashboard').then(m => m.AdminDashboard),
        canActivate: [roleGuard],
        data: { role: 'ADMIN' }
      },
      {
        path: 'engineer',
        loadComponent: () => import('./pages/engineer-dashboard/engineer-dashboard').then(m => m.EngineerDashboard),
        canActivate: [roleGuard],
        data: { role: 'ENGINEER' }
      },
      {
        path: 'client',
        loadComponent: () => import('./pages/client-dashboard/client-dashboard').then(m => m.ClientDashboard),
        canActivate: [roleGuard],
        data: { role: 'CLIENT' }
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];