// src/app/core/guards/admin-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth-service';
import { UserService } from '../services/user-service';

export const adminGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const user = inject(UserService);
  const router = inject(Router);

  // 1) Requiere autenticación
  if (!auth.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // 2) Whitelist por environment (útil en DEV)
  if (environment.enableAdmin === true) {
    return true;
  }

  // 3) Verificar rol consultando el header del usuario
  return user.getHeader().pipe(
    take(1),
    map(p => {
      const role = (p as any)?.Role ?? (p as any)?.SystemRoleCode ?? (p as any)?.role ?? (p as any)?.systemRoleCode;
      const isAdmin = typeof role === 'string' ? role.toUpperCase() === 'ADMIN' : role === 1;
      if (!isAdmin) {
        router.navigate(['/profile/header'], { queryParams: { forbidden: 1 } });
      }
      return isAdmin;
    }),
    catchError(() => {
      // Si falla el ping o no se puede leer perfil, tratamos como no autorizado
      auth.clearLocalSession?.();
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return of(false);
    })
  );
};