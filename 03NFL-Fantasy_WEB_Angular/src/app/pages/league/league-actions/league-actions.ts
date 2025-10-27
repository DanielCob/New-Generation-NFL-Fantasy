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

// Dialog wrappers (uno por acción)
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

  id = Number(this.route.snapshot.paramMap.get('id'));
  name = localStorage.getItem('xnf.currentLeagueName') ?? `League ${this.id}`;

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

  // Navegación (si quieres mantener rutas)
  go(path: 'edit' | 'members' | 'teams') {
    this.router.navigate(['/league', this.id, path === 'edit' ? 'edit' : path]);
  }

  // Acciones (cada una abre su popup)
  openSummaryDialog(): void {
    this.dialog.open(SummaryDialog, {
      data: { leagueId: this.id },
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }
  openEditDialog(): void {
    this.dialog.open(EditConfigDialog, {
      data: { leagueId: this.id },
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }
  openMembersDialog(): void {
    this.dialog.open(MembersDialog, {
      data: { leagueId: this.id },
      panelClass: 'dlg-auto',
      maxWidth: '100vw',
      maxHeight: '100vh'
    });
  }
  openTeamsDialog(): void {
    this.dialog.open(TeamsDialog, {
      data: { leagueId: this.id },
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
    this.leagues.setStatus(this.id, { NewStatus: v.NewStatus!, Reason: v.Reason ?? '' })
      .subscribe({
        next: (r) => this.snack.open(r?.message || 'Status updated', 'OK', { duration: 2500 }),
        error: (e) => this.snack.open(e?.error?.message || 'Failed to update status', 'OK', { duration: 3000 })
      });
  }
}
