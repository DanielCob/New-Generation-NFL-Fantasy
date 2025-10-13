import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NflTeamService } from '../../../core/services/nfl-team.service';
import { ListNFLTeamsResponse, NFLTeamListItem } from '../../../core/models/nfl-team.model';
import { FiltersPanel } from './components/filters-panel/filters-panel';
import { TeamCard } from './components/team-card/team-card';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-nfl-teams-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    FiltersPanel,
    TeamCard
  ],
  templateUrl: './list.html',
  styleUrls: ['./list.css']
})
export class NflTeamsListComponent implements OnInit {
  private nflTeamService = inject(NflTeamService);
  private router = inject(Router);

  teams = signal<NFLTeamListItem[]>([]);
  loading = signal(false);

  // Paginación
  totalRecords = signal(0);
  currentPage = signal(1);
  pageSize = signal(environment.nflTeamsPageSize);

  // Filtros
  searchTerm = signal<string | undefined>(undefined);
  filterCity = signal<string | undefined>(undefined);
  filterIsActive = signal<boolean | undefined>(undefined);

  ngOnInit(): void {
    this.loadTeams();
  }

  loadTeams(): void {
    this.loading.set(true);

    this.nflTeamService.list({
      PageNumber: this.currentPage(),
      PageSize: this.pageSize(),
      SearchTerm: this.searchTerm(),
      FilterCity: this.filterCity(),
      FilterIsActive: this.filterIsActive()
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.teams.set(response.data.Teams);
          this.totalRecords.set(response.data.TotalRecords);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadTeams();
  }

  onFiltersChange(filters: { search?: string; city?: string; isActive?: boolean }): void {
    this.searchTerm.set(filters.search);
    this.filterCity.set(filters.city);
    this.filterIsActive.set(filters.isActive);
    this.currentPage.set(1); // Reset a primera página
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
