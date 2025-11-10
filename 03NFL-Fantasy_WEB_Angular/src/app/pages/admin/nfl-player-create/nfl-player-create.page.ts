import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { CreateNFLPlayerDTO } from '../../../core/models/nfl-player-model';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { NFLTeamBasic } from '../../../core/models/nfl-team-model';

const POSITIONS = ['QB','RB','WR','TE','K','DEF'];

@Component({
  selector: 'app-nfl-player-create',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './nfl-player-create.page.html',
  styleUrl: './nfl-player-create.page.css'
})
export class NFLPlayerCreatePage {
  private fb = inject(FormBuilder);
  private svc = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private snack = inject(MatSnackBar);

  loading = signal(false);
  loadingTeams = signal(false);
  teams = signal<NFLTeamBasic[]>([]);

  form = this.fb.group({
    FirstName: ['', Validators.required],
    LastName:  ['', Validators.required],
    Position:  ['', Validators.required],
    NFLTeamID: [null as number | null, Validators.required],
    // placeholders de imagen (no se envían si quedan vacíos)
    PhotoUrl:     [''],
    ThumbnailUrl: ['']
  });

  ngOnInit(): void {
    this.refreshTeams();
  }

  /** Carga equipos activos para el combo */
  refreshTeams(): void {
    this.loadingTeams.set(true);
    this.teamsSvc.getActive().subscribe({
      next: (resp: any) => {
        // Soportar ApiResponse con data/Data
        const arr: NFLTeamBasic[] = resp?.data ?? resp?.Data ?? [];
        this.teams.set(Array.isArray(arr) ? arr : []);
        this.loadingTeams.set(false);
      },
      error: _ => {
        this.loadingTeams.set(false);
        this.teams.set([]);
        this.snack.open('No se pudieron cargar los equipos', 'OK', { duration: 3000 });
      }
    });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);

    const raw = this.form.value;
    const dto: CreateNFLPlayerDTO = {
      FirstName:  raw.FirstName!,
      LastName:   raw.LastName!,
      Position:   raw.Position!,
      NFLTeamID:  raw.NFLTeamID!,
      ...(raw.PhotoUrl     ? { PhotoUrl: raw.PhotoUrl } : {}),
      ...(raw.ThumbnailUrl ? { ThumbnailUrl: raw.ThumbnailUrl } : {})
    };

    this.svc.create(dto).subscribe({
      next: (res: any) => {
        this.loading.set(false);
        this.snack.open(res?.message ?? 'Jugador creado', 'OK', { duration: 2500 });
        this.form.reset();
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message ?? 'Error al crear el jugador';
        this.snack.open(msg, 'OK', { duration: 3500 });
      }
    });
  }

  get positions() { return POSITIONS; }

  trackByTeam(index: number, item: NFLTeamBasic): number {
    return item.NFLTeamID;
  }
}
