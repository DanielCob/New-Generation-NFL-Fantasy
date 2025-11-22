// src/app/pages/league/summary/summary.ts
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { LeagueService } from '../../../core/services/league-service';
import { LeagueSummary } from '../../../core/models/league-model';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule, MatChipsModule, MatCardModule, MatDividerModule,
    MatProgressSpinnerModule, MatListModule, MatButtonModule, MatTooltipModule
  ],
  templateUrl: './summary.html',
  styleUrl: './summary.css'
})
export class Summary implements OnInit {
  private leagues = inject(LeagueService);
  private route = inject(ActivatedRoute);
  private snack = inject(MatSnackBar);

  /** ✅ Ahora recibe LeaguePublicID */
  @Input() leaguePublicId?: number;

  loading = signal(false);
  error = signal<string | null>(null);
  data = signal<LeagueSummary | null>(null);

  ngOnInit(): void {
    // ✅ El ID de la ruta ahora es LeaguePublicID
    const idFromRoute = Number(this.route.snapshot.paramMap.get('id'));
    
    // ✅ Usar LeaguePublicID del input o de la ruta
    const id = this.leaguePublicId && this.leaguePublicId > 0
      ? this.leaguePublicId
      : Number.isFinite(idFromRoute) && idFromRoute > 0
        ? idFromRoute
        : 0;

    console.log('🎯 [Summary] LeaguePublicID a cargar:', id);

    if (!id) {
      this.error.set('Seleccioná una liga primero');
      this.snack.open('Seleccioná una liga primero', 'OK', { duration: 3000 });
      return;
    }

    this.fetch(id);
  }

  private fetch(leaguePublicId: number): void {
    if (this.loading()) return;
    
    console.log('📡 [Summary] Cargando summary con LeaguePublicID:', leaguePublicId);
    
    this.loading.set(true);
    this.error.set(null);

    // ✅ El servicio ahora recibe LeaguePublicID
    this.leagues.getSummary(leaguePublicId).subscribe({
      next: (raw: any) => {
        console.log('✅ [Summary] Respuesta recibida:', raw);
        
        const success = raw?.success ?? raw?.Success ?? (raw?.data ?? raw?.Data ? true : false);
        const message = raw?.message ?? raw?.Message ?? '';
        const data    = raw?.data    ?? raw?.Data    ?? raw;

        if (!success || !data) {
          this.error.set(message || 'No se pudo cargar el resumen');
          this.data.set(null);
        } else {
          this.data.set(data);
        }
        this.loading.set(false);
      },
      error: (e) => {
        console.error('❌ [Summary] Error:', e);
        const msg = e?.error?.message ?? e?.error?.Message ?? 'No se pudo cargar el resumen';
        this.error.set(msg);
        this.data.set(null);
        this.loading.set(false);
      }
    });
  }

  statusLabel(s: number): string {
    switch (s) {
      case 0: return 'Drafting';
      case 1: return 'Active';
      case 2: return 'Completed';
      case 3: return 'Archived';
      default: return `Status ${s}`;
    }
  }

  statusColor(s: number): 'primary'|'accent'|'warn' {
    switch (s) {
      case 0: return 'accent';
      case 1: return 'primary';
      case 2: return 'primary';
      case 3: return 'warn';
      default: return 'accent';
    }
  }
}