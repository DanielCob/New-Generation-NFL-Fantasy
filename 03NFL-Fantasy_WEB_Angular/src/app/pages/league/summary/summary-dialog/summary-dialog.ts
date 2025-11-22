// src/app/pages/league/summary/summary-dialog/summary-dialog.ts
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { Summary } from '../summary';

@Component({
  standalone: true,
  selector: 'app-summary-dialog',
  imports: [MatDialogModule, Summary],
  template: `
    <h2 mat-dialog-title>League Summary</h2>
    <mat-dialog-content>
      <app-summary [leaguePublicId]="data.leaguePublicId"></app-summary>
    </mat-dialog-content>
  `
})
export class SummaryDialog {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { leaguePublicId: number }) {
    console.log('🔍 [SummaryDialog] Recibido leaguePublicId:', data.leaguePublicId);
  }
}