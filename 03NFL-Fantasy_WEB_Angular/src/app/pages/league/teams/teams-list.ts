import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { LeagueService } from '../../../core/services/league-service';
import { LeagueTeam } from '../../../core/models/league-model';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatProgressSpinnerModule, MatButtonModule, RouterLink],
  templateUrl: './teams-list.html',
  styleUrl: './teams-list.css'
})
export class TeamsList implements OnInit {
  @Input() leagueId!: number;

  private leagues = inject(LeagueService);

  loading = signal(false);
  error   = signal<string | null>(null);
  rows    = signal<LeagueTeam[]>([]);

  ngOnInit(): void {
    if (!this.leagueId) { this.error.set('LeagueID inválido'); return; }
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true); this.error.set(null);
    this.leagues.getTeams(this.leagueId).subscribe({
      next: (r: any) => {
        const list: LeagueTeam[] = (r?.data ?? r?.Data ?? r) || [];
        this.rows.set(list);
        this.loading.set(false);
      },
      error: () => { this.error.set('No se pudieron cargar los equipos'); this.loading.set(false); }
    });
  }
}
