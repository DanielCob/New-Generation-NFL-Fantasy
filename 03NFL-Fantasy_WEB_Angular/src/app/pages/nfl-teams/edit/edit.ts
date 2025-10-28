import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NFLTeamDetails, UpdateNFLTeamDTO } from '../../../core/models/nfl-team-model';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { ImageStorageService } from '../../../core/services/image-storage.service';

@Component({
  selector: 'app-nfl-team-edit',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatSnackBarModule, MatProgressSpinnerModule
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.css']
})
export class EditNFLTeamComponent {
  private fb = inject(FormBuilder);
  private nfl = inject(NFLTeamService);
  private route = inject(ActivatedRoute);
  public router = inject(Router);
  private snack = inject(MatSnackBar);
  private imageStorage = inject(ImageStorageService);

  teamId = signal<number | null>(null);
  loading = signal(true);
  saving = signal(false);
  uploading = signal(false);
  uploadingThumb = signal(false);
  computingA = signal(false);
  computingB = signal(false);

  selectedFile = signal<File | null>(null);
  imagePreview = signal<string | null>(null);
  thumbnailPreview = signal<string | null>(null);

  form: FormGroup = this.fb.group({
    teamName: ['', [Validators.required, Validators.minLength(2)]],
    city: ['', [Validators.required, Validators.minLength(2)]],

    teamImageUrl: [''],
    teamImageWidth: [''],
    teamImageHeight: [''],
    teamImageBytes: [''],

    thumbnailUrl: [''],
    thumbnailWidth: [''],
    thumbnailHeight: [''],
    thumbnailBytes: [''],
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      this.snack.open('Invalid team id.', 'Close', { duration: 3000 });
      this.router.navigate(['/nfl-teams']);
      return;
    }
    this.teamId.set(id);
    this.load(id);
  }

  private load(id: number): void {
    this.loading.set(true);
    this.nfl.getDetails(id).subscribe({
      next: (res) => {
        const ok = (res as any)?.success ?? (res as any)?.Success;
        const data = (res as any)?.data ?? (res as any)?.Data;
        const t = (ok && data) ? (data as NFLTeamDetails) : null;
        if (!t) {
          this.snack.open('Team not found.', 'Close', { duration: 3000 });
          this.router.navigate(['/nfl-teams']);
          return;
        }
        this.form.patchValue({
          teamName: t.TeamName,
          city: t.City,
          teamImageUrl: t.TeamImageUrl || '',
          teamImageWidth: t.TeamImageWidth || '',
          teamImageHeight: t.TeamImageHeight || '',
          teamImageBytes: t.TeamImageBytes || '',
          thumbnailUrl: t.ThumbnailUrl || '',
          thumbnailWidth: t.ThumbnailWidth || '',
          thumbnailHeight: t.ThumbnailHeight || '',
          thumbnailBytes: t.ThumbnailBytes || '',
        });

        // Mostrar preview de imágenes existentes
        if (t.TeamImageUrl) {
          this.imagePreview.set(t.TeamImageUrl);
        }
        if (t.ThumbnailUrl) {
          this.thumbnailPreview.set(t.ThumbnailUrl);
        }

        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Failed to load team.', 'Close', { duration: 3000 });
        this.router.navigate(['/nfl-teams']);
      }
    });
  }

