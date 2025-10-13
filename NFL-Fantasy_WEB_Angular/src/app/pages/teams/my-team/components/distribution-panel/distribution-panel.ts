// src/app/pages/teams/my-team/subcomponents/distribution-panel/distribution-panel.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RosterDistribution } from '../../../../../core/models/team.model';

@Component({
  selector: 'app-distribution-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
  <div class="dist" *ngIf="distribution?.length; else none">
    @for (d of distribution; track d.position) {
      <div class="chip">{{ d.position }}: {{ d.count }} ({{ d.percent | number : '1.0-0' }}%)</div>
    }
  </div>
  <ng-template #none><div class="muted">No distribution data.</div></ng-template>
  `,
  styles: [`.dist{display:flex;gap:8px;flex-wrap:wrap}.chip{padding:6px 10px;border-radius:999px;background:#eee}.muted{color:#666}`]
})
export class DistributionPanelComponent {
  @Input() distribution: RosterDistribution[] = [];
}
