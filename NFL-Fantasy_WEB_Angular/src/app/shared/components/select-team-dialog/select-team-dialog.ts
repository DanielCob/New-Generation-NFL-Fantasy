// src/app/shared/components/select-team-dialog/select-team-dialog.ts
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { UserService } from '../../../core/services/user.service';
import { UserTeam } from '../../../core/models/user.model';

@Component({
  standalone: true,
  selector: 'app-select-team-dialog',
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatAutocompleteModule,
    MatButtonModule, MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>Select a team</h2>

    <div mat-dialog-content class="content">
      <form [formGroup]="searchForm">
        <mat-form-field appearance="outline" class="w100">
          <mat-label>Search team</mat-label>
          <input
            matInput
            formControlName="query"
            [matAutocomplete]="auto"
            placeholder="Type team or league name"
          />
          <mat-autocomplete #auto="matAutocomplete" (optionSelected)="select($event.option.value)">
            @for (t of filtered(); track t.TeamID) {
              <mat-option [value]="t">
                <div class="opt">
                  <div class="labels">
                    <div class="name">{{ t.TeamName }}</div>
                    <div class="sub">{{ t.LeagueName }}</div>
                  </div>
                </div>
              </mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>
      </form>

      <div class="list">
        @for (t of filtered(); track t.TeamID) {
          <button mat-stroked-button class="row" (click)="select(t)">
            <span class="name">{{ t.TeamName }}</span>
            <span class="sub">&nbsp;· {{ t.LeagueName }}</span>
          </button>
        }
      </div>

      <div class="empty" *ngIf="loaded() && !filtered().length">
        <mat-icon>info</mat-icon> No teams found.
      </div>

      <div class="muted" *ngIf="!loaded()">Loading teams…</div>
    </div>

    <div mat-dialog-actions align="end">
      <button mat-stroked-button (click)="close()">Cancel</button>
      <!-- fallback manual opcional: quítalo si no lo querés -->
      <button *ngIf="loaded() && !teams().length" mat-flat-button color="primary"
              (click)="confirmManual()" [disabled]="manualForm.invalid">
        Use Team ID
      </button>
    </div>
  `,
  styles: [`
    .content { min-width: 380px; max-width: 520px; }
    .w100 { width: 100%; }
    .list { display: flex; flex-direction: column; gap: 8px; max-height: 300px; overflow: auto; margin-top: 8px; }
    .row { display: flex; align-items: center; gap: 10px; justify-content: flex-start; }
    .name { font-weight: 600; }
    .sub { color: #666; font-size: 12px; }
    .opt { display: flex; align-items: center; gap: 8px; }
    .labels { display: flex; flex-direction: column; }
    .empty { display:flex; align-items:center; gap:8px; color:#666; padding:12px 0; }
    .muted { color:#777; padding-top:8px; }
  `]
})
export class SelectTeamDialog implements OnInit {
  private ref = inject(MatDialogRef<SelectTeamDialog>);
  private fb = inject(FormBuilder);
  private userSrv = inject(UserService);

  loaded = signal(false);
  teams = signal<UserTeam[]>([]);
  searchForm = this.fb.group({ query: [''] });
  manualForm = this.fb.group({ teamId: [null, [Validators.required]] }); // opcional

  filtered = computed(() => {
    const q = (this.searchForm.value.query ?? '').toLowerCase().trim();
    const all = this.teams();
    if (!q) return all;
    return all.filter(t =>
      (t.TeamName ?? '').toLowerCase().includes(q) ||
      (t.LeagueName ?? '').toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.userSrv.getHeader().subscribe({
      next: p => {
        const list = (p?.Teams ?? []).slice().sort((a,b) =>
          (a.LeagueName || '').localeCompare(b.LeagueName || '') ||
          (a.TeamName || '').localeCompare(b.TeamName || '')
        );
        this.teams.set(list);
        this.loaded.set(true);
      },
      error: () => this.loaded.set(true)
    });
  }

  select(t: UserTeam): void {
    if (!t) return;
    localStorage.setItem('xnf.currentTeamId', String(t.TeamID));
    this.ref.close(t.TeamID);
  }

  // fallback manual opcional
  confirmManual(): void {
    const id = Number(this.manualForm.value.teamId);
    if (!Number.isFinite(id) || id <= 0) return;
    localStorage.setItem('xnf.currentTeamId', String(id));
    this.ref.close(id);
  }

  close() { this.ref.close(); }
}
