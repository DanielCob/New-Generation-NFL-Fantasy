// manage-roster.component.ts - ARCHIVO COMPLETO ARREGLADO

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiResponse } from '../../../core/models/common-model';
import { MyTeamResponse, RosterItem, AddPlayerToRosterDTO } from '../../../core/models/team-model';
import { AddPlayerDialogComponent } from './add-player/add-player';
import { TeamService } from '../../../core/services/team-service';

@Component({
  selector: 'app-manage-roster',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatIconModule, MatButtonModule,
    MatSnackBarModule, MatTableModule, MatDialogModule, MatProgressSpinnerModule
  ],
  templateUrl: './manage-roster.html',
  styleUrls: ['./manage-roster.css'],
})
export class ManageRosterComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private teamSrv = inject(TeamService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  teamId = signal<number>(0);
  loading = signal(true);
  saving = signal(false);
  myTeam = signal<MyTeamResponse | null>(null);

  roster = computed<RosterItem[]>(() => this.myTeam()?.roster ?? []);

  displayed = ['player', 'pos', 'team', 'actions'];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.teamId.set(id);
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.teamSrv.getMyTeam(this.teamId()).subscribe({
      next: (res: ApiResponse<MyTeamResponse>) => {
        console.log('🔍 Respuesta de getMyTeam en manage-roster:', res);
        
        const apiData = (res as any)?.Data ?? (res as any)?.data;
        
        if (apiData) {
          const mappedTeam: MyTeamResponse = {
            teamId: apiData.TeamID ?? apiData.teamId ?? 0,
            teamName: apiData.TeamName ?? apiData.teamName ?? '',
            leagueId: apiData.LeagueID ?? apiData.leagueId ?? 0,
            leagueName: apiData.LeagueName ?? apiData.leagueName ?? '',
            teamImageUrl: apiData.TeamImageUrl ?? apiData.teamImageUrl,
            thumbnailUrl: apiData.ThumbnailUrl ?? apiData.thumbnailUrl,
            roster: this.mapRoster(apiData.Roster ?? apiData.roster ?? []),
            distribution: apiData.Distribution ?? apiData.distribution ?? []
          };
          
          this.myTeam.set(mappedTeam);
          localStorage.setItem('xnf.currentTeamId', String(mappedTeam.teamId));
        } else {
          this.myTeam.set(null);
        }
        
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ Error en reload:', err);
        this.loading.set(false);
      },
    });
  }

  private mapRoster(roster: any[]): RosterItem[] {
    return roster.map(r => ({
      rosterID: r.RosterID ?? r.rosterID ?? 0,
      playerID: r.PlayerID ?? r.playerID ?? 0,
      fullName: r.FullName ?? r.fullName ?? '',
      firstName: r.FirstName ?? r.firstName,
      lastName: r.LastName ?? r.lastName,
      position: r.Position ?? r.position ?? '',
      nflTeamName: r.NFLTeamName ?? r.nflTeamName,
      photoUrl: r.PhotoUrl ?? r.photoUrl,
      photoThumbnailUrl: r.PhotoThumbnailUrl ?? r.photoThumbnailUrl,
      acquisitionType: r.AcquisitionType ?? r.acquisitionType,
      acquiredAt: r.AcquiredAt ?? r.acquiredAt ?? r.AcquisitionDate ?? r.acquisitionDate,
      isStarter: r.IsStarter ?? r.isStarter,
      isIR: r.IsIR ?? r.isIR
    }));
  }

  openAddPlayerDialog(): void {
    const ref = this.dialog.open(AddPlayerDialogComponent, { width: '420px', data: {} });
    ref.afterClosed().subscribe((dto?: AddPlayerToRosterDTO) => {
      if (!dto) return;
      this.saving.set(true);
      this.teamSrv.addPlayerToRoster(this.teamId(), dto).subscribe({
        next: (res) => {
          console.log('🔍 Respuesta de addPlayerToRoster:', res);
          
          const success = (res as any)?.success ?? (res as any)?.Success ?? false;
          const message = (res as any)?.message ?? (res as any)?.Message ?? 'Player added';
          
          if (success) {
            this.snack.open(message, 'Close', { duration: 2500 });
            this.reload();
          } else {
            this.snack.open(message, 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
          }
          this.saving.set(false);
        },
        error: (err) => {
          console.error('❌ Error al agregar jugador:', err);
          const e = err?.error ?? err;
          const msg = e?.message ?? e?.Message ?? 'Could not add player';
          this.snack.open(msg, 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
          this.saving.set(false);
        }
      });
    });
  }

  remove(r: RosterItem): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.teamSrv.removePlayerFromRoster(r.rosterID).subscribe({
      next: (res) => {
        console.log('🔍 Respuesta de removePlayerFromRoster:', res);
        
        const success = (res as any)?.success ?? (res as any)?.Success ?? false;
        const message = (res as any)?.message ?? (res as any)?.Message ?? 'Player removed';
        
        if (success) {
          this.snack.open(message, 'Close', { duration: 2500 });
          this.reload();
        } else {
          this.snack.open(message, 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        }
        this.saving.set(false);
      },
      error: (err) => {
        console.error('❌ Error al remover jugador:', err);
        const e = err?.error ?? err;
        const msg = e?.message ?? e?.Message ?? 'Could not remove player';
        this.snack.open(msg, 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        this.saving.set(false);
      }
    });
  }
}