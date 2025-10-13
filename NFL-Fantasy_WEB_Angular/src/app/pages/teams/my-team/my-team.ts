// src/app/pages/teams/my-team/my-team.ts
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { TeamService } from '../../../core/services/team.service';
import { MyTeamResponse, RosterItem, RosterDistribution } from '../../../core/models/team.model';

// ⬇️ importa los hijos (creados abajo)
import { RosterFiltersComponent} from './components/roster-filters/roster-filters';
import { RosterListComponent } from './components/roster-list/roster-list';
import { DistributionPanelComponent } from './components/distribution-panel/distribution-panel';

@Component({
  selector: 'app-my-team',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    RosterFiltersComponent,
    RosterListComponent,
    DistributionPanelComponent,
  ],
  templateUrl: './my-team.html',
  styleUrls: ['./my-team.css'],
})
export class MyTeamComponent implements OnInit {
  private teamSrv = inject(TeamService);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  teamId = signal<number>(0);
  myTeam = signal<MyTeamResponse | null>(null);

  // filtros (controlados por <app-roster-filters>)
  filterPosition = signal<string | undefined>(undefined);
  filterSearch = signal<string | undefined>(undefined);

  // roster filtrado para pasar al hijo
  filteredRoster = computed<RosterItem[]>(() => {
    const list = this.myTeam()?.roster ?? [];
    const pos = (this.filterPosition() || '').trim().toUpperCase();
    const q = (this.filterSearch() || '').trim().toLowerCase();
    return list.filter(r => {
      const okPos = !pos || (r.position || '').toUpperCase() === pos;
      const full = (r.fullName || `${r.firstName ?? ''} ${r.lastName ?? ''}`).toLowerCase();
      const okQ = !q || full.includes(q) || (r.nflTeamName || '').toLowerCase().includes(q);
      return okPos && okQ;
    });
  });

  ngOnInit(): void {
    const idFromRoute = Number(this.route.snapshot.paramMap.get('id'));
    this.teamId.set(Number.isFinite(idFromRoute) && idFromRoute > 0 ? idFromRoute : 1);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.teamSrv.getMyTeam(this.teamId()).subscribe({
      next: res => {
        this.myTeam.set(res.data ?? null);
        localStorage.setItem('xnf.currentTeamId', String(this.teamId()));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  // handler que pide el HTML
  onFiltersChange(e: { position?: string; search?: string }): void {
    this.filterPosition.set(e?.position);
    this.filterSearch.set(e?.search);
  }

  // helper seguro para distribution
  distribution(): RosterDistribution[] {
    return this.myTeam()?.distribution ?? [];
  }
}
