// edit-branding.component.ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ApiResponse } from '../../../core/models/common-model';
import { UpdateTeamBrandingDTO, MyTeamResponse } from '../../../core/models/team-model';
import { TeamService } from '../../../core/services/team-service';
import { ImageStorageService } from '../../../core/services/image-storage.service';

@Component({
  selector: 'app-edit-branding',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule
  ],
  templateUrl: './edit-branding.html',
  styleUrls: ['./edit-branding.css'],
})
export class EditBrandingComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);
  private teamSrv = inject(TeamService);
  private imageStorage = inject(ImageStorageService);

  teamId = signal<number>(0);
  saving = signal(false);
  uploading = signal(false);
  uploadingThumb = signal(false);
  computingA = signal(false);
  computingB = signal(false);

  selectedFile = signal<File | null>(null);
  imagePreview = signal<string | null>(null);
  thumbnailPreview = signal<string | null>(null);

  form = this.fb.group({
    teamName: ['', [Validators.required, Validators.minLength(2)]],
    teamImageUrl: [''],
    teamImageWidth: [''],
    teamImageHeight: [''],
    teamImageBytes: [''],
    thumbnailUrl: [''],
    thumbnailWidth: [''],
    thumbnailHeight: [''],
    thumbnailBytes: [''],
  });

  async ngOnInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.teamId.set(id);
    localStorage.setItem('xnf.currentTeamId', String(id));
    
    this.teamSrv.getMyTeam(id).subscribe({
      next: async (res: ApiResponse<MyTeamResponse>) => {
        console.log('🔍 Respuesta completa de la API:', res);
        
        if (res?.data || (res as any)?.Data) {
          // El backend devuelve en PascalCase, acceder directamente
          const apiData = (res as any).Data || (res as any).data;
          console.log('🔍 Datos extraídos:', apiData);
          console.log('🔍 TeamName:', apiData.TeamName);
          
          // IMPORTANTE: Usar PascalCase como viene del backend
          this.form.patchValue({
            teamName: apiData.TeamName ?? apiData.teamName ?? '',  // ← Intentar ambos casos
            teamImageUrl: apiData.TeamImageUrl ?? apiData.teamImageUrl ?? '',
            thumbnailUrl: apiData.ThumbnailUrl ?? apiData.thumbnailUrl ?? ''
          });

          const imageUrl = apiData.TeamImageUrl ?? apiData.teamImageUrl;
          const thumbUrl = apiData.ThumbnailUrl ?? apiData.thumbnailUrl;

          // Cargar y calcular metadatos de imagen principal si existe
          if (imageUrl) {
            this.imagePreview.set(imageUrl);
            this.computingA.set(true);
            try {
              const mainMeta = await this.getImageMetaFromUrl(imageUrl);
              this.form.patchValue({
                teamImageWidth: mainMeta.width !== null ? String(mainMeta.width) : '',
                teamImageHeight: mainMeta.height !== null ? String(mainMeta.height) : '',
                teamImageBytes: mainMeta.bytes !== null ? String(mainMeta.bytes) : ''
              });
            } catch (error) {
              console.error('Error loading image metadata:', error);
            }
            this.computingA.set(false);
          }
          
          // Cargar y calcular metadatos de thumbnail si existe
          if (thumbUrl) {
            this.thumbnailPreview.set(thumbUrl);
            this.computingB.set(true);
            try {
              const thumbMeta = await this.getImageMetaFromUrl(thumbUrl);
              this.form.patchValue({
                thumbnailWidth: thumbMeta.width !== null ? String(thumbMeta.width) : '',
                thumbnailHeight: thumbMeta.height !== null ? String(thumbMeta.height) : '',
                thumbnailBytes: thumbMeta.bytes !== null ? String(thumbMeta.bytes) : ''
              });
            } catch (error) {
              console.error('Error loading thumbnail metadata:', error);
            }
            this.computingB.set(false);
          }
        }
      },
      error: (err) => {
        console.error('Error loading team data:', err);
        this.snack.open('No se pudo cargar la información del equipo.', 'Cerrar', { duration: 4000 });
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];

    const validTypes = ['image/jpeg', 'image/jpg', 'image/png'];
    if (!validTypes.includes(file.type)) {
      this.snack.open('Solo se permiten imágenes JPEG o PNG.', 'Cerrar', { duration: 4000 });
      input.value = '';
      return;
    }

    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
      this.snack.open('La imagen no puede superar los 5 MB.', 'Cerrar', { duration: 4000 });
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
    
    const reader = new FileReader();
    reader.onload = (e) => {
      const dataUrl = e.target?.result as string;
      const img = new Image();
      img.onload = () => {
        const w = img.naturalWidth;
        const h = img.naturalHeight;
        
        if (w < 300 || h < 300 || w > 1024 || h > 1024) {
          this.snack.open('Las dimensiones deben estar entre 300x300 y 1024x1024 píxeles.', 'Cerrar', { duration: 4000 });
          this.selectedFile.set(null);
          this.imagePreview.set(null);
          input.value = '';
          return;
        }
        
        this.imagePreview.set(dataUrl);
      };
      img.src = dataUrl;
    };
    reader.readAsDataURL(file);
  }

  // 1. Cambiar la generación del thumbnail para cumplir con las validaciones (mínimo 300x300)
  async uploadImage(): Promise<void> {
    const file = this.selectedFile();
    if (!file) {
      this.snack.open('Por favor selecciona una imagen primero.', 'Cerrar', { duration: 3000 });
      return;
    }

    this.uploading.set(true);

    try {
      const uploadResult = await this.imageStorage.uploadImage(file).toPromise();
      if (!uploadResult?.imageUrl) {
        throw new Error('No se recibió URL de la imagen.');
      }

      const mainImageUrl = uploadResult.imageUrl;
      this.form.patchValue({ teamImageUrl: mainImageUrl });

      this.computingA.set(true);
      const mainMeta = await this.getImageMetaFromUrl(mainImageUrl);
      this.form.patchValue({
        teamImageWidth: mainMeta.width !== null ? String(mainMeta.width) : '',
        teamImageHeight: mainMeta.height !== null ? String(mainMeta.height) : '',
        teamImageBytes: mainMeta.bytes !== null ? String(mainMeta.bytes) : ''
      });
      this.computingA.set(false);

      this.uploadingThumb.set(true);
      // CAMBIAR: Usar 320x320 para cumplir con el rango de validación (300-1024)
      const thumbnailFile = await this.generateThumbnail(file, 320, 320);
      const thumbResult = await this.imageStorage.uploadImage(thumbnailFile).toPromise();
      
      if (!thumbResult?.imageUrl) {
        throw new Error('No se pudo generar el thumbnail.');
      }

      const thumbUrl = thumbResult.imageUrl;
      this.form.patchValue({ thumbnailUrl: thumbUrl });

      const thumbReader = new FileReader();
      thumbReader.onload = (e) => {
        this.thumbnailPreview.set(e.target?.result as string);
      };
      thumbReader.readAsDataURL(thumbnailFile);

      this.computingB.set(true);
      const thumbMeta = await this.getImageMetaFromUrl(thumbUrl);
      this.form.patchValue({
        thumbnailWidth: thumbMeta.width !== null ? String(thumbMeta.width) : '',
        thumbnailHeight: thumbMeta.height !== null ? String(thumbMeta.height) : '',
        thumbnailBytes: thumbMeta.bytes !== null ? String(thumbMeta.bytes) : ''
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

  private generateThumbnail(file: File, targetWidth: number, targetHeight: number): Promise<File> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
          const canvas = document.createElement('canvas');
          canvas.width = targetWidth;
          canvas.height = targetHeight;
          const ctx = canvas.getContext('2d');
          
          if (!ctx) {
            reject(new Error('No se pudo crear contexto de canvas.'));
            return;
          }

          ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

          canvas.toBlob((blob) => {
            if (!blob) {
              reject(new Error('No se pudo generar thumbnail.'));
              return;
            }

            const thumbnailFile = new File(
              [blob], 
              `thumb_${file.name}`, 
              { type: file.type }
            );
            resolve(thumbnailFile);
          }, file.type, 0.9);
        };

        img.onerror = () => reject(new Error('Error al cargar la imagen.'));
        img.src = e.target?.result as string;
      };

      reader.onerror = () => reject(new Error('Error al leer el archivo.'));
      reader.readAsDataURL(file);
    });
  }

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
      teamImageWidth: meta.width !== null ? String(meta.width) : '',
      teamImageHeight: meta.height !== null ? String(meta.height) : '',
      teamImageBytes: meta.bytes !== null ? String(meta.bytes) : ''
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
      thumbnailWidth: meta.width !== null ? String(meta.width) : '',
      thumbnailHeight: meta.height !== null ? String(meta.height) : '',
      thumbnailBytes: meta.bytes !== null ? String(meta.bytes) : ''
    });
    this.computingB.set(false);
  }

  private toNumberOrUndef(x: any): number | undefined {
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

  // 2. Agregar validación antes de guardar
  save(): void {
    if (this.saving() || this.form.invalid) return;
    if (this.uploading() || this.uploadingThumb() || this.computingA() || this.computingB()) {
      this.snack.open('Espera a que se complete la carga de imágenes…', 'Cerrar', { duration: 3000 });
      return;
    }

    const v = this.form.value;

    const dto: UpdateTeamBrandingDTO = {
      ...(v.teamName?.trim() ? { teamName: v.teamName.trim() } : {})
    };

    if (v.teamImageUrl?.trim()) {
      Object.assign(dto, {
        teamImageUrl: v.teamImageUrl.trim(),
        ...(this.toNumberOrUndef(v.teamImageWidth) !== undefined ? { teamImageWidth: this.toNumberOrUndef(v.teamImageWidth)! } : {}),
        ...(this.toNumberOrUndef(v.teamImageHeight) !== undefined ? { teamImageHeight: this.toNumberOrUndef(v.teamImageHeight)! } : {}),
        ...(this.toNumberOrUndef(v.teamImageBytes) !== undefined ? { teamImageBytes: this.toNumberOrUndef(v.teamImageBytes)! } : {}),
        ...(v.thumbnailUrl?.trim() ? { thumbnailUrl: v.thumbnailUrl.trim() } : {}),
        ...(this.toNumberOrUndef(v.thumbnailWidth) !== undefined ? { thumbnailWidth: this.toNumberOrUndef(v.thumbnailWidth)! } : {}),
        ...(this.toNumberOrUndef(v.thumbnailHeight) !== undefined ? { thumbnailHeight: this.toNumberOrUndef(v.thumbnailHeight)! } : {}),
        ...(this.toNumberOrUndef(v.thumbnailBytes) !== undefined ? { thumbnailBytes: this.toNumberOrUndef(v.thumbnailBytes)! } : {}),
      });
    }

    // Validación de metadatos
    const must = (url?: string, w?: number, h?: number, b?: number) =>
      !url || (w && h && b && w > 0 && h > 0 && b > 0);
    if (!must(dto.teamImageUrl, dto.teamImageWidth, dto.teamImageHeight, dto.teamImageBytes) ||
        !must(dto.thumbnailUrl, dto.thumbnailWidth, dto.thumbnailHeight, dto.thumbnailBytes)) {
      this.snack.open('Imagen/thumbnail requieren Width/Height/Bytes válidos.', 'Cerrar', { duration: 4000 });
      return;
    }

    // Validación de dimensiones (300-1024)
    if (dto.teamImageWidth && (dto.teamImageWidth < 300 || dto.teamImageWidth > 1024)) {
      this.snack.open('El ancho de la imagen debe estar entre 300 y 1024 píxeles.', 'Cerrar', { duration: 4000 });
      return;
    }
    if (dto.teamImageHeight && (dto.teamImageHeight < 300 || dto.teamImageHeight > 1024)) {
      this.snack.open('El alto de la imagen debe estar entre 300 y 1024 píxeles.', 'Cerrar', { duration: 4000 });
      return;
    }
    if (dto.thumbnailWidth && (dto.thumbnailWidth < 300 || dto.thumbnailWidth > 1024)) {
      this.snack.open('El ancho del thumbnail debe estar entre 300 y 1024 píxeles.', 'Cerrar', { duration: 4000 });
      return;
    }
    if (dto.thumbnailHeight && (dto.thumbnailHeight < 300 || dto.thumbnailHeight > 1024)) {
      this.snack.open('El alto del thumbnail debe estar entre 300 y 1024 píxeles.', 'Cerrar', { duration: 4000 });
      return;
    }

    // AGREGAR LOGS PARA DEBUG
    console.log('🚀 DTO a enviar (camelCase):', dto);
    console.log('🚀 URL del endpoint:', `${this.teamId()}/branding`);

    this.saving.set(true);
    this.teamSrv.updateBranding(this.teamId(), dto).subscribe({
      next: (res) => {
        console.log('✅ Respuesta del servidor:', res);
        
        // MEJORAR: Verificar ambos formatos de respuesta
        const success = res?.success ?? (res as any)?.Success ?? false;
        const message = res?.message ?? (res as any)?.Message ?? 'Branding updated';
        
        if (success) {
          this.snack.open(message, 'Close', { duration: 2500 });
          this.router.navigate(['/teams', this.teamId(), 'my-team']);
        } else {
          this.snack.open(message, 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        }
        this.saving.set(false);
      },
      error: (err) => {
        console.error('❌ Error completo:', err);
        console.error('❌ Error.error:', err?.error);
        
        const e = err?.error ?? err;
        
        // Extraer errores de validación
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
        
        // Si no hay errores de validación, buscar mensaje genérico
        if (!msg) {
          msg = e?.message ?? e?.Message ?? e?.title ?? e?.detail ?? 'Could not update branding';
        }

        this.snack.open(msg, 'Close', { duration: 6000, panelClass: ['error-snackbar'] });
        this.saving.set(false);
      }
    });
  }
}