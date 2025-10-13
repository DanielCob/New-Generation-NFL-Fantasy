// src/app/shared/components/set-team-id-dialog/set-team-id-dialog.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  standalone: true,
  selector: 'app-set-team-id-dialog',
  imports: [CommonModule, MatDialogModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Set current Team ID</h2>
    <div mat-dialog-content [formGroup]="form">
      <mat-form-field appearance="outline" style="width: 100%">
        <mat-label>Team ID</mat-label>
        <input matInput formControlName="teamId" type="number" />
      </mat-form-field>
    </div>
    <div mat-dialog-actions align="end">
      <button mat-stroked-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid">Save</button>
    </div>
  `
})
export class SetTeamIdDialog {
  private ref = inject(MatDialogRef<SetTeamIdDialog>);
  private fb = inject(FormBuilder);
  form = this.fb.group({ teamId: [null, [Validators.required]] });

  close() { this.ref.close(); }
  save()  { this.ref.close(Number(this.form.value.teamId)); }
}
