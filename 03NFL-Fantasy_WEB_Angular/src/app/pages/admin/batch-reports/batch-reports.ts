// 7. batch-reports.ts - COMPONENTE PRINCIPAL
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { BatchReportListItem, ListBatchReportsRequest } from '../../../core/models/nfl-player-model';
import { BatchReportDetailsDialog } from './batch-report-details-dialog/batch-report-details-dialog';

@Component({
  selector: 'app-batch-reports',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './batch-reports.html',
  styleUrl: './batch-reports.css'
})
export class BatchReports {
  private svc = inject(NFLPlayerService);
  private dialog = inject(MatDialog);
  private snack = inject(MatSnackBar);

  readonly pageSize = 50;

  loading = signal(false);
  rows = signal<BatchReportListItem[]>([]);
  total = signal(0);
  page = signal(1);

  ngOnInit(): void {
    this.load();
  }

  // batch-reports.ts - También corregir aquí
  load(): void {
    console.log('📥 [BatchReports] Cargando reportes - Página:', this.page());
    
    const req: ListBatchReportsRequest = {
      PageNumber: this.page(),
      PageSize: this.pageSize
    };

    this.loading.set(true);
    this.svc.getBatchReports(req).subscribe({
      next: (response: any) => {  // ✅ Usar 'any'
        console.log('✅ [BatchReports] Respuesta recibida:', response);
        
        const data = response?.Data || response?.data;  // ✅ Soportar ambos casos
        this.rows.set(data?.Reports || []);
        this.total.set(data?.TotalRecords || 0);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('❌ [BatchReports] Error:', err);
        const msg = err?.error?.message || err?.error?.Message || 'No se pudieron cargar los reportes';
        this.snack.open(msg, 'OK', { duration: 3000 });
        this.loading.set(false);
      }
    });
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

  openJson(report: BatchReportListItem): void {
    console.log('🔗 [BatchReports] Abriendo JSON:', report.ReportUrl);
    window.open(report.ReportUrl, '_blank');
  }

  viewDetails(report: BatchReportListItem): void {
    console.log('👁️ [BatchReports] Abriendo detalles del reporte:', report.BatchReportID);
    
    this.dialog.open(BatchReportDetailsDialog, {
      width: '700px',
      maxWidth: '90vw',
      data: { batchReportId: report.BatchReportID }
    });
  }

  getStatusColor(report: BatchReportListItem): 'primary' | 'warn' | 'accent' {
    if (report.ErrorCount === 0) return 'primary';
    if (report.SuccessCount === 0) return 'warn';
    return 'accent';
  }

  getStatusText(report: BatchReportListItem): string {
    if (report.ErrorCount === 0) return 'SUCCESS';
    if (report.SuccessCount === 0) return 'FAILED';
    return 'PARTIAL';
  }

  get showingRange(): string {
    const start = (this.page() - 1) * this.pageSize + 1;
    const end = Math.min(this.page() * this.pageSize, this.total());
    return `${start}-${end} / ${this.total()}`;
  }
}