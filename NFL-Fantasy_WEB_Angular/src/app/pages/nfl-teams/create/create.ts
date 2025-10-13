/**
 * CreateNFLTeamComponent
 * -----------------------------------------------------------------------------
 * - Form con TeamName y City obligatorios.
 * - Imagen y Thumbnail opcionales PERO si se envían URLs se exigen metadatos:
 *   Width/Height/Bytes (calculados automáticamente).
 */

import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NflTeamService } from '../../../core/services/nfl-team.service';
import { CreateNFLTeamDTO } from '../../../core/models/nfl-team.model';

@Component({
  selector: 'app-nfl-team-create',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatSnackBarModule, MatProgressSpinnerModule
  ],
  templateUrl: './create.html',
  styleUrls: ['./create.css']
})
export class CreateNFLTeamComponent {
  private fb = inject(FormBuilder);
  private nfl = inject(NflTeamService);
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  saving = signal(false);
  computingA = signal(false);   // TeamImage
  computingB = signal(false);   // Thumbnail

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
    if (this.computingA() || this.computingB()) {
      this.snack.open('Espera a que se calculen los metadatos de imagen…', 'Cerrar', { duration: 3000 });
      return;
    }

    const v = this.form.value;
    const dto: CreateNFLTeamDTO = {
      TeamName: String(v.teamName).trim(),
      City: String(v.city).trim(),
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

    // Validación fuerte: si hay URL, debe haber meta > 0
    const must = (url?: string, w?: number, h?: number, b?: number) =>
      !url || (w && h && b && w > 0 && h > 0 && b > 0);
    if (!must(dto.TeamImageUrl, dto.TeamImageWidth, dto.TeamImageHeight, dto.TeamImageBytes) ||
        !must(dto.ThumbnailUrl, dto.ThumbnailWidth, dto.ThumbnailHeight, dto.ThumbnailBytes)) {
      this.snack.open('Imagen/thumbnail requieren Width/Height/Bytes válidos.', 'Cerrar', { duration: 4000 });
      return;
    }

    this.saving.set(true);
    this.nfl.create(dto).subscribe({
      next: (res) => {
        const ok = (res as any)?.success ?? (res as any)?.Success;
        const msg = (res as any)?.message ?? (res as any)?.Message ?? 'Team created.';
        if (ok) {
          this.snack.open(msg, 'Cerrar', { duration: 3000 });
          this.router.navigate(['/nfl-teams']);
        } else {
          this.snack.open(msg, 'Cerrar', { duration: 5000 });
        }
        this.saving.set(false);
      },
      error: (err) => {
        console.error('create team error:', err);
        this.snack.open('No se pudo crear el equipo.', 'Cerrar', { duration: 5000 });
        this.saving.set(false);
      }
    });
  }

  goBack(): void { this.router.navigate(['/nfl-teams']); }

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