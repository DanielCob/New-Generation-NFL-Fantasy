/**
 * sessions.ts
 * -----------------------------------------------------------------------------
 * FIX:
 * - Al hacer logoutAll(), NO volvemos a llamar this.load() porque la sesión
 *   ya se limpia en finalize() del AuthService y esa recarga provocaba un
 *   GET /api/User/sessions -> 401 -> redirección por interceptor.
 * - En su lugar, navegamos explícitamente al /login tras la respuesta (tanto
 *   en success como en error), eliminando el "request fantasma" y el 401.
 */

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router } from '@angular/router';

import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserSession } from '../../../core/models/user.model';
import { ApiResponse } from '../../../core/models/auth.model';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatSnackBarModule,
  ],
  templateUrl: './sessions.html',
  styleUrls: ['./sessions.css'],
})
export class Sessions implements OnInit {
  private userService = inject(UserService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private router = inject(Router);

  loading = signal(true);
  loadingAction = signal(false);
  sessions = signal<UserSession[]>([]);

  // ID de la sesión actual (opcional para resaltar fila)
  currentSessionId = computed(() => this.auth.session?.SessionID ?? null);

  displayedColumns = ['SessionID', 'CreatedAt', 'LastActivityAt', 'ExpiresAt', 'IsValid'];

  ngOnInit(): void {
    this.load();
  }

  /** Carga inicial de sesiones activas */
  private load(): void {
    this.loading.set(true);
    this.userService.getActiveSessions().subscribe({
      next: (rows) => {
        this.sessions.set(rows ?? []);
        this.loading.set(false);
      },
      error: () => {
        // Si hay error (por ejemplo, 401), solo cerramos loading.
        // El interceptor ya se encarga de redirigir si corresponde.
        this.loading.set(false);
      }
    });
  }

  /**
   * Acción: Cerrar todas las sesiones (POST /api/Auth/logout-all)
   * - AuthService.logoutAll() hace finalize(() => clearSession()) → nos deja sin token.
   * - Antes recargábamos la tabla y eso provocaba 401. Lo quitamos.
   * - Redirigimos manualmente a /login en éxito o error (ya no hay sesión local).
   */
  closeAll(): void {
    if (this.loadingAction()) return;
    this.loadingAction.set(true);

    this.auth.logoutAll().subscribe({
      next: (res: ApiResponse<string>) => {
        const msg = res?.message || 'Se cerraron todas las sesiones.';
        // Mostramos el mensaje; si prefieres, puedes omitir el snack al navegar.
        this.snack.open(msg, 'Cerrar', { duration: 2000 });

        // ✅ NO llamar this.load() (evita GET /User/sessions → 401)
        // ✅ Navegar al login explícitamente (sesión local ya está limpia)
        this.router.navigate(['/login']);
        this.loadingAction.set(false);
      },
      error: () => {
        // Aunque falle, finalize() en AuthService limpia la sesión local.
        this.snack.open('Sesión finalizada. Vuelve a iniciar sesión.', 'Cerrar', { duration: 3000 });
        this.router.navigate(['/login']);
        this.loadingAction.set(false);
      }
    });
  }

  // Para performance en *ngFor
  trackBySession = (_: number, s: UserSession) => s.SessionID;
}
