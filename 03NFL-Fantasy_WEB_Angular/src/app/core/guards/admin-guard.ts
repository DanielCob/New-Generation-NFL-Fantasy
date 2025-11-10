// src/app/core/guards/admin-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth-service';
import { UserService } from '../services/user-service';

function isAdminRole(role: unknown, payload?: any): boolean {
  if (payload?.IsAdmin === true || payload?.isAdmin === true) return true;
  const codes = [
    role,
    payload?.Role, payload?.role,
    payload?.RoleCode, payload?.roleCode,
    payload?.SystemRole, payload?.systemRole,
    payload?.SystemRoleCode, payload?.systemRoleCode
  ];
  for (const c of codes) {
    if (typeof c === 'number' && c === 1) return true;
    const s = String(c ?? '').toUpperCase();
    if (['ADMIN','SYSTEM_ADMIN','SYS_ADMIN'].includes(s)) return true;
  }
  return false;
}

export const adminGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const user = inject(UserService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
  if (environment.enableAdmin === true) return true;

  return user.getHeader().pipe(
    take(1),
    map((resp: any) => {
      // soportar data/Data o payload plano
      const p = resp?.data ?? resp?.Data ?? resp;
      const ok = isAdminRole(undefined, p);
      if (!ok) router.navigate(['/profile/header'], { queryParams: { forbidden: 1 } });
      return ok;
    }),
    catchError(() => {
      auth.clearLocalSession?.();
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return of(false);
    })
  );
};
