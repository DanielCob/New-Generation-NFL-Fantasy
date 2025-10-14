// src/app/core/guards/no-auth-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';

export const noAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) {
    // si ya está logueado, lo mando a un lugar protegido
    router.navigate(['/profile/header']);
    return false;
  }
  return true;
};
