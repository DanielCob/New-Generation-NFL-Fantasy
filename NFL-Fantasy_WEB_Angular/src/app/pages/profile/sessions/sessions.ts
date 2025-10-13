// src/app/pages/profile/sessions/sessions.ts
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

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

  loading = signal(true);
  loadingAction = signal(false);
  sessions = signal<UserSession[]>([]);

  // ID de la sesión actual (opcional para resaltar fila)
  currentSessionId = computed(() => this.auth.session?.SessionID ?? null);

  displayedColumns = ['SessionID', 'CreatedAt', 'LastActivityAt', 'ExpiresAt', 'IsValid'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.userService.getActiveSessions().subscribe({
      next: (rows) => {
        this.sessions.set(rows ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  // Acción: Cerrar todas las sesiones (POST /api/auth/logout-all)
  closeAll(): void {
    if (this.loadingAction()) return;
    this.loadingAction.set(true);

    this.auth.logoutAll().subscribe({
      next: (res: ApiResponse<string>) => {
        if (res.success) {
          this.snack.open(res.message || 'Se cerraron todas las sesiones.', 'Cerrar', { duration: 3000 });
          // refrescamos la tabla
          this.load();
        } else {
          this.snack.open(res.message || 'No fue posible cerrar las sesiones.', 'Cerrar', { duration: 4000 });
        }
        this.loadingAction.set(false);
      },
      error: () => {
        this.snack.open('No fue posible cerrar las sesiones.', 'Cerrar', { duration: 5000 });
        this.loadingAction.set(false);
      }
    });
  }

  // Para performance en *ngFor
  trackBySession = (_: number, s: UserSession) => s.SessionID;
}
