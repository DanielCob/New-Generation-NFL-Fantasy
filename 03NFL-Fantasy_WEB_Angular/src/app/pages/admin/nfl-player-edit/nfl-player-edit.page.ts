import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';

import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { UpdateNFLPlayerDTO, NFLPlayerDetails } from '../../../core/models/nfl-player-model';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { NFLTeamBasic } from '../../../core/models/nfl-team-model';

const POSITIONS = ['QB','RB','WR','TE','K','DEF'];

@Component({
  selector: 'app-nfl-player-edit',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSlideToggleModule, MatChipsModule
  ],
  templateUrl: './nfl-player-edit.page.html',
  styleUrl: './nfl-player-edit.page.css'
})
export class NFLPlayerEditPage {
  private fb = inject(FormBuilder);
  private svc = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  id = Number(this.route.snapshot.paramMap.get('id'));

  // estados separados para no “congelar” el toggle
  pageLoading = signal(false);
  saving      = signal(false);
  toggling    = signal(false);

  teams   = signal<NFLTeamBasic[]>([]);
  details = signal<NFLPlayerDetails | null>(null);
  active  = signal<boolean>(false);

  form = this.fb.group({
    FirstName: [''],
    LastName:  [''],
    Position:  [''],
    NFLTeamID: [null as number | null],
    PhotoUrl:     [''],
    ThumbnailUrl: ['']
  });

  ngOnInit(): void {
    // Cargar equipos activos
    this.teamsSvc.getActive().subscribe({
      next: r => this.teams.set((r as any).data ?? (r as any).Data ?? []),
      error: _ => this.snack.open('No se pudieron cargar los equipos', 'OK', { duration: 3000 })
    });

    // Cargar detalles del jugador y precargar form + estado activo
    this.loadDetails();
  }

  private loadDetails(): void {
    this.pageLoading.set(true);
    this.svc.getDetails(this.id).subscribe({
      next: r => {
        const d: any = (r as any).data ?? (r as any).Data;
        this.details.set(d);
        this.active.set(!!(d?.IsActive ?? d?.isActive));

        this.form.patchValue({
          FirstName: d?.FirstName ?? d?.firstName ?? '',
          LastName:  d?.LastName  ?? d?.lastName  ?? '',
          Position:  d?.Position  ?? d?.position  ?? '',
          NFLTeamID: d?.NFLTeamID ?? d?.nflTeamId ?? null,
          PhotoUrl: d?.PhotoUrl ?? d?.photoUrl ?? '',
          ThumbnailUrl: d?.ThumbnailUrl ?? d?.thumbnailUrl ?? ''
        });

        this.pageLoading.set(false);
      },
      error: _ => {
        this.pageLoading.set(false);
        this.snack.open('No se pudo cargar el jugador', 'OK', { duration: 3000 });
        this.router.navigate(['/admin/nfl-player-list']);
      }
    });
  }

  get displayName(): string {
    const d: any = this.details();
    if (!d) return `#${this.id}`;
    const full = d.FullName ?? d.fullName;
    if (typeof full === 'string' && full.trim()) return full.trim();
    const name = `${d.FirstName ?? d.firstName ?? ''} ${d.LastName ?? d.lastName ?? ''}`.trim();
    return name || `#${this.id}`;
  }

  onActiveToggle(checked: boolean): void {
    // bloquear solo el toggle mientras llama API
    this.toggling.set(true);
    const call = checked ? this.svc.reactivate(this.id) : this.svc.deactivate(this.id);

    call.subscribe({
      next: r => {
        this.active.set(checked);
        const d: any = this.details();
        if (d) this.details.set({ ...d, IsActive: checked });
        const msg = (r as any)?.message || (r as any)?.Message || (checked ? 'Jugador reactivado' : 'Jugador desactivado');
        this.snack.open(msg, 'OK', { duration: 2500 });
        this.toggling.set(false);
      },
      error: err => {
        // Mostrar motivo (p.ej. “asignado a un equipo activo en la temporada actual”)
        const msg = err?.error?.message || err?.error?.Message || 'No se pudo cambiar el estado';
        this.snack.open(msg, 'OK', { duration: 4000 });
        // revertir visualmente
        this.toggling.set(false);
      }
    });
  }

  save(): void {
    const dto: UpdateNFLPlayerDTO = {};
    Object.entries(this.form.value).forEach(([k, v]) => {
      if (v !== null && v !== undefined && v !== '') (dto as any)[k] = v;
    });

    this.saving.set(true);
    this.svc.update(this.id, dto).subscribe({
      next: r => {
        const msg = (r as any)?.message || (r as any)?.Message || 'Jugador actualizado';
        this.snack.open(msg, 'OK', { duration: 2500 });
        this.saving.set(false);
        this.router.navigate(['/admin/nfl-player-list']);
      },
      error: err => {
        const msg = err?.error?.message || err?.error?.Message || 'Error al actualizar';
        this.snack.open(msg, 'OK', { duration: 3500 });
        this.saving.set(false);
      }
    });
  }

  get positions() { return POSITIONS; }
}
