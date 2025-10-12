// src/app/core/guards/auth-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { UserService } from '../services/user.service';

/**
 * Reglas:
 * - Si NO hay SessionID local -> redirige a /login.
 * - Si hay SessionID -> hace un GET (getHeader) para validar/refresh en backend.
 *   * Si el GET ok -> permite navegación.
 *   * Si falla -> limpia sesión y redirige a /login?returnUrl=...
 */
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const user = inject(UserService);
  const router = inject(Router);

  // 1) No hay sesión local
  if (!auth.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // 2) Hay sesión local -> ping GET para refrescar/validar en backend
  return user.getHeader().pipe(
    take(1),
    map(() => true),
    catchError(() => {
      // Si el ping falla, invalida sesión local y manda a login
      auth.clearLocalSession?.();
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return of(false);
    })
  );
};
