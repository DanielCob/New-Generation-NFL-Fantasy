// src/app/pages/league/teams/teams-list.ts
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { LeagueService } from '../../../core/services/league-service';
import { LeagueTeam } from '../../../core/models/league-model';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatProgressSpinnerModule, MatButtonModule],
  templateUrl: './teams-list.html',
  styleUrl: './teams-list.css'
})
export class TeamsList implements OnInit {
  // ✅ CAMBIO: leaguePublicId
  @Input() leaguePublicId!: number;

  private leagues = inject(LeagueService);

  loading = signal(false);
  error   = signal<string | null>(null);
  rows    = signal<LeagueTeam[]>([]);

  ngOnInit(): void {
    console.log('🎯 [TeamsList] leaguePublicId:', this.leaguePublicId);
    
    if (!this.leaguePublicId) { 
      console.error('❌ [TeamsList] LeaguePublicID inválido');
      this.error.set('LeaguePublicID inválido'); 
      return; 
    }
    this.fetch();
  }

  private fetch(): void {
    console.log('📡 [TeamsList] Cargando equipos para LeaguePublicID:', this.leaguePublicId);
    
    this.loading.set(true); 
    this.error.set(null);
    
    this.leagues.getTeams(this.leaguePublicId).subscribe({
      next: (r: any) => {
        console.log('✅ [TeamsList] Respuesta recibida:', r);
        console.log('🔍 [TeamsList] Tipo de r:', typeof r);
        console.log('🔍 [TeamsList] r.data:', r?.data);
        console.log('🔍 [TeamsList] r.Data:', r?.Data);
        
        const list: LeagueTeam[] = (r?.data ?? r?.Data ?? r) || [];
        
        console.log('🏈 [TeamsList] Lista de equipos:', list);
        console.log('🏈 [TeamsList] Total equipos:', list.length);
        
        this.rows.set(list);
        this.loading.set(false);
      },
      error: (e) => { 
        console.error('❌ [TeamsList] Error cargando equipos:', e);
        console.error('❌ [TeamsList] Error completo:', e.error);
        
        this.error.set('No se pudieron cargar los equipos'); 
        this.loading.set(false); 
      }
    });
  }
}