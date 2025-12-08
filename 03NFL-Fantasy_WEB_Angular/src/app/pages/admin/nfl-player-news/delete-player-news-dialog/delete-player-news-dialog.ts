// 13. delete-player-news-dialog.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import {
  PlayerNewsItem,
  ListPlayerNewsRequest,
  NFLPlayerListItem
} from '../../../../core/models/nfl-player-model';

@Component({
  selector: 'app-delete-player-news-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatCardModule
  ],
  templateUrl: './delete-player-news-dialog.html',
  styleUrl: './delete-player-news-dialog.css'
})
export class DeletePlayerNewsDialog {
  private dialogRef = inject(MatDialogRef<DeletePlayerNewsDialog>);
  private data = inject(MAT_DIALOG_DATA);
  private svc = inject(NFLPlayerService);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  player: NFLPlayerListItem = this.data.player;

  loadingNews = signal(true);
  deleting = signal(false);
  news = signal<PlayerNewsItem[]>([]);
  error = signal<string | null>(null);

  form = this.fb.group({
    selectedNewsId: [null as number | null, Validators.required]
  });

  ngOnInit(): void {
    console.log('🗑️ [DeleteNewsDialog] Cargando noticias para eliminar:', this.player.FullName);
    this.loadNews();
  }

  loadNews(): void {
    const req: ListPlayerNewsRequest = {
      PageNumber: 1,
      PageSize: 20
    };

    this.loadingNews.set(true);
    this.svc.getPlayerNews(this.player.NFLPlayerID, req).subscribe({
      next: (response: any) => {
        console.log('✅ [DeleteNewsDialog] Noticias recibidas:', response);
        const data = response?.Data || response?.data;
        this.news.set(data?.News || []);
        this.loadingNews.set(false);
      },
      error: (err) => {
        console.error('❌ [DeleteNewsDialog] Error:', err);
        const msg = 'Error cargando noticias';
        this.error.set(msg);
        this.snack.open(msg, 'OK', { duration: 3000 });
        this.loadingNews.set(false);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  deleteNews(): void {
    if (this.deleting()) {
      return;
    }

    if (this.form.invalid) {
      this.snack.open('Por favor selecciona una noticia para eliminar', 'OK', {
        duration: 3000
      });
      return;
    }

    const newsId = this.form.value.selectedNewsId!;
    const selectedNews = this.news().find(n => n.NewsID === newsId);

    if (!selectedNews) {
      this.snack.open('Noticia no encontrada', 'OK', { duration: 3000 });
      return;
    }

    if (
      !confirm(
        `¿Estás seguro de eliminar la noticia ID #${newsId}?\n\n"${selectedNews.NewsText.substring(
          0,
          100
        )}..."\n\nEsta acción no se puede deshacer.`
      )
    ) {
      return;
    }

    console.log('🗑️ [DeleteNewsDialog] Eliminando noticia ID:', newsId);
    this.deleting.set(true);

    this.svc.deletePlayerNews(newsId).subscribe({
      next: (response: any) => {
        console.log('✅ [DeleteNewsDialog] Noticia eliminada:', response);
        this.snack.open(
          response.Message || 'Noticia eliminada exitosamente',
          'OK',
          { duration: 3000 }
        );
        this.dialogRef.close({ deleted: true, newsId });
      },
      error: (err) => {
        console.error('❌ [DeleteNewsDialog] Error:', err);
        const msg =
          err?.error?.message ||
          err?.error?.Message ||
          'Error eliminando noticia';
        this.snack.open(msg, 'OK', { duration: 4000 });
        this.deleting.set(false);
      }
    });
  }

  getDesignationColor(designation?: string): string {
    if (!designation) return '#ccc';
    const colors: Record<string, string> = {
      O: '#f44336',
      D: '#ff9800',
      Q: '#ffeb3b',
      P: '#8bc34a',
      FP: '#4caf50',
      IR: '#9c27b0',
      PUP: '#2196f3',
      SUS: '#b71c1c'
    };
    return colors[designation] || '#ccc';
  }

  getDesignationLabel(designation?: string): string {
    if (!designation) return 'N/A';
    const labels: Record<string, string> = {
      O: 'Out',
      D: 'Doubtful',
      Q: 'Questionable',
      P: 'Probable',
      FP: 'Full Practice',
      IR: 'Injured Reserve',
      PUP: 'PUP',
      SUS: 'Suspended'
    };
    return labels[designation] || designation;
  }

  get selectedNewsId(): number | null {
    return this.form.value.selectedNewsId ?? null;
  }

  formatDate(value: string): string {
    const d = new Date(value);
    if (isNaN(d.getTime())) return value;
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }
}
