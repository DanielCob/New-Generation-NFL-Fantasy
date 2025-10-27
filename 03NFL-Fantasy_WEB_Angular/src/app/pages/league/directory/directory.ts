import { Component, Input, OnChanges, OnInit, SimpleChanges, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { LeagueService } from '../../../core/services/league-service';
import { LeagueDirectoryItem } from '../../../core/models/league-model';
import { JoinLeagueDialogComponent, JoinLeagueDialogResult } from './join-league-dialog.ts/join-league.dialog';

@Component({
  selector: 'league-directory',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './directory.html',
  styleUrls: ['./directory.css']
})
export class LeagueDirectoryComponent implements OnInit, OnChanges {
  // filtros opcionales
  @Input() seasonId?: number;
  @Input() status?: number;
  // control de UI
  @Input() showFilter: boolean = true;
  @Input() showJoinButton: boolean = true;

  private svc = inject(LeagueService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  loading = signal(true);
  error = signal<string | null>(null);
  filter = signal('');
  rows = signal<LeagueDirectoryItem[]>([]);
  displayedColumns = ['LeagueID','Name','SeasonLabel','Teams','Available','CreatedAt','actions'];

  ngOnInit(): void { this.load(); }

  ngOnChanges(changes: SimpleChanges): void {
    if ('seasonId' in changes || 'status' in changes) this.load();
  }

load(): void {
  this.loading.set(true);
  this.error.set(null);

  this.svc.getDirectory({ seasonId: this.seasonId, status: this.status }).subscribe({
    next: (res: any) => {
      const list = res?.data ?? res?.Data ?? [];
      this.rows.set(Array.isArray(list) ? list : []);
      this.loading.set(false);
    },
    error: (e) => {
      console.error(e);
      this.error.set('Failed to load leagues directory.');
      this.loading.set(false);
    }
  });
}


  filteredRows = computed(() => {
    const q = this.filter().trim().toLowerCase();
    if (!q) return this.rows();
    return this.rows().filter(x =>
      (x.Name ?? '').toLowerCase().includes(q) ||
      String(x.LeagueID).includes(q) ||
      (x.SeasonLabel ?? '').toLowerCase().includes(q)
    );
  });

  join(row: LeagueDirectoryItem) {
    const ref = this.dialog.open<JoinLeagueDialogComponent, {leagueId:number, leagueName:string}, JoinLeagueDialogResult>(
      JoinLeagueDialogComponent,
      { width: '420px', data: { leagueId: row.LeagueID, leagueName: row.Name } }
    );

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.svc.joinLeague({
        LeagueID: result.leagueId,
        LeaguePassword: result.password,
        TeamName: result.teamName
      }).subscribe({
        next: (res) => {
          this.snack.open(res.message || 'Joined league successfully', 'OK', { duration: 3000 });
          this.load(); // actualiza AvailableSlots
        },
        error: (e) => {
          console.error(e);
          this.snack.open('Could not join league', 'Dismiss', { duration: 3500 });
        }
      });
    });
  }
}
