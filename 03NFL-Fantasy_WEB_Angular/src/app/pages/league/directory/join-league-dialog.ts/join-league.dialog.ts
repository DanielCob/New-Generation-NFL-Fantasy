import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';

export interface JoinLeagueDialogResult {
  leaguePublicId: number;  // ✅ Cambiado de leagueId a leaguePublicId
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
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    teamName: ['', [Validators.required, Validators.maxLength(60)]],
    password: ['', [Validators.required]]
  });

  constructor(
    private ref: MatDialogRef<JoinLeagueDialogComponent, JoinLeagueDialogResult>,
    @Inject(MAT_DIALOG_DATA) public data: { leaguePublicId: number; leagueName: string }  // ✅ Cambiado
  ) {}

  submit() {
    if (this.form.invalid) return;
    this.ref.close({
      leaguePublicId: this.data.leaguePublicId,  // ✅ Cambiado
      password: this.form.value.password!,
      teamName: this.form.value.teamName!
    });
  }
}