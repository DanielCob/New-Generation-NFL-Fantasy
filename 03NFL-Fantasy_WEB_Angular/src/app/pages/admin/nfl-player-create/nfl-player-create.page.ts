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
import { ImageStorageService } from '../../../core/services/image-storage.service';

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
  styleUrls: ['./nfl-player-create.page.css']
})
export class NFLPlayerCreatePage {
  private fb = inject(FormBuilder);
  private svc = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private snack = inject(MatSnackBar);
  private imageSvc = inject(ImageStorageService);

  loading = signal(false);
  loadingTeams = signal(false);
  teams = signal<NFLTeamBasic[]>([]);

  // 🔹 Estados para imagen
  photoPreview: string | null = null;
  selectedFile: File | null = null;
  selectedFileName: string | null = null;
  photoError: string | null = null;

  form = this.fb.group({
    FirstName: ['', Validators.required],
    LastName:  ['', Validators.required],
    Position:  ['', Validators.required],
    NFLTeamID: [null as number | null, Validators.required],
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

  /** 🔹 Manejador de selección de imagen */
  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.photoError = null;

    // Validaciones
    if (!file.type.startsWith('image/')) {
      this.photoError = 'El archivo debe ser una imagen.';
      return;
    }
    if (file.size > 2 * 1024 * 1024) { // 2 MB
      this.photoError = 'La imagen no debe superar los 2 MB.';
      return;
    }

    this.selectedFileName = file.name;
    this.selectedFile = file;

    // Leer y previsualizar
    const reader = new FileReader();
    reader.onload = () => this.photoPreview = reader.result as string;
    reader.readAsDataURL(file);
  }

  /** 🔹 Limpia la imagen seleccionada */
  clearPhoto(): void {
    this.photoPreview = null;
    this.selectedFile = null;
    this.selectedFileName = null;
    this.form.patchValue({
      PhotoUrl: '',
      ThumbnailUrl: ''
    });
  }

  /** 🔹 Crear jugador */
  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    try {
      let photoUrl = '';
      let thumbUrl = '';
      let width: number | undefined;
      let height: number | undefined;
      let bytes: number | undefined;

      // Subir imagen si hay archivo seleccionado
      if (this.selectedFile) {
        bytes = this.selectedFile.size;

        // Obtener dimensiones
        const img = new Image();
        const loadPromise = new Promise<void>((resolve) => {
          img.onload = () => {
            width = img.width;
            height = img.height;
            resolve();
          };
        });
        img.src = URL.createObjectURL(this.selectedFile);
        await loadPromise;

        const res: any = await this.imageSvc.uploadImage(this.selectedFile).toPromise();
        console.log('Upload response:', res);
        photoUrl = res?.imageUrl ?? res?.data?.imageUrl ?? '';
        thumbUrl = res?.thumbnailUrl ?? res?.data?.thumbnailUrl ?? '';
      }

      const raw = this.form.value;
      const dto: CreateNFLPlayerDTO = {
        FirstName: raw.FirstName!,
        LastName: raw.LastName!,
        Position: raw.Position!,
        NFLTeamID: raw.NFLTeamID!,
        ...(photoUrl ? { PhotoUrl: photoUrl } : {}),
        ...(thumbUrl ? { ThumbnailUrl: thumbUrl } : {}),
        ...(width ? { PhotoWidth: width } : {}),
        ...(height ? { PhotoHeight: height } : {}),
        ...(bytes ? { PhotoBytes: bytes } : {})
      };

      this.svc.create(dto).subscribe({
        next: (res: any) => {
          this.loading.set(false);
          this.snack.open(res?.message ?? 'Jugador creado', 'OK', { duration: 2500 });
          this.form.reset();
          this.clearPhoto();
        },
        error: (err) => {
          this.loading.set(false);
          const msg = err?.error?.message ?? err?.error?.Message ?? 'Error al crear el jugador';
          this.snack.open(msg, 'OK', { duration: 3500 });
        }
      });

    } catch (err) {
      console.error(err);
      this.loading.set(false);
      this.snack.open('Error al subir la imagen o crear el jugador', 'OK', { duration: 3500 });
    }
  }

  get positions() { return POSITIONS; }

  trackByTeam(index: number, item: NFLTeamBasic): number {
    return item.NFLTeamID;
  }
}
