import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { UserProfile, CommissionedLeague, UserTeam } from '../../../core/models/user-model';
import { TableSimple, TableColumn } from '../../../shared/components/table-simple/table-simple';
import { UserService } from '../../../core/services/user-service';

@Component({
  selector: 'app-full-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatListModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    TableSimple,
  ],
  templateUrl: './full-profile.html',
  styleUrl: './full-profile.css'
})
export class FullProfile implements OnInit {
  private userSvc = inject(UserService);

  loading = signal(true);
  error   = signal<string | null>(null);
  profile = signal<UserProfile | null>(null);

  // Columnas: Ligas como comisionado
  commissionedColumns: TableColumn<CommissionedLeague>[] = [
    { label: 'League',   key: 'LeagueName' },
    { label: 'Slots',    key: 'TeamSlots' },
    { label: 'Role',     key: 'RoleCode' },
    { label: 'Primary',  key: 'IsPrimaryCommissioner', format: 'yesno' },
    { label: 'Status',   key: 'Status', formatter: (r) => this.statusLabel(r.Status) },
    { label: 'Joined',   key: 'JoinedAt', format: 'date', dateFormat: 'mediumDate' },
  ];

  // Columnas: Mis equipos
  teamsColumns: TableColumn<UserTeam>[] = [
    { label: 'Team',   key: 'TeamName' },
    { label: 'League', key: 'LeagueName' },
    { label: 'Since',  key: 'CreatedAt', format: 'date', dateFormat: 'mediumDate' },
  ];

  trackByLeague = (_: number, l: CommissionedLeague) => l.LeagueID;
  trackByTeam   = (_: number, t: UserTeam)         => t.TeamID;

  ngOnInit(): void {
    this.load();
  }

  /** Carga perfil COMPLETO (sp_GetUserProfile) */
  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.userSvc.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: (e) => {
        const msg =
          e?.error?.Message || e?.error?.message || 'No se pudo cargar el perfil.';
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }

  /** Mapeo visual del status de liga (ajusta a tu catálogo real) */
  statusLabel(status: number): string {
    switch (status) {
      case 0: return 'Draft';
      case 1: return 'Active';
      case 2: return 'Paused';
      case 3: return 'Closed';
      default: return String(status);
    }
  }

  /** Color de chip para status (referencia futura) */
  statusChipColor(status: number): 'primary' | 'accent' | 'warn' {
    if (status === 1) return 'primary';
    if (status === 2) return 'accent';
    return 'warn';
  }
}
