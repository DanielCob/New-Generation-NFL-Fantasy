import { Component, Inject } from '@angular/core';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { EditConfigForm } from '../edit-config';

@Component({
  selector: 'app-edit-config-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, EditConfigForm],
  templateUrl: './edit-config-dialog.html',
  styleUrls: ['./edit-config-dialog.css']
})
export class EditConfigDialog {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { leagueId: number },
    public ref: MatDialogRef<EditConfigDialog>
  ) {}
}
