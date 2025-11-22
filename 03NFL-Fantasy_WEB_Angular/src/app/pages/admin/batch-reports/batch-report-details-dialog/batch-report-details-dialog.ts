// 4. batch-report-details-dialog.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import { BatchReportDetails } from '../../../../core/models/nfl-player-model';

@Component({
  selector: 'app-batch-report-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatCardModule
  ],
  templateUrl: './batch-report-details-dialog.html',
  styleUrl: './batch-report-details-dialog.css'
})
export class BatchReportDetailsDialog {
  private dialogRef = inject(MatDialogRef<BatchReportDetailsDialog>);
  private data = inject(MAT_DIALOG_DATA);
  private svc = inject(NFLPlayerService);

  loading = signal(true);
  details = signal<BatchReportDetails | null>(null);
  error = signal<string | null>(null);

  // batch-report-details-dialog.ts - CORREGIR
  ngOnInit(): void {
    console.log('🔍 [Dialog] Cargando detalles del reporte:', this.data.batchReportId);
    
    this.svc.getBatchReportDetails(this.data.batchReportId).subscribe({
      next: (response: any) => {  // ✅ Usar 'any' para evitar el problema del tipo
        console.log('✅ [Dialog] Detalles recibidos:', response);
        this.details.set(response.Data || response.data);  // ✅ Soportar ambos casos
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ [Dialog] Error cargando detalles:', err);
        this.error.set(err?.error?.message || err?.error?.Message || 'Error cargando detalles');
        this.loading.set(false);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  openJson(): void {
    const url = this.details()?.ReportUrl;
    if (url) {
      window.open(url, '_blank');
    }
  }

  getStatusColor(): string {
    const d = this.details();
    if (!d) return 'grey';
    if (d.ErrorCount === 0) return 'green';
    if (d.SuccessCount === 0) return 'red';
    return 'orange';
  }

  getStatusText(): string {
    const d = this.details();
    if (!d) return 'Unknown';
    if (d.ErrorCount === 0) return 'SUCCESS';
    if (d.SuccessCount === 0) return 'FAILED';
    return 'PARTIAL';
  }
}