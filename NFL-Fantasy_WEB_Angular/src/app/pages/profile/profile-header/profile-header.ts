/**
 * profile-header.ts
 * -----------------------------------------------------------------------------
 * NUEVO:
 * - Se muestra imagen del usuario (si existe ProfileImageUrl).
 * - Si /User/header no trae la URL, intentamos una sola vez /User/profile
 *   para enriquecer con ProfileImageUrl desde el SP.
 * - Prefill de la URL al entrar a editar si ya existe imagen.
 * - Manejo de error de carga (oculta la imagen si falla).
 *
 * Además mantiene la lógica previa de metadatos de imagen (W/H/Bytes)
 * obligatorios cuando se envía una URL al guardar.
 */

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { UserService } from '../../../core/services/user.service';
import { UserProfile, EditUserProfileRequest } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile-header',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatSelectModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile-header.html',
  styleUrl: './profile-header.css'
})
export class ProfileHeader implements OnInit {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  private snack = inject(MatSnackBar);

  // Estado general
  loading = signal(true);
  saving  = signal(false);
  editing = signal(false);

  // Estado imagen
  computingImageMeta = signal(false); // cálculo W/H/Bytes
  showAvatar = signal(true);          // oculta avatar si la carga falla

  profile = signal<UserProfile | null>(null);
  triedFullProfile = false;           // evita pedir /profile más de una vez

