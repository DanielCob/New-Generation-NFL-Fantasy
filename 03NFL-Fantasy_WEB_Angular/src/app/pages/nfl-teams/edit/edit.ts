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

  teamId = signal<number | null>(null);
  loading = signal(true);
  saving = signal(false);

  computingA = signal(false);
  computingB = signal(false);

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
        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Failed to load team.', 'Close', { duration: 3000 });
        this.router.navigate(['/nfl-teams']);
      }
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
    if (this.computingA() || this.computingB()) {
      this.snack.open('Espera a que se calculen los metadatos de imagen…', 'Cerrar', { duration: 3000 });
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
