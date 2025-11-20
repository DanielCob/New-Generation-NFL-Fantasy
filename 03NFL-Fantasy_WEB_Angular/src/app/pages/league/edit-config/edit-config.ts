// src/app/pages/league/edit-config/edit-config.ts
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { LeagueService } from '../../../core/services/league-service';
import { EditLeagueConfigRequest, LeagueSummary } from '../../../core/models/league-model';

@Component({
  selector: 'app-edit-config',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './edit-config.html',
  styleUrl: './edit-config.css'
})
export class EditConfigForm implements OnInit {
  // ✅ CAMBIO: leaguePublicId
  @Input() leaguePublicId!: number;
  @Input() preload = true;

  private leagues = inject(LeagueService);
  private fb = inject(FormBuilder).nonNullable;
  private snack = inject(MatSnackBar);

  loading = signal(false);
  saving  = signal(false);
  error   = signal<string | null>(null);

  form = this.fb.group({
    name: this.fb.control('', { validators: [Validators.required, Validators.maxLength(80)] }),
    description: this.fb.control('', { validators: [Validators.maxLength(500)] }),
    teamSlots: this.fb.control(10, { validators: [Validators.required, Validators.min(2), Validators.max(32)] }),
    positionFormatID: this.fb.control<number | null>(null, { validators: [Validators.required] }),
    scoringSchemaID: this.fb.control<number | null>(null,   { validators: [Validators.required] }),
    playoffTeams: this.fb.control(4, { validators: [Validators.required, Validators.min(2), Validators.max(16)] }),
    allowDecimals: this.fb.control(false),
    tradeDeadlineEnabled: this.fb.control(false),
    tradeDeadlineDate: this.fb.control(''),
    maxRosterChangesPerTeam: this.fb.control(0, { validators: [Validators.min(0)] }),
    maxFreeAgentAddsPerTeam: this.fb.control(0, { validators: [Validators.min(0)] }),
  });

  ngOnInit(): void {
    console.log('🎯 [EditConfigForm] leaguePublicId:', this.leaguePublicId);
    
    if (!this.leaguePublicId || this.leaguePublicId <= 0) {
      this.error.set('LeaguePublicID inválido');
      return;
    }
    if (this.preload) this.prefill(this.leaguePublicId);
  }

  private prefill(id: number): void {
    console.log('📡 [EditConfigForm] Cargando configuración para LeaguePublicID:', id);
    
    this.loading.set(true);
    this.error.set(null);
    this.leagues.getSummary(id).subscribe({
      next: (r: any) => {
        console.log('✅ [EditConfigForm] Summary recibido:', r);
        
        const s: LeagueSummary | null = (r?.data ?? r?.Data ?? r) as LeagueSummary | null;
        if (!s) { 
          this.error.set('No se pudo cargar la config'); 
          this.loading.set(false); 
          return; 
        }
        
        this.form.patchValue({
          name: s.Name,
          description: s.Description ?? '',
          teamSlots: s.TeamSlots,
          positionFormatID: s.PositionFormatID,
          scoringSchemaID: s.ScoringSchemaID,
          playoffTeams: s.PlayoffTeams,
          allowDecimals: !!s.AllowDecimals,
          tradeDeadlineEnabled: !!s.TradeDeadlineEnabled,
          tradeDeadlineDate: '',
          maxRosterChangesPerTeam: 0,
          maxFreeAgentAddsPerTeam: 0
        });
        this.loading.set(false);
      },
      error: (e) => {
        console.error('❌ [EditConfigForm] Error cargando config:', e);
        this.error.set('No se pudo cargar la config');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    if (this.form.invalid || !this.leaguePublicId) return;
    
    console.log('💾 [EditConfigForm] Guardando configuración para LeaguePublicID:', this.leaguePublicId);
    
    this.saving.set(true);
    const v = this.form.getRawValue();
    
    // ✅ Usar PascalCase para coincidir con el API
    const payload: EditLeagueConfigRequest = {
      Name: v.name!,
      Description: v.description ?? '',
      TeamSlots: v.teamSlots!,
      PositionFormatID: v.positionFormatID!,
      ScoringSchemaID: v.scoringSchemaID!,
      PlayoffTeams: v.playoffTeams!,
      AllowDecimals: !!v.allowDecimals,
      TradeDeadlineEnabled: !!v.tradeDeadlineEnabled,
      TradeDeadlineDate: v.tradeDeadlineDate || '',
      MaxRosterChangesPerTeam: v.maxRosterChangesPerTeam ?? 0,
      MaxFreeAgentAddsPerTeam: v.maxFreeAgentAddsPerTeam ?? 0,
    };
    
    console.log('📤 [EditConfigForm] Payload:', payload);
    
    this.leagues.editConfig(this.leaguePublicId, payload).subscribe({
      next: (resp: any) => {
        console.log('✅ [EditConfigForm] Configuración guardada:', resp);
        const msg = resp?.message ?? resp?.Message ?? 'Configuración guardada';
        this.snack.open(msg, 'OK', { duration: 2600 });
        this.saving.set(false);
      },
      error: (e) => {
        console.error('❌ [EditConfigForm] Error guardando:', e);
        console.error('❌ [EditConfigForm] Error body:', e.error);
        const msg = e?.error?.message ?? e?.error?.Message ?? 'Error al guardar';
        this.snack.open(msg, 'OK', { duration: 3200 });
        this.saving.set(false);
      }
    });
  }
}