  // Idiomas de ejemplo
  languages = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
  ];

  // Form con campos de metadatos (solo lectura) para edición
  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    alias: ['', [Validators.required, Validators.minLength(2)]],
    languageCode: ['en', [Validators.required]],

    profileImageUrl: [''],
    profileImageWidth: [''],
    profileImageHeight: [''],
    profileImageBytes: [''],
  });

  ngOnInit(): void {
    this.loadHeader();
  }

  /** Carga datos desde /User/header y, si no viene la imagen, intenta /User/profile */
  private loadHeader(): void {
    this.loading.set(true);
    this.userService.getHeader().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.patchFormFromProfile(p);
        this.loading.set(false);

        // Si no hay URL en header, intenta una sola vez el perfil completo
        const noImg = !p.ProfileImageUrl || !String(p.ProfileImageUrl).trim();
        if (noImg && !this.triedFullProfile) {
          this.triedFullProfile = true;
          this.userService.getProfile().subscribe({
            next: (fp) => {
              if (fp?.ProfileImageUrl) {
                // Enriquecer el perfil actual con la URL
                const curr = this.profile();
                this.profile.set({ ...(curr || fp), ProfileImageUrl: fp.ProfileImageUrl });
              }
            },
            error: () => { /* silencioso */ }
          });
        }
      },
      error: (err) => {
        console.error('getHeader error:', err);
        this.snack.open('No se pudo cargar el perfil.', 'Cerrar', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  /** Sincroniza formularios desde el perfil actual */
  private patchFormFromProfile(p: UserProfile): void {
    this.form.patchValue({
      name: p.Name,
      alias: p.Alias,
      languageCode: p.LanguageCode,
      // Prefill de URL si existe (útil para editar)
      profileImageUrl: (p.ProfileImageUrl || '').toString(),
      profileImageWidth: '',
      profileImageHeight: '',
      profileImageBytes: '',
    });
    // Si cambiamos de perfil/URL, volvemos a mostrar el avatar
    this.showAvatar.set(true);
  }

  enableEdit(): void { this.editing.set(true); }

  cancelEdit(): void {
    const p = this.profile();
    if (p) this.patchFormFromProfile(p);
    this.editing.set(false);
  }

  /** Si el <img> falla, ocultamos el avatar para no mostrar ícono roto */
  onAvatarError(): void {
    this.showAvatar.set(false);
  }

  /** Cálculo de metadatos al cambiar URL en edición */
  async onProfileImageUrlChanged(): Promise<void> {
    const url = String(this.form.get('profileImageUrl')?.value ?? '').trim();

    if (!url) {
      this.form.patchValue({
        profileImageWidth: '',
        profileImageHeight: '',
        profileImageBytes: ''
      });
      return;
    }

    this.computingImageMeta.set(true);
    try {
      const { width, height, bytes } = await this.getImageMetaFromUrl(url);
      this.form.patchValue({
        profileImageWidth: width ?? '',
        profileImageHeight: height ?? '',
        profileImageBytes: bytes ?? ''
      });
    } catch {
      this.form.patchValue({
        profileImageWidth: '',
        profileImageHeight: '',
        profileImageBytes: ''
      });
    } finally {
      this.computingImageMeta.set(false);
    }
  }

  /** Construye el payload para PUT /User/profile (con validación de metadatos si hay URL) */
  private buildRequestOrThrow(): EditUserProfileRequest {
    const v = this.form.value;

    const name = String(v.name ?? '').trim();
    const alias = String(v.alias ?? '').trim();
    const language = String(v.languageCode ?? '').trim();
    const url = String(v.profileImageUrl ?? '').trim();

    const req: EditUserProfileRequest = { Name: name, Alias: alias, LanguageCode: language };

    if (url) {
      const w = this.toNumberOrUndefined(v.profileImageWidth);
      const h = this.toNumberOrUndefined(v.profileImageHeight);
      const b = this.toNumberOrUndefined(v.profileImageBytes);
      if (!w || !h || !b || w <= 0 || h <= 0 || b <= 0) {
        throw new Error('La imagen requiere Width/Height/Bytes válidos.');
      }
      req.ProfileImageUrl = url;
      req.ProfileImageWidth = w;
      req.ProfileImageHeight = h;
      req.ProfileImageBytes = b;
    }

    return req;
  }

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) return;
    if (this.computingImageMeta()) {
      this.snack.open('Espera a que se calculen los metadatos de la imagen…', 'Cerrar', { duration: 3000 });
      return;
    }

    let body: EditUserProfileRequest;
    try { body = this.buildRequestOrThrow(); }
    catch (e: any) {
      this.snack.open(e?.message || 'Datos de imagen inválidos.', 'Cerrar', { duration: 4000 });
      return;
    }

    this.saving.set(true);
    this.userService.updateProfile(body).subscribe({
      next: (res) => {
        this.saving.set(false);
        const ok = (res as any)?.success ?? (res as any)?.Success;
        const msg = (res as any)?.message ?? (res as any)?.Message ?? 'Perfil actualizado.';

        if (ok) {
          this.snack.open(msg, 'Cerrar', { duration: 3000 });

          // Refresca valores locales visibles
          const p = this.profile();
          if (p) {
            this.profile.set({
              ...p,
              Name: body.Name,
              Alias: body.Alias,
              LanguageCode: body.LanguageCode,
              // Si se envió una nueva URL, reflejarla
              ProfileImageUrl: body.ProfileImageUrl ?? p.ProfileImageUrl
            });
          }
          this.editing.set(false);
        } else {
          this.snack.open(msg || 'No se pudo actualizar el perfil.', 'Cerrar', { duration: 4000 });
        }
      },
      error: (err) => {
        console.error('updateProfile error:', err);
        const msg =
          err?.error?.message ||
          err?.error?.Message ||
          'Error al actualizar el perfil';
        this.snack.open(msg, 'Cerrar', { duration: 5000 });
        this.saving.set(false);
      }
    });
  }

  // ===== Helpers (mismos que en register) =====

  private toNumberOrUndefined(x: any): number | undefined {
    const n = Number(x);
    return Number.isFinite(n) ? n : undefined;
  }

  private async getImageMetaFromUrl(url: string): Promise<{ width: number | null, height: number | null, bytes: number | null }> {
    const getDims = () => new Promise<{ width: number | null; height: number | null }>((resolve) => {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => resolve({ width: img.naturalWidth || null, height: img.naturalHeight || null });
      img.onerror = () => resolve({ width: null, height: null });
      img.src = url;
    });

    const getBytesViaHEAD = async () => {
      try {
        const r = await fetch(url, { method: 'HEAD', mode: 'cors' });
        const len = r.headers.get('content-length');
        return len ? Number(len) : null;
      } catch { return null; }
    };

    const dims = await getDims();
    let bytes = await getBytesViaHEAD();

    if (bytes == null) {
      try {
        const r = await fetch(url, { method: 'GET', mode: 'cors' });
        const b = await r.blob();
        bytes = b.size;
      } catch { bytes = null; }
    }

    return { width: dims.width, height: dims.height, bytes };
  }
}
