import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RosterItem } from '../../../../../core/models/team-model';

@Component({
  selector: 'app-roster-list',
  standalone: true,
  imports: [CommonModule],
  template: `
  <table class="mat-elevation-z1 full-width" *ngIf="roster?.length; else emptyTpl">
    <thead>
      <tr><th>Player</th><th>Pos</th><th>Team</th></tr>
    </thead>
    <tbody>
      @for (r of roster; track r.playerID) {
        <tr>
          <td>{{ r.fullName }}</td>
          <td>{{ r.position }}</td>
          <td>{{ r.nflTeamName || 'FA' }}</td>
        </tr>
      }
    </tbody>
  </table>
  <ng-template #emptyTpl>
    <div class="empty"><span>No players in roster.</span></div>
  </ng-template>
  `,
  styles: [`.full-width{width:100%}.empty{padding:12px 0;color:#666}`]
})
export class RosterListComponent {
  @Input() roster: RosterItem[] = [];
}