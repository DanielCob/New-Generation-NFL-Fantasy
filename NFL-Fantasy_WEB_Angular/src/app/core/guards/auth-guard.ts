// src/app/core/guards/auth-guard.ts
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }
  // Devolver UrlTree en vez de llamar router.navigate
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
