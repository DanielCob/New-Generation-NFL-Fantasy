// src/app/pages/league/create/create.ts
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
import { UserService } from '../../../core/services/user-service';
import { OwnedTeamOption } from '../../../core/models/team-model';
import { CreateLeagueRequest } from '../../../core/models/league-model';

@Component({
  standalone: true,
  selector: 'app-create',
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatSlideToggleModule, MatButtonModule,
    MatIconModule, MatSnackBarModule, MatProgressSpinnerModule
  ],
  templateUrl: './create.html',
  styleUrls: ['./create.css']
})
export class Create {
  private fb = inject(FormBuilder).nonNullable;   // 👈 no-nullable controls
  private leagues = inject(LeagueService);
  private users = inject(UserService);
  private snack = inject(MatSnackBar);

  loading = false;
  loadingProfile = true;
  hidePwd = true;

  teams: OwnedTeamOption[] = [];   // equipos del usuario para el select

  // Campos con el mismo casing del backend (menos el SelectedTeamId, que mapearemos)
  form = this.fb.group({
    Name: this.fb.control('', { validators: [Validators.required, Validators.maxLength(80)] }),
    Description: this.fb.control('', { validators: [Validators.maxLength(500)] }),
    TeamSlots: this.fb.control(10, { validators: [Validators.required, Validators.min(2), Validators.max(20)] }),
    PlayoffTeams: this.fb.control(4, { validators: [Validators.required, Validators.min(2)] }),
    AllowDecimals: this.fb.control(false),
    PositionFormatID: this.fb.control(1, { validators: [Validators.required] }),
    ScoringSchemaID: this.fb.control(1, { validators: [Validators.required] }),
    LeaguePassword: this.fb.control('', { validators: [Validators.minLength(4), Validators.maxLength(32)] }),
    // 👇 Nuevo: el usuario elige un equipo propio
    SelectedTeamId: this.fb.control<number | null>(null, { validators: [Validators.required] })
  });

  // opciones temporales – TODO: consumir endpoints de referencia
  positionFormats = [
    { id: 1, label: 'Standard' }, { id: 2, label: 'PPR Flex' }, { id: 3, label: '2QB / Superflex' },
  ];
  scoringSchemas = [
    { id: 1, label: 'Standard' }, { id: 2, label: 'Half-PPR' }, { id: 3, label: 'PPR' },
  ];

  get teamSlots() { return this.form.controls.TeamSlots.value; }

  ngOnInit(): void {
    // Cargar perfil completo para obtener equipos
    this.users.getProfile().subscribe({
      next: (profile) => {
        this.teams = (profile?.Teams ?? []).map(t => ({
          teamId: t.TeamID,
          teamName: t.TeamName,
          leagueName: t.LeagueName,
          thumbnailUrl: undefined
        }));
        this.loadingProfile = false;
      },
      error: () => {
        this.loadingProfile = false;
        this.teams = [];
        this.snack.open('No se pudieron cargar tus equipos', 'OK', { duration: 3000 });
      }
    });
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

    // Mapear equipo seleccionado -> InitialTeamName (API actual)
    const selId = this.form.controls.SelectedTeamId.value;
    const sel = this.teams.find(t => t.teamId === selId);
    if (!sel) {
      this.snack.open('Seleccioná un equipo válido', 'OK', { duration: 2500 });
      return;
    }

    // Armar body con el casing esperado por el backend
    const v = this.form.getRawValue();
    const body: CreateLeagueRequest = {
      Name: v.Name,
      Description: v.Description,
      TeamSlots: v.TeamSlots,
      PlayoffTeams: v.PlayoffTeams,
      AllowDecimals: v.AllowDecimals,
      PositionFormatID: v.PositionFormatID,
      ScoringSchemaID: v.ScoringSchemaID,
      LeaguePassword: this.form.controls.LeaguePassword.value,
      InitialTeamName: sel.teamName   // 👈 viene del equipo elegido
    };

    this.loading = true;
    this.leagues.create(body).subscribe({
      next: (res) => {
        this.loading = false;
        const id = res?.data?.LeagueID;
        const name = res?.data?.Name ?? v.Name;
        this.snack.open(id ? `League creada (#${id}) – ${name}` : 'League creada', 'OK', { duration: 3500 });

        // TODO: cuando exista “mis ligas”, guardar y navegar
        // localStorage.setItem('xnf.currentLeagueId', String(id));
        // localStorage.setItem('xnf.currentLeagueName', name);
        // this.router.navigate(['/league', id, 'summary']);

        this.form.reset({
          Name: '',
          Description: '',
          TeamSlots: 10,
          PlayoffTeams: 4,
          AllowDecimals: false,
          PositionFormatID: 1,
          ScoringSchemaID: 1,
          LeaguePassword: '',
          SelectedTeamId: null
        });
      },
      error: (err) => {
        this.loading = false;
        const e = err?.error;
        const msg = e?.suggestedAction || e?.message || 'No se pudo crear la liga';
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }
}
