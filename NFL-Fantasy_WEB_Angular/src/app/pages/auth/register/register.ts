// src/app/pages/register/register.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest, SimpleOkResponse } from '../../../core/models/auth.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  hidePassword = signal(true);
  isLoading = signal(false);

  languages = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
  ];

  registerForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    alias: [''],
    password: ['', [Validators.required, Validators.minLength(6)]],
    passwordConfirm: ['', [Validators.required]],
    languageCode: [''],
    profileImageUrl: [''],
    profileImageWidth: [''],
    profileImageHeight: [''],
    profileImageBytes: [''],
  }, { validators: this.passwordMatchValidator });

  // ---- Valida que las contraseñas coincidan ----
  private passwordMatchValidator(group: AbstractControl) {
    const p = group.get('password')?.value;
    const c = group.get('passwordConfirm')?.value;
    return p === c ? null : { passwordMismatch: true };
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(v => !v);
  }

  // --- Se activa al cambiar el campo de URL ---
  async onProfileImageUrlChanged(): Promise<void> {
    const url = this.registerForm.get('profileImageUrl')?.value?.trim();
    if (!url) return;

    const { width, height, bytes } = await this.getImageMetaFromUrl(url);

    this.registerForm.patchValue({
      profileImageWidth: width ?? '',
      profileImageHeight: height ?? '',
      profileImageBytes: bytes ?? ''
    });
  }

  // --- Envío del formulario ---
onSubmit(): void {
    if (this.isLoading()) return;

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.snackBar.open('Revisa los campos requeridos.', 'Cerrar', {
        duration: 3000, panelClass: ['error-snackbar']
      });
      return;
    }

    const v = this.registerForm.value;

    // ✅ Construye el payload incluyendo SÓLO los opcionales con valor
    const req: RegisterRequest = {
      Name: v.name,
      Email: v.email,
      Password: v.password,
      PasswordConfirm: v.passwordConfirm,
      ...(v.alias?.trim() ? { Alias: v.alias.trim() } : {}),
      ...(v.languageCode ? { LanguageCode: v.languageCode } : {}),
      ...(v.profileImageUrl?.trim()
        ? {
            ProfileImageUrl: v.profileImageUrl.trim(),
            ...(this.toNumberOrUndefined(v.profileImageWidth) !== undefined
              ? { ProfileImageWidth: this.toNumberOrUndefined(v.profileImageWidth)! } : {}),
            ...(this.toNumberOrUndefined(v.profileImageHeight) !== undefined
              ? { ProfileImageHeight: this.toNumberOrUndefined(v.profileImageHeight)! } : {}),
            ...(this.toNumberOrUndefined(v.profileImageBytes) !== undefined
              ? { ProfileImageBytes: this.toNumberOrUndefined(v.profileImageBytes)! } : {}),
          }
        : {})
    };

    this.isLoading.set(true);
    this.auth.register(req).subscribe({
      next: (res: SimpleOkResponse) => {
        if (res.success) {
          this.snackBar.open(res.message || 'Registro exitoso. Ahora puedes iniciar sesión.', 'Cerrar', {
            duration: 3000, panelClass: ['success-snackbar']
          });
          this.router.navigateByUrl('/login');
        } else {
          this.snackBar.open(res.message || 'No se pudo completar el registro.', 'Cerrar', {
            duration: 4500, panelClass: ['error-snackbar']
          });
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Register error:', err);
        const msg = this.extractApiError(err);
        this.snackBar.open(msg || 'No se pudo completar el registro.', 'Cerrar', {
          duration: 6000, panelClass: ['error-snackbar']
        });
        this.isLoading.set(false);
      }
    });
  }

  // --- Helpers ---
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

  // --- Nueva función para mostrar errores claros del backend ---
  private extractApiError(err: any): string {
    if (!err) return 'Error inesperado';
    const http = err;
    if (http.status === 0) return 'No se pudo conectar con el servidor.';
    const e = http.error ?? http;

    // Caso: ApiResponse simple
    if (e && typeof e === 'object' && ('message' in e || 'Message' in e)) {
      return (e.message ?? e.Message) as string;
    }

    // Caso: ProblemDetails (ASP.NET Core)
    if (e && typeof e === 'object' && ('title' in e || 'detail' in e || 'errors' in e)) {
      const parts: string[] = [];
      if (e.title) parts.push(String(e.title));
      if (e.detail) parts.push(String(e.detail));
      if (e.errors && typeof e.errors === 'object') {
        const all = Object.values(e.errors as Record<string, string[]>).flat();
        if (all.length) parts.push(all.join(' | '));
      }
      if (parts.length) return parts.join(' - ');
    }

    // Caso: string plano
    if (typeof e === 'string') return e;

    return `Error ${http.status || ''}`.trim();
  }
  private toNumberOrZero(x: any): number {
    const n = Number(x);
    return Number.isFinite(n) ? n : 0;
  }
  

}
