// core/guards/admin-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, take } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthService } from '../services/auth-service';
import { AuthzService } from '../services/authz/authz.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const authz = inject(AuthzService);

  if (!auth.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  authz.refresh();

  return authz.isAdmin$.pipe(
    take(1),
    map(ok => ok ? true : router.createUrlTree(['/profile/header'], { queryParams: { forbidden: 1 } })),
    catchError(() => {
      auth.clearLocalSession?.();
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return of(false);
    })
  );
};
