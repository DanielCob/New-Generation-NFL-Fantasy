// my-team.component.ts
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

  loading = signal(true);
  noTeam = signal(false);
  errorMessage = signal<string | null>(null);

  teamId = signal<number>(0);
  myTeam = signal<MyTeamResponse | null>(null);

  filterPosition = signal<string | undefined>(undefined);
  filterSearch = signal<string | undefined>(undefined);

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
    if (Number.isFinite(idFromRoute) && idFromRoute > 0) {
      this.teamId.set(idFromRoute);
      this.load();
      return;
    }

    const stored = Number(localStorage.getItem('xnf.currentTeamId'));
    if (Number.isFinite(stored) && stored > 0) {
      this.teamId.set(stored);
      this.load();
      return;
    }

    this.loading.set(true);
    this.teamSrv.listOwnedTeams().subscribe({
      next: (res) => {
        const list = (res as any)?.data ?? (res as any)?.Data ?? [];
        const first = list?.[0] as any;
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
        console.log('🔍 Respuesta completa de getMyTeam:', res);
        
        // Acceder a Data con ambos casos
        const apiData = (res as any)?.Data ?? (res as any)?.data;
        console.log('🔍 apiData:', apiData);
        
        if (apiData) {
          // Mapear los datos de PascalCase a camelCase
          const mappedTeam: MyTeamResponse = {
            teamId: apiData.TeamID ?? apiData.teamId ?? 0,
            teamName: apiData.TeamName ?? apiData.teamName ?? '',
            leagueId: apiData.LeagueID ?? apiData.leagueId ?? 0,
            leagueName: apiData.LeagueName ?? apiData.leagueName ?? '',
            teamImageUrl: apiData.TeamImageUrl ?? apiData.teamImageUrl,
            thumbnailUrl: apiData.ThumbnailUrl ?? apiData.thumbnailUrl,
            roster: this.mapRoster(apiData.Roster ?? apiData.roster ?? []),
            distribution: this.mapDistribution(apiData.Distribution ?? apiData.distribution ?? [])
          };
          
          console.log('🔍 mappedTeam:', mappedTeam);
          this.myTeam.set(mappedTeam);
          
          // Guardar teamId en localStorage
          localStorage.setItem('xnf.currentTeamId', String(mappedTeam.teamId));
        } else {
          this.noTeam.set(true);
        }
        
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ Error en getMyTeam:', err);
        if (err?.status === 404) {
          this.myTeam.set(null);
          this.noTeam.set(true);
        } else {
          this.errorMessage.set(this.normalizeApiError(err));
        }
        this.loading.set(false);
      },
    });
  }

  onFiltersChange(e: { position?: string; search?: string }): void {
    this.filterPosition.set(e?.position);
    this.filterSearch.set(e?.search);
  }

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

  private mapRoster(roster: any[]): RosterItem[] {
    return roster.map(r => ({
      rosterID: r.RosterID ?? r.rosterID ?? 0,
      playerID: r.PlayerID ?? r.playerID ?? 0,
      fullName: r.FullName ?? r.fullName ?? '',
      firstName: r.FirstName ?? r.firstName,
      lastName: r.LastName ?? r.lastName,
      position: r.Position ?? r.position ?? '',
      nflTeamName: r.NFLTeamName ?? r.nflTeamName,
      photoUrl: r.PhotoUrl ?? r.photoUrl,
      photoThumbnailUrl: r.PhotoThumbnailUrl ?? r.photoThumbnailUrl,
      acquisitionType: r.AcquisitionType ?? r.acquisitionType,
      acquiredAt: r.AcquiredAt ?? r.acquiredAt ?? r.AcquisitionDate ?? r.acquisitionDate,
      isStarter: r.IsStarter ?? r.isStarter,
      isIR: r.IsIR ?? r.isIR
    }));
  }

  private mapDistribution(distribution: any[]): RosterDistribution[] {
    return distribution.map(d => ({
      position: d.Position ?? d.position ?? d.AcquisitionType ?? d.acquisitionType ?? '',
      count: d.Count ?? d.count ?? d.PlayerCount ?? d.playerCount ?? 0,
      percent: d.Percent ?? d.percent ?? d.Percentage ?? d.percentage ?? 0
    }));
  }
}