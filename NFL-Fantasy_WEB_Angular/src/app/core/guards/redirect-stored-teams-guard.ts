// src/app/core/guards/redirect-stored-team.guard.ts
import { CanActivateFn, ActivatedRouteSnapshot, Router } from '@angular/router';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

export const redirectStoredTeamGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const router = inject(Router);
  const snack = inject(MatSnackBar);

  const dest = (route.data?.['dest'] as string) || 'my-team';
  const raw = localStorage.getItem('xnf.currentTeamId');
  const id = raw ? Number(raw) : NaN;

  if (Number.isFinite(id) && id > 0) {
    return router.createUrlTree(['/teams', id, dest]);
  }

  snack.open('Select a team first (opening NFL Teams)', 'Close', { duration: 2500 });
  return router.createUrlTree(['/nfl-teams']);
};