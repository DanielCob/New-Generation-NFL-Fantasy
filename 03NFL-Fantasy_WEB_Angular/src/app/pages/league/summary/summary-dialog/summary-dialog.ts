import { Component, Inject } from '@angular/core';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { Summary } from '../summary';

@Component({
  selector: 'app-summary-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, Summary],
  templateUrl: './summary-dialog.html',
  styleUrls: ['./summary-dialog.css']
})
export class SummaryDialog {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { leagueId: number },
    public ref: MatDialogRef<SummaryDialog>
  ) {}
}
