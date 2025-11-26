// src/app/pages/league/members/members-list.ts
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeagueService } from '../../../core/services/league-service';
import { LeagueMember } from '../../../core/models/league-model';

@Component({
  selector: 'app-members-list',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatChipsModule, MatProgressSpinnerModule],
  templateUrl: './members-list.html',
  styleUrl: './members-list.css'
})
export class MembersList implements OnInit {
  // ✅ CAMBIO: leaguePublicId
  @Input() leaguePublicId!: number;

  private leagues = inject(LeagueService);

  loading = signal(false);
  error   = signal<string | null>(null);
  rows    = signal<LeagueMember[]>([]);

  ngOnInit(): void {
    console.log('🎯 [MembersList] leaguePublicId:', this.leaguePublicId);
    
    if (!this.leaguePublicId) { 
      console.error('❌ [MembersList] LeaguePublicID inválido');
      this.error.set('LeaguePublicID inválido'); 
      return; 
    }
    this.fetch();
  }

  private fetch(): void {
    console.log('📡 [MembersList] Cargando miembros para LeaguePublicID:', this.leaguePublicId);
    
    this.loading.set(true); 
    this.error.set(null);
    
    this.leagues.getMembers(this.leaguePublicId).subscribe({
      next: (r: any) => {
        console.log('✅ [MembersList] Respuesta recibida:', r);
        console.log('🔍 [MembersList] Tipo de r:', typeof r);
        console.log('🔍 [MembersList] r.data:', r?.data);
        console.log('🔍 [MembersList] r.Data:', r?.Data);
        
        const list: LeagueMember[] = (r?.data ?? r?.Data ?? r) || [];
        
        console.log('👥 [MembersList] Lista de miembros:', list);
        console.log('👥 [MembersList] Total miembros:', list.length);
        
        this.rows.set(list);
        this.loading.set(false);
      },
      error: (e) => { 
        console.error('❌ [MembersList] Error cargando miembros:', e);
        console.error('❌ [MembersList] Error completo:', e.error);
        
        this.error.set('No se pudieron cargar los miembros'); 
        this.loading.set(false); 
      }
    });
  }
}