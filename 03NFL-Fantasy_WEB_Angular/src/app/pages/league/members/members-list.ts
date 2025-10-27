import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeagueService } from '../../../core/services/league-service';
import { LeagueMember } from '../../../core/models/league-model';

@Component({
  selector: 'app-members-list',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatChipsModule, MatProgressSpinnerModule],
  templateUrl: './members-list.html',
  styleUrl: './members-list.css'
})
export class MembersList implements OnInit {
  @Input() leagueId!: number;

  private leagues = inject(LeagueService);

  loading = signal(false);
  error   = signal<string | null>(null);
  rows    = signal<LeagueMember[]>([]);

  ngOnInit(): void {
    if (!this.leagueId) { this.error.set('LeagueID inválido'); return; }
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true); this.error.set(null);
    this.leagues.getMembers(this.leagueId).subscribe({
      next: (r: any) => {
        const list: LeagueMember[] = (r?.data ?? r?.Data ?? r) || [];
        this.rows.set(list);
        this.loading.set(false);
      },
      error: () => { this.error.set('No se pudieron cargar los miembros'); this.loading.set(false); }
    });
  }
}
