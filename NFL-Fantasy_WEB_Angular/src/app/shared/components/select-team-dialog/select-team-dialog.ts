// src/app/shared/components/select-team-dialog/select-team-dialog.ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { UserService } from '../../../core/services/user.service';
import { UserTeam } from '../../../core/models/user.model';

@Component({
  standalone: true,
  selector: 'app-select-team-dialog',
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Select a team</h2>

    <div mat-dialog-content class="content">
      <div class="list" *ngIf="teams().length; else emptyTpl">
        @for (t of teams(); track t.TeamID) {
          <button mat-stroked-button class="row" (click)="select(t)">
            <span class="name">{{ t.TeamName }}</span>
            <span class="sub">&nbsp;· {{ t.LeagueName }}</span>
          </button>
        }
      </div>

      <ng-template #emptyTpl>
        <div class="empty">
          <mat-icon>info</mat-icon> No teams found.
        </div>
      </ng-template>
    </div>

    <div mat-dialog-actions align="end">
      <button mat-stroked-button (click)="close()">Cancel</button>
    </div>
  `,
  styles: [`
    .content { min-width: 360px; max-width: 520px; }
    .list { display: flex; flex-direction: column; gap: 8px; max-height: 320px; overflow: auto; margin-top: 8px; }
    .row { display: flex; align-items: center; gap: 10px; justify-content: flex-start; text-align: left; }
    .name { font-weight: 600; }
    .sub { color: #666; font-size: 12px; }
    .empty { display:flex; align-items:center; gap:8px; color:#666; padding:12px 0; }
  `]
})
export class SelectTeamDialog implements OnInit {
  private ref = inject(MatDialogRef<SelectTeamDialog>);
  private userSrv = inject(UserService);

  teams = signal<UserTeam[]>([]);

  ngOnInit(): void {
    this.userSrv.getProfile().subscribe({
      next: (p) => {
        const list = (p?.Teams ?? []).slice().sort(
          (a, b) =>
            (a.LeagueName || '').localeCompare(b.LeagueName || '') ||
            (a.TeamName   || '').localeCompare(b.TeamName   || '')
        );
        this.teams.set(list);
      }
    });
  }

  select(t: UserTeam): void {
    const id = t.TeamID;                 // 👈 usa el TeamID numérico
    if (!id) return;
    localStorage.setItem('xnf.currentTeamId', String(id));
    this.ref.close(String(id));
  }

  close(): void { this.ref.close(); }
}
