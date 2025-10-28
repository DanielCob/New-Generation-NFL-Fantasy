import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

interface DialogData {
  seasonLabel: string;
  weeks: { WeekNumber: number; StartDate: string; EndDate: string }[];
}

@Component({
  selector: 'app-season-weeks-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatListModule, MatButtonModule, MatIconModule],
  templateUrl: './season-weeks-dialog.html'
})
export class SeasonWeeksDialog {
  private ref = inject(MatDialogRef<SeasonWeeksDialog>);
  data: DialogData = inject(MAT_DIALOG_DATA);

  close(): void { this.ref.close(); }
}