  /**
   * Maneja la selección de archivo de imagen
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];

    // Validar tipo de archivo
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png'];
    if (!validTypes.includes(file.type)) {
      this.snack.open('Solo se permiten imágenes JPEG o PNG.', 'Cerrar', { duration: 4000 });
      input.value = ''; // Limpiar input
      return;
    }

    this.selectedFile.set(file);
    
    // Mostrar preview
    const reader = new FileReader();
    reader.onload = (e) => {
      this.imagePreview.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  }

  /**
   * Sube la imagen seleccionada y genera el thumbnail automáticamente
   */
  async uploadImage(): Promise<void> {
    const file = this.selectedFile();
    if (!file) {
      this.snack.open('Por favor selecciona una imagen primero.', 'Cerrar', { duration: 3000 });
      return;
    }

    this.uploading.set(true);

    try {
      // 1. Subir imagen principal
      const uploadResult = await this.imageStorage.uploadImage(file).toPromise();
      if (!uploadResult?.imageUrl) {
        throw new Error('No se recibió URL de la imagen.');
      }

      const mainImageUrl = uploadResult.imageUrl;
      this.form.patchValue({ teamImageUrl: mainImageUrl });

      // 2. Calcular metadatos de la imagen principal
      this.computingA.set(true);
      const mainMeta = await this.getImageMetaFromUrl(mainImageUrl);
      this.form.patchValue({
        teamImageWidth: mainMeta.width ?? '',
        teamImageHeight: mainMeta.height ?? '',
        teamImageBytes: mainMeta.bytes ?? ''
      });
      this.computingA.set(false);

      // 3. Generar y subir thumbnail
      this.uploadingThumb.set(true);
      const thumbnailFile = await this.generateThumbnail(file, 320, 180);
      const thumbResult = await this.imageStorage.uploadImage(thumbnailFile).toPromise();
      
      if (!thumbResult?.imageUrl) {
        throw new Error('No se pudo generar el thumbnail.');
      }

      const thumbUrl = thumbResult.imageUrl;
      this.form.patchValue({ thumbnailUrl: thumbUrl });

      // Mostrar preview del thumbnail
      const thumbReader = new FileReader();
      thumbReader.onload = (e) => {
        this.thumbnailPreview.set(e.target?.result as string);
      };
      thumbReader.readAsDataURL(thumbnailFile);

      // 4. Calcular metadatos del thumbnail
      this.computingB.set(true);
      const thumbMeta = await this.getImageMetaFromUrl(thumbUrl);
      this.form.patchValue({
        thumbnailWidth: thumbMeta.width ?? '',
        thumbnailHeight: thumbMeta.height ?? '',
        thumbnailBytes: thumbMeta.bytes ?? ''
      });
      this.computingB.set(false);
      this.uploadingThumb.set(false);

      this.snack.open('Imagen y thumbnail cargados exitosamente.', 'Cerrar', { duration: 3000 });
    } catch (error) {
      console.error('Error uploading image:', error);
      this.snack.open('Error al subir la imagen. Intenta de nuevo.', 'Cerrar', { duration: 5000 });
      this.computingA.set(false);
      this.computingB.set(false);
      this.uploadingThumb.set(false);
    } finally {
      this.uploading.set(false);
    }
  }

  /**
   * Genera un thumbnail redimensionado de la imagen
   */
  private generateThumbnail(file: File, targetWidth: number, targetHeight: number): Promise<File> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
          // Crear canvas para redimensionar
          const canvas = document.createElement('canvas');
          canvas.width = targetWidth;
          canvas.height = targetHeight;
          const ctx = canvas.getContext('2d');
          
          if (!ctx) {
            reject(new Error('No se pudo crear contexto de canvas.'));
            return;
          }

          // Dibujar imagen redimensionada
          ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

