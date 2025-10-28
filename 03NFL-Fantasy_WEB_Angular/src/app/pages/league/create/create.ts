// create.component.ts - ARCHIVO COMPLETO CON InitialTeamName OPCIONAL

import { Component, inject } from '@angular/core';
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

  form = this.fb.group({
    Name: this.fb.control('', { validators: [Validators.required, Validators.maxLength(80)] }),
    Description: this.fb.control('', { validators: [Validators.maxLength(500)] }),
    TeamSlots: this.fb.control(10, { validators: [Validators.required, Validators.min(2), Validators.max(20)] }),
    PlayoffTeams: this.fb.control(4, { validators: [Validators.required, Validators.min(2)] }),
    AllowDecimals: this.fb.control(false),
    PositionFormatID: this.fb.control(1, { validators: [Validators.required] }),
    ScoringSchemaID: this.fb.control(1, { validators: [Validators.required] }),
    InitialTeamName: this.fb.control('', { validators: [Validators.maxLength(40)] }), // ← AHORA ES OPCIONAL
    LeaguePassword: this.fb.control('', { validators: [Validators.required, Validators.minLength(4), Validators.maxLength(32)] })
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

  get teamSlots() { 
    return this.form.controls.TeamSlots.value; 
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open('Revisá los campos requeridos', 'OK', { duration: 2500 });
      return;
    }
    
    const playoff = this.form.controls.PlayoffTeams.value;
    if (playoff > this.teamSlots) {
      this.snack.open('Playoff teams no puede exceder Team slots', 'OK', { duration: 2500 });
      return;
    }

    const v = this.form.getRawValue();
    
    // Construir body base
    const body: CreateLeagueRequest = {
      Name: v.Name,
      Description: v.Description,
      TeamSlots: v.TeamSlots,
      PlayoffTeams: v.PlayoffTeams,
      AllowDecimals: v.AllowDecimals,
      PositionFormatID: v.PositionFormatID,
      ScoringSchemaID: v.ScoringSchemaID,
      LeaguePassword: v.LeaguePassword
    };

    // Solo agregar InitialTeamName si tiene valor
    const teamName = v.InitialTeamName?.trim();
    if (teamName) {
      body.InitialTeamName = teamName;
    }

    console.log('🚀 Body a enviar:', body);

    this.loading = true;
    this.leagues.create(body).subscribe({
      next: (res) => {
        console.log('✅ Respuesta de crear liga:', res);
        this.loading = false;
        
        const apiData = (res as any)?.Data ?? (res as any)?.data;
        const id = apiData?.LeagueID ?? apiData?.leagueID;
        const name = apiData?.Name ?? apiData?.name ?? v.Name;
        
        this.snack.open(
          id ? `Liga creada (#${id}) – ${name}` : 'Liga creada exitosamente', 
          'OK', 
          { duration: 3500 }
        );

        this.form.reset({
          Name: '',
          Description: '',
          TeamSlots: 10,
          PlayoffTeams: 4,
          AllowDecimals: false,
          PositionFormatID: 1,
          ScoringSchemaID: 1,
          InitialTeamName: '',
          LeaguePassword: ''
        });
      },
      error: (err) => {
        console.error('❌ Error al crear liga:', err);
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
          msg = e?.message ?? e?.Message ?? e?.suggestedAction ?? 'No se pudo crear la liga';
        }
        
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }
}