// 4. nfl-player-news.ts - COMPONENTE PRINCIPAL
import { Component, inject, signal } from '@angular/core';
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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { ListNFLPlayersRequest, NFLPlayerListItem } from '../../../core/models/nfl-player-model';
import { NFLTeamBasic } from '../../../core/models/nfl-team-model';
import { PlayerNewsListDialog } from './player-news-list-dialog/player-news-list-dialog';
import { CreatePlayerNewsDialog } from './create-player-news-dialog/create-player-news-dialog';
import { DeletePlayerNewsDialog } from './delete-player-news-dialog/delete-player-news-dialog';

const POSITIONS = ['QB', 'RB', 'WR', 'TE', 'K', 'DEF'];

@Component({
  selector: 'app-nfl-player-news',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './nfl-player-news.html',
  styleUrl: './nfl-player-news.css'
})
export class NflPlayerNews {
  private svc = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  readonly pageSize = 50;

  loading = signal(false);
  rows = signal<NFLPlayerListItem[]>([]);
  total = signal(0);
  page = signal(1);
  teams = signal<NFLTeamBasic[]>([]);

  filters = this.fb.group({
    SearchTerm: [''],
    FilterPosition: [''],
    FilterNFLTeamID: [null as number | null],
    FilterIsActive: [true as boolean | null]
  });

  ngOnInit(): void {
    this.loadTeams();
    this.load();
  }

  loadTeams(): void {
    this.teamsSvc.getActive().subscribe({
      next: (r: any) => {
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
    this.filters.reset({ SearchTerm: '', FilterPosition: '', FilterNFLTeamID: null, FilterIsActive: true });
    this.page.set(1);
    this.load();
  }

  prev(): void {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
      this.load();
    }
  }

  next(): void {
    const pages = Math.max(1, Math.ceil(this.total() / this.pageSize));
    if (this.page() < pages) {
      this.page.set(this.page() + 1);
      this.load();
    }
  }

  viewNews(player: NFLPlayerListItem): void {
    console.log('📰 [News] Ver noticias del jugador:', player.FullName);
    
    this.dialog.open(PlayerNewsListDialog, {
      width: '1250px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: { player }
    });
  }

  createNews(player: NFLPlayerListItem): void {
    console.log('➕ [News] Crear noticia para:', player.FullName);
    
    const dialogRef = this.dialog.open(CreatePlayerNewsDialog, {
      width: '600px',
      maxWidth: '95vw',
      data: { player }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.created) {
        this.snack.open('Noticia creada exitosamente', 'OK', { duration: 3000 });
      }
    });
  }

  deleteNews(player: NFLPlayerListItem): void {
    console.log('🗑️ [News] Eliminar noticia de:', player.FullName);
    
    const dialogRef = this.dialog.open(DeletePlayerNewsDialog, {
      width: '750px',
      maxWidth: '95vw',
      data: { player }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.deleted) {
        this.snack.open('Noticia eliminada exitosamente', 'OK', { duration: 3000 });
      }
    });
  }

  get positions() {
    return POSITIONS;
  }

  teamName(id: number): string {
    const t = this.teams().find(x => x.NFLTeamID === id);
    return t?.TeamName ?? `#${id}`;
  }

  get showingRange(): string {
    const start = (this.page() - 1) * this.pageSize + 1;
    const end = Math.min(this.page() * this.pageSize, this.total());
    return `${start}-${end} / ${this.total()}`;
  }
}