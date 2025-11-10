import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';


import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { ListNFLPlayersRequest, NFLPlayerListItem } from '../../../core/models/nfl-player-model';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { NFLTeamBasic } from '../../../core/models/nfl-team-model';
import { Router } from '@angular/router';

const PLACEHOLDER = 'assets/img/player-placeholder.svg'; // poné cualquier asset
const POSITIONS = ['QB','RB','WR','TE','K','DEF'];


@Component({
  selector: 'app-nfl-player-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatIconModule, MatButtonModule,
    MatInputModule, MatSelectModule, MatChipsModule, MatProgressSpinnerModule,  MatTooltipModule
  ],
  templateUrl: './nfl-player-list.page.html',
  styleUrl: './nfl-player-list.page.css'
})
export class NFLPlayerListPage {
  private svc = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private snack = inject(MatSnackBar);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  readonly pageSize = 50;

    imgOf(r: NFLPlayerListItem): string {
    return r.ThumbnailUrl || (r as any).PhotoUrl || PLACEHOLDER;
  }

  loading = signal(false);
  rows = signal<NFLPlayerListItem[]>([]);
  total = signal(0);
  page = signal(1);

  teams = signal<NFLTeamBasic[]>([]);

  filters = this.fb.group({
    SearchTerm: [''],
    FilterPosition: [''],
    FilterNFLTeamID: [null as number | null],
    FilterIsActive: [null as boolean | null]
  });
  constructor() {
    effect(() => { this.load(); });
  }

  ngOnInit(): void {
    this.teamsSvc.getActive().subscribe({
      next: (r: any) => {
        // ✅ soportar data/Data y setear equipos
        this.teams.set(r?.data ?? r?.Data ?? []);
      },
      error: _ => {
        this.teams.set([]);
      }
    });
  }

  load(): void {
      const f = this.filters.value;
      const req: ListNFLPlayersRequest = {
          PageNumber: this.page(),
          PageSize: this.pageSize,
          SearchTerm: f.SearchTerm || undefined,
          FilterPosition: f.FilterPosition || undefined,
          FilterNFLTeamID: f.FilterNFLTeamID ?? undefined,
          FilterIsActive: typeof f.FilterIsActive === 'boolean' ? f.FilterIsActive : undefined
      };

      this.loading.set(true);
      this.svc.list(req).subscribe({
        next: (r: any) => {
          // ✅ soportar data/Data para el payload
          const data = r?.data ?? r?.Data;
          this.rows.set(data?.Players ?? []);
          this.total.set(data?.TotalRecords ?? 0);
          this.loading.set(false);
        },
        error: err => {
          const msg = err?.error?.message || err?.error?.Message || 'No se pudieron cargar los jugadores';
          this.snack.open(msg, 'OK', { duration: 3000 });
          this.loading.set(false);
        }
      });
    }

  clearFilters(): void {
    this.filters.reset({ SearchTerm:'', FilterPosition:'', FilterNFLTeamID:null, FilterIsActive:null });
    this.page.set(1);
    this.load();
  }

  prev(): void { if (this.page() > 1) { this.page.set(this.page()-1); this.load(); } }
  next(): void {
    const pages = Math.max(1, Math.ceil(this.total() / this.pageSize));
    if (this.page() < pages) { this.page.set(this.page()+1); this.load(); }
  }

  edit(p: NFLPlayerListItem) {
  this.router.navigate(['/admin/nfl-player-edit', p.NFLPlayerID]);
  }

  toggleActive(p: NFLPlayerListItem) {
    const call = p.IsActive ? this.svc.deactivate(p.NFLPlayerID) : this.svc.reactivate(p.NFLPlayerID);
    call.subscribe({
      next: r => {
        this.snack.open(r.message || (p.IsActive ? 'Jugador desactivado' : 'Jugador reactivado'), 'OK', { duration: 2500 });
        this.load();
      },
      error: err => {
        // Alterno: si la API manda mensaje de bloqueo por estar asignado a un equipo activo, se muestra tal cual.
        const msg = err?.error?.message || 'No se pudo cambiar el estado';
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  get positions() { return POSITIONS; }
  teamName(id: number): string {
    const t = this.teams().find(x => x.NFLTeamID === id);
    return t?.TeamName ?? `#${id}`;
  }

  get showingRange() {
    const start = (this.page()-1)*this.pageSize + 1;
    const end = Math.min(this.page()*this.pageSize, this.total());
    return `${start}-${end} / ${this.total()}`;
  }
}
