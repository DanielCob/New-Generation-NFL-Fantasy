// src/app/pages/league/league-actions/league-actions.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { LeagueService } from '../../../core/services/league-service';

import { SummaryDialog } from '../summary/summary-dialog/summary-dialog';
import { EditConfigDialog } from '../edit-config/edit-config-dialog/edit-config-dialog';
import { MembersDialog } from '../members/members-dialog/members-dialog';
import { TeamsDialog } from '../teams/teams-dialog/teams-dialog';

@Component({
  standalone: true,
  selector: 'app-league-actions',
  imports: [
    CommonModule, MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatSelectModule, MatInputModule,
    ReactiveFormsModule, MatSnackBarModule, MatDialogModule
  ],
  templateUrl: './league-actions.html',
  styleUrls: ['./league-actions.css']
})
export class LeagueActionsComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder).nonNullable;
  private snack = inject(MatSnackBar);
  private leagues = inject(LeagueService);
  private dialog = inject(MatDialog);

  // ✅ Ahora es LeaguePublicID desde la ruta
  leaguePublicId = Number(this.route.snapshot.paramMap.get('id'));
  name = localStorage.getItem('xnf.currentLeagueName') ?? `League ${this.leaguePublicId}`;

  statusOptions = [
    { value: 0, label: 'Drafting' },
    { value: 1, label: 'Active' },
    { value: 2, label: 'Completed' },
    { value: 3, label: 'Archived' }
  ];

  statusForm = this.fb.group({
    NewStatus: this.fb.control<number | null>(null, { validators: [Validators.required] }),
    Reason: this.fb.control('', { validators: [Validators.maxLength(200)] })
  });

  constructor() {
    console.log('🎯 [LeagueActions] LeaguePublicID:', this.leaguePublicId);
  }

  // ✅ Actualizar para pasar leaguePublicId
  openSummaryDialog(): void {
    console.log('🔍 [LeagueActions] Abriendo Summary con LeaguePublicID:', this.leaguePublicId);
    this.dialog.open(SummaryDialog, {
      data: { leaguePublicId: this.leaguePublicId }, // ✅ Cambio: leaguePublicId
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }

  openEditDialog(): void {
    this.dialog.open(EditConfigDialog, {
      data: { leaguePublicId: this.leaguePublicId }, // ✅ Cambio
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }

  openMembersDialog(): void {
    this.dialog.open(MembersDialog, {
      data: { leaguePublicId: this.leaguePublicId }, // ✅ Cambio
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }

  openTeamsDialog(): void {
    this.dialog.open(TeamsDialog, {
      data: { leaguePublicId: this.leaguePublicId }, // ✅ Cambio
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }

  setStatus() {
    if (this.statusForm.invalid) {
      this.statusForm.markAllAsTouched();
      return;
    }
    const v = this.statusForm.getRawValue();
    // ✅ Usar leaguePublicId
    this.leagues.setStatus(this.leaguePublicId, { NewStatus: v.NewStatus!, Reason: v.Reason ?? '' })
      .subscribe({
        next: (r) => this.snack.open(r?.message || 'Status updated', 'OK', { duration: 2500 }),
        error: (e) => this.snack.open(e?.error?.message || 'Failed to update status', 'OK', { duration: 3000 })
      });
  }
}
