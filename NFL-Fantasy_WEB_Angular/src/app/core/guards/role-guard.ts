// src/app/core/guards/role-guard.ts
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = route.data['role'] as string;


  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }


  return true;
};
