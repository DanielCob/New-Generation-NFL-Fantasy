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
        this.myTeam.set(res.data ?? null);
        localStorage.setItem('xnf.currentTeamId', String(this.teamId()));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openAddPlayerDialog(): void {
    const ref = this.dialog.open(AddPlayerDialogComponent, { width: '420px', data: {} });
    ref.afterClosed().subscribe((dto?: AddPlayerToRosterDTO) => {
      if (!dto) return;
      this.saving.set(true);
      this.teamSrv.addPlayerToRoster(this.teamId(), dto).subscribe({
        next: (res) => {
          if (res.success) {
            this.snack.open(res.message || 'Player added', 'Close', { duration: 2500 });
            this.reload();
          } else {
            this.snack.open(res.message || 'Could not add player', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
          }
          this.saving.set(false);
        },
        error: () => {
          this.snack.open('Could not add player', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
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
        if (res.success) {
          this.snack.open(res.message || 'Player removed', 'Close', { duration: 2500 });
          this.reload();
        } else {
          this.snack.open(res.message || 'Could not remove player', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        }
        this.saving.set(false);
      },
      error: () => {
        this.snack.open('Could not remove player', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        this.saving.set(false);
      }
    });
  }
}
