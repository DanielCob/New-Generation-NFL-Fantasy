import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { AddPlayerToRosterDTO } from '../../../../core/models/team.model';

@Component({
  selector: 'app-add-player-dialog',
  standalone: true,
  imports: [
    CommonModule, MatDialogModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  template: `
  <h2 mat-dialog-title>Add Player</h2>
  <div mat-dialog-content [formGroup]="form" class="grid">
    <mat-form-field appearance="outline">
      <mat-label>Player ID</mat-label>
      <input matInput formControlName="playerID" type="number" />
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Position</mat-label>
      <mat-select formControlName="position">
        <mat-option value="QB">QB</mat-option>
        <mat-option value="RB">RB</mat-option>
        <mat-option value="WR">WR</mat-option>
        <mat-option value="TE">TE</mat-option>
        <mat-option value="K">K</mat-option>
        <mat-option value="DEF">DEF</mat-option>
      </mat-select>
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Acquisition</mat-label>
      <mat-select formControlName="acquisitionType">
        <mat-option value="Draft">Draft</mat-option>
        <mat-option value="Waivers">Waivers</mat-option>
        <mat-option value="FreeAgent">FreeAgent</mat-option>
        <mat-option value="Trade">Trade</mat-option>
      </mat-select>
    </mat-form-field>
  </div>

  <div mat-dialog-actions align="end">
    <button mat-stroked-button (click)="close()">Cancel</button>
    <button mat-flat-button color="primary" (click)="confirm()" [disabled]="form.invalid">Add</button>
  </div>
  `,
  styles: [`.grid{display:grid;gap:12px;grid-template-columns:1fr 1fr}@media(max-width:720px){.grid{grid-template-columns:1fr}}`]
})
export class AddPlayerDialogComponent {
  private ref = inject(MatDialogRef<AddPlayerDialogComponent>);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    playerID: [null, [ Validators.required ]],
    position: ['',  [ Validators.required ]],
    acquisitionType: ['FreeAgent']
  });

  close() { this.ref.close(); }
  confirm() {
    const v = this.form.value;
    const dto: AddPlayerToRosterDTO = {
      playerID: Number(v.playerID),
      position: String(v.position),
      acquisitionType: v.acquisitionType as any
    };
    this.ref.close(dto);
  }
}
