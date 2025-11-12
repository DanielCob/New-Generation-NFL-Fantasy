import { Component, Input, OnChanges, OnInit, SimpleChanges, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { LeagueService } from '../../../core/services/league-service';
import { LeagueDirectoryItem, LeagueSearchResponse, LeagueSearchResult } from '../../../core/models/league-model';
import { JoinLeagueDialogComponent, JoinLeagueDialogResult } from './join-league-dialog.ts/join-league.dialog';

@Component({
  selector: 'league-directory',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './directory.html',
  styleUrls: ['./directory.css']
})
export class LeagueDirectoryComponent implements OnInit, OnChanges {
  // filtros opcionales
  @Input() seasonId?: number;
  @Input() status?: number;
  // control de UI
  @Input() showFilter: boolean = true;
  @Input() showJoinButton: boolean = true;

  private svc = inject(LeagueService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  loading = signal(false); // ✅ Cambiado a false inicialmente
  error = signal<string | null>(null);
  filter = signal('');
  rows = signal<LeagueDirectoryItem[]>([]);
  displayedColumns = ['LeaguePublicID','Name','SeasonLabel','Teams','Available','CreatedAt','actions'];
  hasSearched = signal(false); // ✅ Nueva señal para controlar si se ha hecho búsqueda

  ngOnInit(): void { 
    // ✅ No cargar automáticamente al inicio
    this.rows.set([]);
  }

  ngOnChanges(changes: SimpleChanges): void {
    // ✅ No cargar automáticamente cuando cambien los filtros
    if (this.hasSearched() && ('seasonId' in changes || 'status' in changes)) {
      this.load();
    }
  }

  load(): void {
    const searchTerm = this.filter().trim();
    
    // ✅ Validar que tenga al menos 3 caracteres
    if (!searchTerm || searchTerm.length < 3) {
      this.error.set('Ingresa al menos 3 caracteres para buscar ligas.');
      this.rows.set([]);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.hasSearched.set(true); // ✅ Marcar que se ha hecho búsqueda

    console.log('🚀 BUSCANDO LIGAS CON FILTRO:', searchTerm);
    
    this.svc.searchLeagues({ 
      SearchTerm: searchTerm, // ✅ Enviar el término de búsqueda
      SeasonID: this.seasonId,
      PageNumber: 1,
      PageSize: 50
    }).subscribe({
      next: (res: any) => {
        console.log('🔍 RESPUESTA CRUDA:', res);
        console.log('📊 TIPO DE RESPUESTA:', typeof res);
        console.log('🏷️ PROPIEDADES DE RES:', Object.keys(res));
        
        // Buscar los datos en diferentes estructuras posibles
        let list = [];
        
        if (Array.isArray(res)) {
          // Caso 1: La respuesta es directamente un array
          console.log('✅ La respuesta ES un array directamente');
          list = res;
        } else if (res?.data?.data && Array.isArray(res.data.data)) {
          // Caso 2: Estructura ApiResponse<PagedLeagueSearchResult>
          console.log('✅ Estructura: ApiResponse<PagedLeagueSearchResult>');
          list = res.data.data;
        } else if (res?.data && Array.isArray(res.data)) {
          // Caso 3: Estructura ApiResponse<LeagueSearchResult[]>
          console.log('✅ Estructura: ApiResponse<LeagueSearchResult[]>');
          list = res.data;
        } else if (res?.Data?.data && Array.isArray(res.Data.data)) {
          // Caso 4: Estructura con Data (mayúscula)
          console.log('✅ Estructura: Data con data (mayúscula)');
          list = res.Data.data;
        } else if (res?.Data && Array.isArray(res.Data)) {
          // Caso 5: Estructura con Data (mayúscula) array directo
          console.log('✅ Estructura: Data array directo (mayúscula)');
          list = res.Data;
        } else if (res && Array.isArray(res)) {
          // Caso 6: Array en la raíz
          console.log('✅ Array en la raíz de la respuesta');
          list = res;
        } else {
          console.log('❌ No se pudo encontrar la lista de ligas en la respuesta');
          console.log('🔍 Revisando estructura completa:', JSON.stringify(res, null, 2));
        }
        
        console.log('📋 LISTA ENCONTRADA:', list);
        console.log('🔢 CANTIDAD DE LIGAS:', list.length);

        // Convertir a LeagueDirectoryItem[]
        const directoryItems: LeagueDirectoryItem[] = list.map((item: any) => ({
          LeagueID: item.LeagueID || item.leagueID || 0,
          LeaguePublicID: item.LeaguePublicID || item.leaguePublicID || 0,
          Name: item.Name || item.name || 'Sin nombre',
          SeasonLabel: item.SeasonLabel || item.seasonLabel || 'Sin temporada',
          Status: item.Status || item.status || 0,
          TeamSlots: item.TeamSlots || item.teamSlots || 0,
          TeamsCount: item.TeamsCount || item.teamsCount || 0,
          AvailableSlots: item.AvailableSlots || item.availableSlots || 0,
          CreatedByUserID: item.CreatedByUserID || item.createdByUserID || 0,
          CreatedAt: item.CreatedAt || item.createdAt || new Date().toISOString()
        }));
        
        console.log('🏆 LIGAS CONVERTIDAS:', directoryItems);
        
        this.rows.set(directoryItems);
        
        // ✅ Mostrar mensaje si no hay resultados
        if (directoryItems.length === 0) {
          this.error.set('No se encontraron ligas con ese criterio de búsqueda.');
        }
        
        this.loading.set(false);
      },
      error: (e) => {
        console.error('❌ Error loading leagues:', e);
        console.error('❌ Error completo:', e);
        this.error.set('Error al buscar ligas.');
        this.loading.set(false);
      }
    });
  }

  filteredRows = computed(() => {
    // ✅ Mostrar todas las ligas encontradas (sin filtro adicional)
    return this.rows();
  });

  join(row: LeagueDirectoryItem) {
    const ref = this.dialog.open<JoinLeagueDialogComponent, {leagueId:number, leagueName:string}, JoinLeagueDialogResult>(
      JoinLeagueDialogComponent,
      { width: '420px', data: { leagueId: row.LeagueID, leagueName: row.Name } }
    );

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.svc.joinLeague({
        LeagueID: result.leagueId,
        LeaguePassword: result.password,
        TeamName: result.teamName
      }).subscribe({
        next: (res) => {
          this.snack.open(res.message || 'Joined league successfully', 'OK', { duration: 3000 });
          this.load(); // actualiza AvailableSlots
        },
        error: (e) => {
          console.error(e);
          this.snack.open('Could not join league', 'Dismiss', { duration: 3500 });
        }
      });
    });
  }
}