          // Convertir canvas a blob
          canvas.toBlob((blob) => {
            if (!blob) {
              reject(new Error('No se pudo generar thumbnail.'));
              return;
            }

            // Crear archivo del thumbnail
            const thumbnailFile = new File(
              [blob], 
              `thumb_${file.name}`, 
              { type: file.type }
            );
            resolve(thumbnailFile);
          }, file.type, 0.9); // 0.9 = calidad 90%
        };

        img.onerror = () => reject(new Error('Error al cargar la imagen.'));
        img.src = e.target?.result as string;
      };

      reader.onerror = () => reject(new Error('Error al leer el archivo.'));
      reader.readAsDataURL(file);
    });
  }

  /**
   * Limpia la imagen seleccionada
   */
  clearImage(): void {
    this.selectedFile.set(null);
    this.imagePreview.set(null);
    this.thumbnailPreview.set(null);
    this.form.patchValue({
      teamImageUrl: '',
      teamImageWidth: '',
      teamImageHeight: '',
      teamImageBytes: '',
      thumbnailUrl: '',
      thumbnailWidth: '',
      thumbnailHeight: '',
      thumbnailBytes: '',
    });
  }

  async onTeamImageUrlChanged(): Promise<void> {
    const url = (this.form.get('teamImageUrl')?.value || '').trim();
    if (!url) {
      this.form.patchValue({ teamImageWidth: '', teamImageHeight: '', teamImageBytes: '' });
      return;
    }
    this.computingA.set(true);
    const meta = await this.getImageMetaFromUrl(url);
    this.form.patchValue({
      teamImageWidth: meta.width ?? '',
      teamImageHeight: meta.height ?? '',
      teamImageBytes: meta.bytes ?? ''
    });
    this.computingA.set(false);
  }

  async onThumbUrlChanged(): Promise<void> {
    const url = (this.form.get('thumbnailUrl')?.value || '').trim();
    if (!url) {
      this.form.patchValue({ thumbnailWidth: '', thumbnailHeight: '', thumbnailBytes: '' });
      return;
    }
    this.computingB.set(true);
    const meta = await this.getImageMetaFromUrl(url);
    this.form.patchValue({
      thumbnailWidth: meta.width ?? '',
      thumbnailHeight: meta.height ?? '',
      thumbnailBytes: meta.bytes ?? ''
    });
    this.computingB.set(false);
  }

  save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.uploading() || this.uploadingThumb() || this.computingA() || this.computingB()) {
      this.snack.open('Espera a que se complete la carga de imágenes…', 'Cerrar', { duration: 3000 });
      return;
    }

    const v = this.form.value;
    const dto: UpdateNFLTeamDTO = {
      TeamName: v.teamName?.trim(),
      City: v.city?.trim(),
      ...(v.teamImageUrl?.trim()
        ? {
            TeamImageUrl: v.teamImageUrl.trim(),
            TeamImageWidth: this.num(v.teamImageWidth),
            TeamImageHeight: this.num(v.teamImageHeight),
            TeamImageBytes: this.num(v.teamImageBytes),
          } : {}),
      ...(v.thumbnailUrl?.trim()
        ? {
            ThumbnailUrl: v.thumbnailUrl.trim(),
            ThumbnailWidth: this.num(v.thumbnailWidth),
            ThumbnailHeight: this.num(v.thumbnailHeight),
            ThumbnailBytes: this.num(v.thumbnailBytes),
          } : {})
    };

    const must = (url?: string, w?: number, h?: number, b?: number) =>
      !url || (w && h && b && w > 0 && h > 0 && b > 0);
    if (!must(dto.TeamImageUrl, dto.TeamImageWidth, dto.TeamImageHeight, dto.TeamImageBytes) ||
        !must(dto.ThumbnailUrl, dto.ThumbnailWidth, dto.ThumbnailHeight, dto.ThumbnailBytes)) {
      this.snack.open('Imagen/thumbnail requieren Width/Height/Bytes válidos.', 'Cerrar', { duration: 4000 });
      return;
    }

    const id = this.teamId();
    if (!id) return;

    this.saving.set(true);
    this.nfl.update(id, dto).subscribe({
      next: (res) => {
        const ok = (res as any)?.success ?? (res as any)?.Success;
        const msg = (res as any)?.message ?? (res as any)?.Message ?? 'Team updated.';
        if (ok) {
          this.snack.open(msg, 'Cerrar', { duration: 3000 });
          this.router.navigate(['/nfl-teams']);
        } else {
          this.snack.open(msg, 'Cerrar', { duration: 5000 });
        }
        this.saving.set(false);
      },
      error: (err) => {
        console.error('update team error:', err);
        this.snack.open('No se pudo actualizar el equipo.', 'Cerrar', { duration: 5000 });
        this.saving.set(false);
      }
    });
  }

  // Helpers
  private num(x: any): number | undefined {
    const n = Number(x);
    return Number.isFinite(n) ? n : undefined;
  }

  private async getImageMetaFromUrl(url: string): Promise<{ width: number | null, height: number | null, bytes: number | null }> {
    const dims = await new Promise<{ width: number | null; height: number | null }>((r) => {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => r({ width: img.naturalWidth || null, height: img.naturalHeight || null });
      img.onerror = () => r({ width: null, height: null });
      img.src = url;
    });
    let bytes: number | null = null;
    try {
      const h = await fetch(url, { method: 'HEAD', mode: 'cors' });
      const len = h.headers.get('content-length');
      bytes = len ? Number(len) : null;
    } catch { /* ignore */ }
    if (bytes == null) {
      try {
        const g = await fetch(url, { method: 'GET', mode: 'cors' });
        const b = await g.blob();
        bytes = b.size;
      } catch { bytes = null; }
    }
    return { width: dims.width, height: dims.height, bytes };
  }
}