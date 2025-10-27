import { Component, Inject } from '@angular/core';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MembersList } from '../members-list';

@Component({
  selector: 'app-members-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MembersList],
  templateUrl: './members-dialog.html',
  styleUrls: ['./members-dialog.css']
})
export class MembersDialog {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { leagueId: number },
    public ref: MatDialogRef<MembersDialog>
  ) {}
}
