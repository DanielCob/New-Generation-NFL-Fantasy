import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MyTeamResponse, RosterItem, RosterDistribution } from '../../../core/models/team-model';
import { RosterFiltersComponent } from './components/roster-filters/roster-filters';
import { RosterListComponent } from './components/roster-list/roster-list';
import { DistributionPanelComponent } from './components/distribution-panel/distribution-panel';
import { TeamService } from '../../../core/services/team-service';

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

  // estado UI
  loading = signal(true);
  noTeam = signal(false);
  errorMessage = signal<string | null>(null);

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
    // 1) Intentar id de ruta
    const idFromRoute = Number(this.route.snapshot.paramMap.get('id'));
    if (Number.isFinite(idFromRoute) && idFromRoute > 0) {
      this.teamId.set(idFromRoute);
      this.load();
      return;
    }

    // 2) Intentar último teamId usado
    const stored = Number(localStorage.getItem('xnf.currentTeamId'));
    if (Number.isFinite(stored) && stored > 0) {
      this.teamId.set(stored);
      this.load();
      return;
    }

    // 3) Consultar equipos del usuario; si viene vacío => noTeam
    this.loading.set(true);
    this.teamSrv.listOwnedTeams().subscribe({
      next: (res) => {
        const list = (res as any)?.data ?? (res as any)?.Data ?? [];
        const first = list?.[0] as any;
        // ser tolerantes con el naming que venga del backend
        const id = first?.teamID ?? first?.TeamID ?? first?.id ?? first?.Id;
        if (Number.isFinite(id) && id > 0) {
          this.teamId.set(Number(id));
          this.load();
        } else {
          this.noTeam.set(true);
          this.loading.set(false);
        }
      },
      error: () => {
        // si falla el listado, mostramos "sin equipo" en lugar de romper la vista
        this.noTeam.set(true);
        this.loading.set(false);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.noTeam.set(false);

    this.teamSrv.getMyTeam(this.teamId()).subscribe({
      next: res => {
        this.myTeam.set(res.data ?? null);
        // persistimos el teamId para futuras cargas
        if ((res as any)?.data?.teamID ?? (res as any)?.Data?.TeamID) {
          const tid = (res as any).data?.teamID ?? (res as any).Data?.TeamID;
          localStorage.setItem('xnf.currentTeamId', String(tid));
        } else {
          localStorage.setItem('xnf.currentTeamId', String(this.teamId()));
        }
        this.loading.set(false);
      },
      error: (err) => {
        if (err?.status === 404) {
          // Caso esperado: el usuario no tiene equipo
          this.myTeam.set(null);
          this.noTeam.set(true);
        } else {
          this.errorMessage.set(this.normalizeApiError(err));
        }
        this.loading.set(false);
      },
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

  private normalizeApiError(err: any): string {
    return (
      err?.error?.message ||
      err?.error?.Message ||
      err?.message ||
      'Ocurrió un error al cargar tu equipo.'
    );
  }
}
