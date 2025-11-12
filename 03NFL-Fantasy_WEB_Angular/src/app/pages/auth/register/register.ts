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

import { RegisterRequest, SimpleOkResponse } from '../../../core/models/auth-model';
import { AuthService } from '../../../core/services/auth-service';
import { UserService } from '../../../core/services/user-service';
import { ImageStorageService, UploadResponse } from '../../../core/services/image-storage.service';
import { EditUserProfileRequest } from '../../../core/models/user-model';

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
  private userService = inject(UserService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private imageStorage = inject(ImageStorageService);

  hidePassword = signal(true);
  isLoading = signal(false);
  profileImageUploading = signal(false);
  profileImageError = signal<string | null>(null);
  previewImageUrl = signal<string | null>(null);

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
    languageCode: ['en'],
    profileImageWidth: [''],
    profileImageHeight: [''],
    profileImageBytes: [''],
  }, { validators: this.passwordMatchValidator });

  private profileImageFile: File | null = null;

  private passwordMatchValidator(group: AbstractControl) {
    const p = group.get('password')?.value;
    const c = group.get('passwordConfirm')?.value;
    return p === c ? null : { passwordMismatch: true };
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(v => !v);
  }

  // --- Selección de archivo: valida tipo, tamaño y dimensiones (300-1024) ---
  async onProfileImageSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const clearSelection = () => {
      // Clear current selection so the same file can be re-selected
      if (input) input.value = '';
      this.profileImageFile = null;
      this.previewImageUrl.set(null);
      this.registerForm.patchValue({
        profileImageWidth: '',
        profileImageHeight: '',
        profileImageBytes: ''
      });
      // Ensure we are not stuck in any loading state related to image
      this.profileImageUploading.set(false);
    };

    // Reset previous error state on each selection
    this.profileImageError.set(null);

    if (!input.files || input.files.length === 0) {
      clearSelection();
      return;
    }

    const file = input.files[0];

    if (!['image/jpeg', 'image/png'].includes(file.type.toLowerCase())) {
      this.profileImageError.set('Solo se permiten imágenes JPEG o PNG.');
      clearSelection();
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.profileImageError.set('La imagen no puede superar 5 MB.');
      clearSelection();
      return;
    }

    // Obtener dimensiones
    const dims = await this.getImageMetaFromFile(file);
    if (!dims.width || !dims.height ||
        dims.width < 300 || dims.height < 300 ||
        dims.width > 1024 || dims.height > 1024) {
      this.profileImageError.set('Las dimensiones deben estar entre 300 × 300 px y 1024 × 1024 px.');
      clearSelection();
      return;
    }

    // Valid image -> persist selection
    this.profileImageError.set(null);
    this.profileImageFile = file;

    this.registerForm.patchValue({
      profileImageWidth: dims.width,
      profileImageHeight: dims.height,
      profileImageBytes: file.size
    });

    const dataUrl = await this.readFileAsDataUrl(file);
    this.previewImageUrl.set(dataUrl);
  }

  // --- Envío: register -> login silencioso -> upload -> updateProfile -> logout -> redirect ---
  onSubmit(): void {
    if (this.isLoading()) return;
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.snackBar.open('Revisa los campos requeridos.', 'Cerrar', { duration: 3000, panelClass: ['error-snackbar'] });
      return;
    }

    const v = this.registerForm.value;

    const req: RegisterRequest = {
      Name: v.name,
      Email: v.email,
      Password: v.password,
      PasswordConfirm: v.passwordConfirm,
      ...(v.alias?.trim() ? { Alias: v.alias.trim() } : {}),
      ...(v.languageCode ? { LanguageCode: v.languageCode } : {}),
    };

    this.isLoading.set(true);

    // 1) Registrar
    this.auth.register(req).subscribe({
      next: (res: SimpleOkResponse) => {
        if (res.success) {
          // 2) Login silencioso
          const loginPayload = { Email: v.email, Password: v.password };
          this.auth.login(loginPayload as any).subscribe({
            next: () => {
              // ahora estamos logueados en cliente (session persisted by AuthService)
              // 3) Si hay file -> subir y asignar
              if (this.profileImageFile) {
                this.profileImageUploading.set(true);
                this.imageStorage.uploadImage(this.profileImageFile).subscribe({
                  next: (upload: UploadResponse) => {
                    // 4) Asignar mediante UserService.updateProfile (igual al profile-header)
                    const updateBody: EditUserProfileRequest = {
                      Name: v.name,
                      Alias: v.alias,
                      LanguageCode: v.languageCode,
                      ProfileImageUrl: upload.imageUrl,
                      ProfileImageWidth: Number(v.profileImageWidth),
                      ProfileImageHeight: Number(v.profileImageHeight),
                      ProfileImageBytes: Number(v.profileImageBytes)
                    } as EditUserProfileRequest;

                    this.userService.updateProfile(updateBody).subscribe({
                      next: () => {
                        // 5) logout silencioso y redirect
                        this.finalizeAfterProfileAssign();
                      },
                      error: (err: any) => {
                        console.error('Error updateProfile (register flow):', err);
                        this.profileImageUploading.set(false);
                        this.snackBar.open('Usuario registrado pero no se pudo asignar la imagen.', 'Cerrar', { duration: 5000, panelClass: ['error-snackbar'] });
                        // intentar limpiar sesión local
                        this.auth.logout().subscribe({
                          next: () => { this.isLoading.set(false); this.router.navigateByUrl('/'); },
                          error: () => { this.isLoading.set(false); this.router.navigateByUrl('/'); }
                        });
                      }
                    });
                  },
                  error: (err: any) => {
                    console.error('Error uploading image after silent login:', err);
                    this.profileImageUploading.set(false);
                    this.snackBar.open('Registro correcto pero error al subir la imagen.', 'Cerrar', { duration: 5000, panelClass: ['error-snackbar'] });
                    // cleanup session
                    this.auth.logout().subscribe({
                      next: () => { this.isLoading.set(false); this.router.navigateByUrl('/'); },
                      error: () => { this.isLoading.set(false); this.router.navigateByUrl('/'); }
                    });
                  }
                });
              } else {
                // no hay imagen: solo desloguear y redirigir
                this.auth.logout().subscribe({
                  next: () => {
                    this.isLoading.set(false);
                    this.snackBar.open('Registro completado.', 'Cerrar', { duration: 2000, panelClass: ['success-snackbar'] });
                    this.router.navigateByUrl('/');
                  },
                  error: () => {
                    this.isLoading.set(false);
                    this.router.navigateByUrl('/');
                  }
                });
              }
            },
            error: (err: any) => {
              console.error('Silent login failed:', err);
              this.isLoading.set(false);
              this.snackBar.open('Registro completado. Inicia sesión para agregar tu imagen.', 'Cerrar', { duration: 5000, panelClass: ['info-snackbar'] });
              this.router.navigateByUrl('/login');
            }
          });
        } else {
          this.snackBar.open(res.message || 'No se pudo completar el registro.', 'Cerrar', { duration: 4500, panelClass: ['error-snackbar'] });
          this.isLoading.set(false);
        }
      },
      error: (err: any) => {
        console.error('Register error:', err);
        const msg = this.extractApiError(err);
        this.snackBar.open(msg || 'No se pudo completar el registro.', 'Cerrar', { duration: 6000, panelClass: ['error-snackbar'] });
        this.isLoading.set(false);
      }
    });
  }

  private finalizeAfterProfileAssign(): void {
    this.profileImageUploading.set(false);
    this.auth.logout().subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Registro completado y imagen asignada.', 'Cerrar', { duration: 2500, panelClass: ['success-snackbar'] });
        this.router.navigateByUrl('/');
      },
      error: (err: any) => {
        console.error('Error logout after assigning profile image:', err);
        this.isLoading.set(false);
        this.router.navigateByUrl('/');
      }
    });
  }

  private async getImageMetaFromFile(file: File): Promise<{ width: number | null; height: number | null }> {
    return new Promise((resolve) => {
      const img = new Image();
      const url = URL.createObjectURL(file);
      img.onload = () => {
        const w = img.naturalWidth || null;
        const h = img.naturalHeight || null;
        URL.revokeObjectURL(url);
        resolve({ width: w, height: h });
      };
      img.onerror = () => {
        URL.revokeObjectURL(url);
        resolve({ width: null, height: null });
      };
      img.src = url;
    });
  }

  private readFileAsDataUrl(file: File): Promise<string | null> {
    return new Promise((resolve) => {
      const fr = new FileReader();
      fr.onload = () => resolve(fr.result as string);
      fr.onerror = () => resolve(null);
      fr.readAsDataURL(file);
    });
  }

  private extractApiError(err: any): string {
    if (!err) return 'Error inesperado';
    const http = err;
    if (http.status === 0) return 'No se pudo conectar con el servidor.';
    const e = http.error ?? http;

    if (e && typeof e === 'object' && ('message' in e || 'Message' in e)) {
      return (e.message ?? e.Message) as string;
    }

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

    if (typeof e === 'string') return e;

    return `Error ${http.status || ''}`.trim();
  }
}
