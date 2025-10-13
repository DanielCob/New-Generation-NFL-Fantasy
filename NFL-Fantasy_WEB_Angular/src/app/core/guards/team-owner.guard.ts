import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { TeamService } from '../services/team.service';
import { AuthService } from '../services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Guard que valida que el usuario sea dueño del equipo
 * Usado en: /teams/:id/edit-branding, /teams/:id/manage-roster
 */
export const teamOwnerGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const teamService = inject(TeamService);
  const authService = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  const teamId = Number(route.paramMap.get('id'));
  const currentUserId = authService.getCurrentUserId(); // Método a agregar en AuthService

  if (!teamId || isNaN(teamId)) {
    router.navigate(['/not-found']);
    return false;
  }

  return teamService.getMyTeam(teamId).pipe(
    map(response => {
      if (response.success && response.data) {
        // Verificar que el usuario actual sea el dueño
        // Esto se valida en el backend, pero aquí verificamos para UX
        return true;
      }

      snackBar.open('No tienes permisos para editar este equipo', 'Cerrar', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
      router.navigate(['/leagues']);
      return false;
    }),
    catchError(error => {
      if (error.status === 403) {
        snackBar.open('No eres el dueño de este equipo', 'Cerrar', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
      }
      router.navigate(['/leagues']);
      return of(false);
    })
  );
};
