// src/app/pages/league/edit-config/edit-config-dialog/edit-config-dialog.ts
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
    // ✅ CAMBIO: leaguePublicId
    @Inject(MAT_DIALOG_DATA) public data: { leaguePublicId: number },
    public ref: MatDialogRef<EditConfigDialog>
  ) {
    console.log('🔍 [EditConfigDialog] Recibido leaguePublicId:', data.leaguePublicId);
  }
}