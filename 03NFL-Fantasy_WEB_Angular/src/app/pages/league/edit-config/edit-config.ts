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
import { LeagueSummary } from '../../../core/models/league-model';

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
  @Input() leagueId!: number;
  @Input() preload = true; // ⬅️ si false, no hace prefill

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
    if (!this.leagueId || this.leagueId <= 0) {
      this.error.set('LeagueID inválido');
      return;
    }
    if (this.preload) this.prefill(this.leagueId);
  }

  private prefill(id: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.leagues.getSummary(id).subscribe({
      next: (r: any) => {
        const s: LeagueSummary | null = (r?.data ?? r?.Data ?? r) as LeagueSummary | null;
        if (!s) { this.error.set('No se pudo cargar la config'); this.loading.set(false); return; }
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
      error: () => {
        this.error.set('No se pudo cargar la config');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    if (this.form.invalid || !this.leagueId) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    this.leagues.editConfig(this.leagueId, {
      name: v.name!,
      description: v.description ?? '',
      teamSlots: v.teamSlots!,
      positionFormatID: v.positionFormatID!,
      scoringSchemaID: v.scoringSchemaID!,
      playoffTeams: v.playoffTeams!,
      allowDecimals: !!v.allowDecimals,
      tradeDeadlineEnabled: !!v.tradeDeadlineEnabled,
      tradeDeadlineDate: v.tradeDeadlineDate || '',
      maxRosterChangesPerTeam: v.maxRosterChangesPerTeam ?? 0,
      maxFreeAgentAddsPerTeam: v.maxFreeAgentAddsPerTeam ?? 0,
    }).subscribe({
      next: (resp: any) => {
        const msg = resp?.message ?? resp?.Message ?? 'Configuración guardada';
        this.snack.open(msg, 'OK', { duration: 2600 });
        this.saving.set(false);
      },
      error: (e) => {
        const msg = e?.error?.message ?? e?.error?.Message ?? 'Error al guardar';
        this.snack.open(msg, 'OK', { duration: 3200 });
        this.saving.set(false);
      }
    });
  }
}
