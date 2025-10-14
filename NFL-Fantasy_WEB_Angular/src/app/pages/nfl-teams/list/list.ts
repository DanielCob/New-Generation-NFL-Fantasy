import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ListNFLTeamsResponse, NFLTeamListItem } from '../../../core/models/nfl-team-model';
import { FiltersPanel, FiltersChange } from '../components/filters-panel/filters-panel';
import { TeamCard } from '../components/team-card/team-card';
import { environment } from '../../../../environments/environment';
import { NFLTeamService } from '../../../core/services/nfl-team-service';

@Component({
  selector: 'app-nfl-teams-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatPaginatorModule, MatProgressSpinnerModule,
    FiltersPanel, TeamCard
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.css']
})
export class NflTeamsListComponent implements OnInit {
  private nfl = inject(NFLTeamService);
  private router = inject(Router);

  teams = signal<NFLTeamListItem[]>([]);
  loading = signal(false);

  totalRecords = signal(0);
  currentPage = signal(1);
  pageSize = signal(environment.nflTeamsPageSize ?? 50);

  // 🔎 filtros
  searchTeam = signal<string | undefined>(undefined);
  filterCity = signal<string | undefined>(undefined);
  filterIsActive = signal<boolean | undefined>(undefined);

  ngOnInit(): void {
    this.loadTeams();
  }

  loadTeams(): void {
    this.loading.set(true);
    this.nfl.list({
      PageNumber: this.currentPage(),
      PageSize: this.pageSize(),
      SearchTeam: this.searchTeam(),      // ✅ aquí va SearchTeam
      FilterCity: this.filterCity(),
      FilterIsActive: this.filterIsActive()
    }).subscribe({
      next: (res) => {
        if ((res as any)?.success && (res as any)?.data) {
          const data = (res as any).data as ListNFLTeamsResponse;
          this.teams.set(data.Teams ?? []);
          this.totalRecords.set(data.TotalRecords ?? 0);
        } else {
          this.teams.set([]);
          this.totalRecords.set(0);
        }
        this.loading.set(false);
      },
      error: () => {
        this.teams.set([]);
        this.totalRecords.set(0);
        this.loading.set(false);
      }
    });
  }

  onFiltersChange(evt: FiltersChange): void {
    this.searchTeam.set(evt.searchTeam);
    this.filterCity.set(evt.city);
    this.filterIsActive.set(evt.isActive);
    this.currentPage.set(1);
    this.loadTeams();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadTeams();
  }

  onViewDetails(teamId: number): void {
    this.router.navigate(['/nfl-teams', teamId]);
  }
  onEditTeam(teamId: number): void {
    this.router.navigate(['/nfl-teams', teamId, 'edit']);
  }
  goToCreate(): void {
    this.router.navigate(['/nfl-teams/create']);
  }
}
