import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';

export interface JoinLeagueDialogResult {
  leagueId: number;
  password: string;
  teamName: string;
}

@Component({
  selector: 'app-join-league-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, ReactiveFormsModule],
  templateUrl: './join-league.dialog.html'
})
export class JoinLeagueDialogComponent {
  // ✅ inyecta primero…
  private readonly fb = inject(FormBuilder);

  // …y luego crea el form sin errores
  readonly form = this.fb.group({
    teamName: ['', [Validators.required, Validators.maxLength(60)]],
    password: ['', [Validators.required]]
  });

  constructor(
    private ref: MatDialogRef<JoinLeagueDialogComponent, JoinLeagueDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: { leagueId: number; leagueName: string }
  ) {}

  submit() {
    if (this.form.invalid) return;
    this.ref.close({
      leagueId: this.data.leagueId,
      password: this.form.value.password!,
      teamName: this.form.value.teamName!
    });
  }
}
