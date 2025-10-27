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
    @Inject(MAT_DIALOG_DATA) public data: { leagueId: number },
    public ref: MatDialogRef<TeamsDialog>
  ) {}
}
