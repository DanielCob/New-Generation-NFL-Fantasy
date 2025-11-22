// 7. player-news-list-dialog.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import { PlayerNewsItem, ListPlayerNewsRequest, NFLPlayerListItem } from '../../../../core/models/nfl-player-model';

@Component({
  selector: 'app-player-news-list-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatChipsModule,
    MatCardModule
  ],
  templateUrl: './player-news-list-dialog.html',
  styleUrl: './player-news-list-dialog.css'
})
export class PlayerNewsListDialog {
  private dialogRef = inject(MatDialogRef<PlayerNewsListDialog>);
  private data = inject(MAT_DIALOG_DATA);
  private svc = inject(NFLPlayerService);
  private snack = inject(MatSnackBar);

  player: NFLPlayerListItem = this.data.player;
  
  loading = signal(true);
  news = signal<PlayerNewsItem[]>([]);
  total = signal(0);
  error = signal<string | null>(null);

  ngOnInit(): void {
    console.log('📰 [NewsListDialog] Cargando noticias del jugador:', this.player.FullName);
    this.loadNews();
  }

  loadNews(): void {
    const req: ListPlayerNewsRequest = {
      PageNumber: 1,
      PageSize: 50
    };

    this.loading.set(true);
    this.svc.getPlayerNews(this.player.NFLPlayerID, req).subscribe({
      next: (response: any) => {
        console.log('✅ [NewsListDialog] Noticias recibidas:', response);
        const data = response?.Data || response?.data;
        this.news.set(data?.News || []);
        this.total.set(data?.TotalRecords || 0);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ [NewsListDialog] Error:', err);
        this.error.set(err?.error?.message || err?.error?.Message || 'Error cargando noticias');
        this.loading.set(false);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  getDesignationColor(designation?: string): string {
    if (!designation) return 'grey';
    const colors: Record<string, string> = {
      'O': 'red',
      'D': 'orange',
      'Q': 'yellow',
      'P': 'lightgreen',
      'FP': 'green',
      'IR': 'purple',
      'PUP': 'blue',
      'SUS': 'darkred'
    };
    return colors[designation] || 'grey';
  }

  getDesignationLabel(designation?: string): string {
    if (!designation) return 'N/A';
    const labels: Record<string, string> = {
      'O': 'Out',
      'D': 'Doubtful',
      'Q': 'Questionable',
      'P': 'Probable',
      'FP': 'Full Practice',
      'IR': 'Injured Reserve',
      'PUP': 'PUP',
      'SUS': 'Suspended'
    };
    return labels[designation] || designation;
  }
}