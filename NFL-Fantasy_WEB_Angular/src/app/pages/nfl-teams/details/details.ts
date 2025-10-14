import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { NFLTeamDetails } from '../../../core/models/nfl-team-model';
import { TableSimple, TableSimpleColumn } from '../../../shared/components/table-simple/table-simple';

@Component({
  selector: 'app-details',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TableSimple
  ],
  templateUrl: './details.html',
  styleUrl: './details.css'
})
export class Details implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(NFLTeamService);

  loading = signal<boolean>(true);
  notFound = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  teamId = signal<number>(0);
  team = signal<NFLTeamDetails | null>(null);

  readonly playerColumns: TableSimpleColumn[] = [
    {
      key: 'Player',
      label: 'Player',
      formatter: (row: any) => {
        const full = (row?.FullName || '').trim();
        const names = `${row?.FirstName ?? ''} ${row?.LastName ?? ''}`.trim();
        return full || names || `#${row?.PlayerID ?? ''}`;
      }
    },
    { key: 'Position', label: 'Pos' },
    { key: 'InjuryStatus', label: 'Injury' },
    { key: 'IsActive', label: 'Active', format: 'yesno' }
  ];

  // texto para el chip de estado
  statusLabel = computed(() => this.team()?.IsActive ? 'Active' : 'Inactive');

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }
    this.teamId.set(id);
    this.load();
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.notFound.set(false);

    this.api.getDetails(this.teamId()).subscribe({
      next: (res: any) => {
        // Tolerar Data/data y Success/success
        const ok = (res?.Success ?? res?.success) === true;
        const data = res?.Data ?? res?.data ?? null;

        if (!ok || !data) {
          // Algunos backends devuelven 200 con Success=false cuando no existe
          this.team.set(null);
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }

        this.team.set(data as NFLTeamDetails);
        this.loading.set(false);
      },
      error: (err) => {
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.errorMessage.set(
            err?.error?.Message ||
            err?.error?.message ||
            err?.message ||
            'Ocurrió un error al cargar los detalles del equipo.'
          );
        }
        this.loading.set(false);
      }
    });
  }
}