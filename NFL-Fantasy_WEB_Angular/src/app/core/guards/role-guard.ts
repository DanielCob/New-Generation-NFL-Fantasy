// src/app/core/guards/role-guard.ts
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { Auth } from '../services/auth';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(Auth);
  const router = inject(Router);

  const requiredRole = route.data['role'] as string;
  const userType = authService.userType();

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  if (requiredRole && userType !== requiredRole) {
    // Redirect to appropriate dashboard
    switch (userType) {
      case 'ADMIN':
        router.navigate(['/admin']);
        break;
      case 'ENGINEER':
        router.navigate(['/engineer']);
        break;
      case 'CLIENT':
        router.navigate(['/client']);
        break;
      default:
        router.navigate(['/']);
    }
    return false;
  }

  return true;
};