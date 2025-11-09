import { Component, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { LeagueService } from '../../../core/services/league-service';
import { CreateLeagueRequest } from '../../../core/models/league-model';

interface TeamSlotsOption {
  value: number;
  label: string;
  description: string;
  allowedPlayoffTeams: number[];
}

interface PlayoffTeamsOption {
  value: number;
  label: string;
  description: string;
}

@Component({
  standalone: true,
  selector: 'app-create',
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatSlideToggleModule, MatButtonModule,
    MatIconModule, MatSnackBarModule, MatProgressSpinnerModule
  ],
  templateUrl: './create.html',
  styleUrls: ['./create.css']
})
export class Create {
  private fb = inject(FormBuilder).nonNullable;
  private leagues = inject(LeagueService);
  private snack = inject(MatSnackBar);

  loading = false;
  hidePwd = true;

  // ✅ Opciones predefinidas según el SP
  teamSlotsOptions: TeamSlotsOption[] = [
    { value: 4, label: '4 Teams', description: 'Small league', allowedPlayoffTeams: [4] },
    { value: 6, label: '6 Teams', description: 'Compact league', allowedPlayoffTeams: [4] },
    { value: 8, label: '8 Teams', description: 'Standard league', allowedPlayoffTeams: [4] },
    { value: 10, label: '10 Teams', description: 'Medium league', allowedPlayoffTeams: [4, 6] },
    { value: 12, label: '12 Teams', description: 'Large league', allowedPlayoffTeams: [4, 6] },
    { value: 14, label: '14 Teams', description: 'Extended league', allowedPlayoffTeams: [4, 6] },
    { value: 16, label: '16 Teams', description: 'Very large league', allowedPlayoffTeams: [4, 6] },
    { value: 18, label: '18 Teams', description: 'Professional league', allowedPlayoffTeams: [4, 6] },
    { value: 20, label: '20 Teams', description: 'Maximum league', allowedPlayoffTeams: [4, 6] }
  ];

  playoffTeamsOptions: PlayoffTeamsOption[] = [
    { value: 4, label: '4 Teams', description: 'Semi-finals' },
    { value: 6, label: '6 Teams', description: 'With wild cards' }
  ];

  // ✅ Señal para opciones dinámicas de playoff teams
  availablePlayoffTeams = signal<PlayoffTeamsOption[]>([]);

  form = this.fb.group({
    Name: this.fb.control('', { validators: [Validators.required, Validators.maxLength(80)] }),
    Description: this.fb.control('', { validators: [Validators.maxLength(500)] }),
    TeamSlots: this.fb.control(8, { validators: [Validators.required] }), // ✅ Valor por defecto: 8
    PlayoffTeams: this.fb.control(4, { validators: [Validators.required] }), // ✅ Valor por defecto: 4
    AllowDecimals: this.fb.control(false),
    PositionFormatID: this.fb.control(1, { validators: [Validators.required] }),
    ScoringSchemaID: this.fb.control(1, { validators: [Validators.required] }),
    InitialTeamName: this.fb.control('', { 
      validators: [Validators.required, Validators.maxLength(40)] // ✅ AHORA ES OBLIGATORIO
    }),
    LeaguePassword: this.fb.control('', { 
      validators: [
        Validators.required, 
        Validators.minLength(8), 
        Validators.maxLength(12),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,12}$/)
      ] 
    })
  });

  positionFormats = [
    { id: 1, label: 'Standard' }, 
    { id: 2, label: 'PPR Flex' }, 
    { id: 3, label: '2QB / Superflex' }
  ];
  
  scoringSchemas = [
    { id: 1, label: 'Standard' }, 
    { id: 2, label: 'Half-PPR' }, 
    { id: 3, label: 'PPR' }
  ];

  constructor() {
    // ✅ Inicializar opciones de playoff teams
    this.updatePlayoffTeamsOptions(8);
    
    // ✅ Escuchar cambios en teamSlots
    this.form.controls.TeamSlots.valueChanges.subscribe(teamSlots => {
      if (teamSlots) {
        this.updatePlayoffTeamsOptions(teamSlots);
      }
    });
  }

  // ✅ Actualizar opciones disponibles de playoff teams
  private updatePlayoffTeamsOptions(teamSlots: number): void {
    const selectedTeamSlots = this.teamSlotsOptions.find(opt => opt.value === teamSlots);
    
    if (selectedTeamSlots) {
      const availableOptions = this.playoffTeamsOptions.filter(opt => 
        selectedTeamSlots.allowedPlayoffTeams.includes(opt.value)
      );
      
      this.availablePlayoffTeams.set(availableOptions);
      
      // ✅ Ajustar playoffTeams si la selección actual no es válida
      const currentPlayoffTeams = this.form.controls.PlayoffTeams.value;
      if (currentPlayoffTeams && !availableOptions.some(opt => opt.value === currentPlayoffTeams)) {
        this.form.controls.PlayoffTeams.setValue(availableOptions[0]?.value || 4);
      }
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open('Please check required fields', 'OK', { duration: 2500 });
      return;
    }

    const v = this.form.getRawValue();
    
    // Construir body
    const body: CreateLeagueRequest = {
      Name: v.Name,
      Description: v.Description,
      TeamSlots: v.TeamSlots,
      PlayoffTeams: v.PlayoffTeams,
      AllowDecimals: v.AllowDecimals,
      PositionFormatID: v.PositionFormatID,
      ScoringSchemaID: v.ScoringSchemaID,
      LeaguePassword: v.LeaguePassword,
      InitialTeamName: v.InitialTeamName // ✅ SIEMPRE se envía (obligatorio)
    };

    console.log('🚀 Body to send:', body);

    this.loading = true;
    this.leagues.create(body).subscribe({
      next: (res) => {
        console.log('✅ League creation response:', res);
        this.loading = false;
        
        const apiData = (res as any)?.Data ?? (res as any)?.data;
        const id = apiData?.LeagueID ?? apiData?.leagueID;
        const name = apiData?.Name ?? apiData?.name ?? v.Name;
        
        this.snack.open(
          id ? `League created (#${id}) – ${name}` : 'League created successfully', 
          'OK', 
          { duration: 3500 }
        );

        // ✅ Reset form manteniendo valores por defecto
        this.form.reset({
          Name: '',
          Description: '',
          TeamSlots: 8,
          PlayoffTeams: 4,
          AllowDecimals: false,
          PositionFormatID: 1,
          ScoringSchemaID: 1,
          InitialTeamName: '',
          LeaguePassword: ''
        });
        
        // ✅ Restaurar opciones de playoff teams
        this.updatePlayoffTeamsOptions(8);
      },
      error: (err) => {
        console.error('❌ Error creating league:', err);
        this.loading = false;
        
        const e = err?.error ?? err;
        
        // Manejar errores de validación
        let msg = '';
        if (e?.errors && typeof e.errors === 'object') {
          const errorMessages: string[] = [];
          for (const [field, messages] of Object.entries(e.errors)) {
            if (Array.isArray(messages)) {
              errorMessages.push(...messages);
            }
          }
          msg = errorMessages.join(' | ');
        }
        
        if (!msg) {
          msg = e?.message ?? e?.Message ?? e?.suggestedAction ?? 'Could not create league';
        }
        
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }
}