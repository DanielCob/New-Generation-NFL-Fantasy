import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../services/user-service';

/**
 * Guard que valida que el usuario sea dueño del equipo
 * Usado en: /teams/:id/edit-branding, /teams/:id/manage-roster
 */
export const teamOwnerGuard: CanActivateFn = (route) => {
  const userSvc = inject(UserService);
  const router  = inject(Router);
  const snack   = inject(MatSnackBar);

  const rawId = route.paramMap.get('id');
  const id = rawId ? Number(rawId) : NaN;      // 👈 numérico
  if (!Number.isFinite(id) || id <= 0) {
    snack.open('Invalid team id', 'Close', { duration: 2500 });
    return router.createUrlTree(['/nfl-teams']);
  }

  return userSvc.getProfile().pipe(
    map(p => {
      const owns = (p?.Teams ?? []).some(t => t.TeamID === id); // 👈 compara TeamID
      if (owns) return true;
      snack.open('You don’t own this team. Select one of your teams.', 'Close', { duration: 3000 });
      return router.createUrlTree(['/my-team']);
    }),
    catchError(() => {
      snack.open('Could not verify team ownership.', 'Close', { duration: 2500 });
      return of(router.createUrlTree(['/nfl-teams']));
    })
  );
};