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
import { ImageStorageService } from '../../../core/services/image-storage.service';

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
  private imageSvc = inject(ImageStorageService);

  id = Number(this.route.snapshot.paramMap.get('id'));

  pageLoading = signal(false);
  saving      = signal(false);
  toggling    = signal(false);

  teams   = signal<NFLTeamBasic[]>([]);
  details = signal<NFLPlayerDetails | null>(null);
  active  = signal<boolean>(false);

  imagePreview = signal<string | null>(null);
  selectedFile: File | null = null; // 👈 se guarda aquí el archivo temporal

  form = this.fb.group({
    FirstName: [''],
    LastName:  [''],
    Position:  [''],
    NFLTeamID: [null as number | null],
    PhotoUrl:     [''],
    ThumbnailUrl: ['']
  });

  ngOnInit(): void {
    this.teamsSvc.getActive().subscribe({
      next: r => this.teams.set((r as any).data ?? (r as any).Data ?? []),
      error: _ => this.snack.open('No se pudieron cargar los equipos', 'OK', { duration: 3000 })
    });

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

        this.imagePreview.set(d?.PhotoUrl ?? d?.photoUrl ?? null);
        this.pageLoading.set(false);
      },
      error: _ => {
        this.pageLoading.set(false);
        this.snack.open('No se pudo cargar el jugador', 'OK', { duration: 3000 });
        this.router.navigate(['/admin/nfl-player-list']);
      }
    });
  }

  onFileSelected(event: Event): void {
    const fileInput = event.target as HTMLInputElement;
    if (!fileInput.files || fileInput.files.length === 0) return;

    const file = fileInput.files[0];
    if (!file.type.startsWith('image/')) {
      this.snack.open('Solo se permiten archivos de imagen', 'OK', { duration: 2500 });
      return;
    }

    // solo vista previa
    const reader = new FileReader();
    reader.onload = e => this.imagePreview.set(e.target?.result as string);
    reader.readAsDataURL(file);

    this.selectedFile = file; // 👈 se guarda para subirla después en "save"
  }

  onActiveToggle(checked: boolean): void {
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
        const msg = err?.error?.message || err?.error?.Message || 'No se pudo cambiar el estado';
        this.snack.open(msg, 'OK', { duration: 4000 });
        this.toggling.set(false);
      }
    });
  }

  async save(): Promise<void> {
    this.saving.set(true);

    try {
      const dto: UpdateNFLPlayerDTO = {};

      let width: number | undefined;
      let height: number | undefined;
      let bytes: number | undefined;

      // Subir imagen si se seleccionó una
      if (this.selectedFile) {
        const file = this.selectedFile;
        bytes = file.size; // ✅ número

        // calcular ancho y alto de la imagen
        const img = new Image();
        const imageLoadPromise = new Promise<void>((resolve) => {
          img.onload = () => {
            width = img.width;
            height = img.height;
            resolve();
          };
        });
        img.src = URL.createObjectURL(file);
        await imageLoadPromise;

        const res: any = await this.imageSvc.uploadImage(file).toPromise();
        console.log('Upload response:', res);
        const url = res?.imageUrl ?? res?.data?.imageUrl ?? res?.url ?? '';
        const thumb = res?.data?.thumbnailUrl ?? res?.Data?.ThumbnailUrl ?? res?.thumbnailUrl ?? '';

        this.form.patchValue({ PhotoUrl: url, ThumbnailUrl: thumb });
      }

      Object.entries(this.form.value).forEach(([k, v]) => {
        if (v !== null && v !== undefined && v !== '') (dto as any)[k] = v;
      });

      // agregar metadatos numéricos de la imagen
      dto.PhotoWidth = width;
      dto.PhotoHeight = height;
      dto.PhotoBytes = bytes;
      
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

    } catch (e) {
      console.error(e);
      this.snack.open('Error al subir imagen', 'OK', { duration: 3000 });
      this.saving.set(false);
    }
  }

  get displayName(): string {
    const d: any = this.details();
    if (!d) return `#${this.id}`;
    const full = d.FullName ?? d.fullName;
    if (typeof full === 'string' && full.trim()) return full.trim();
    const name = `${d.FirstName ?? d.firstName ?? ''} ${d.LastName ?? d.lastName ?? ''}`.trim();
    return name || `#${this.id}`;
  }

  get positions() { return POSITIONS; }
}
