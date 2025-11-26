// src/app/pages/league/teams/teams-dialog/teams-dialog.ts
import { Component, Inject } from '@angular/core';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { TeamsList } from '../teams-list';

@Component({
  selector: 'app-teams-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, TeamsList],
  templateUrl: './teams-dialog.html',
  styleUrls: ['./teams-dialog.css']
})
export class TeamsDialog {
  constructor(
    // ✅ CAMBIO: leaguePublicId
    @Inject(MAT_DIALOG_DATA) public data: { leaguePublicId: number },
    public ref: MatDialogRef<TeamsDialog>
  ) {
    console.log('🔍 [TeamsDialog] Recibido leaguePublicId:', data.leaguePublicId);
  }
}