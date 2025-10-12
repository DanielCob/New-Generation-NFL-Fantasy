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

import { AuthService } from '../../core/services/auth.service';
import { RegisterRequest, SimpleOkResponse } from '../../core/models/auth.model';

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

  // Opciones para LanguageCode (ajusta si tienes catálogo en backend)
  languages = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
  ];

  // Formulario alineado 1:1 con RegisterRequest
  // Nota: los metadatos de imagen son opcionales a nivel de UX, pero tu interfaz exige number.
  // Si no se proveen, enviaremos 0 para width/height/bytes y '' para url.
  registerForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    alias: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    passwordConfirm: ['', [Validators.required]],
    languageCode: ['en', [Validators.required]],

    // Imagen de perfil (opcionales). Si están vacíos, se mandan como 0/''.
    profileImageUrl: [''],
    profileImageWidth: [''],
    profileImageHeight: [''],
    profileImageBytes: [''],
  }, { validators: this.passwordMatchValidator });

  // --- Validadores ---
  private passwordMatchValidator(group: AbstractControl) {
    const p = group.get('password')?.value;
    const c = group.get('passwordConfirm')?.value;
    return p === c ? null : { passwordMismatch: true };
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(v => !v);
  }

  // --- Envío ---
  onSubmit(): void {
    if (this.isLoading()) return;

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.snackBar.open('Revisa los campos requeridos.', 'Cerrar', { duration: 3000, panelClass: ['error-snackbar'] });
      return;
    }

    const v = this.registerForm.value;

    // Normaliza campos opcionales de imagen a valores válidos para el backend
    const req: RegisterRequest = {
      name: v.name,
      email: v.email,
      alias: v.alias,
      password: v.password,
      passwordConfirm: v.passwordConfirm,
      languageCode: v.languageCode,

      profileImageUrl: (v.profileImageUrl ?? '').trim(),
      profileImageWidth: this.toNumberOrZero(v.profileImageWidth),
      profileImageHeight: this.toNumberOrZero(v.profileImageHeight),
      profileImageBytes: this.toNumberOrZero(v.profileImageBytes),
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
        const msg = err?.error?.Message || err?.error?.message || 'Error al registrar';
        this.snackBar.open(msg, 'Cerrar', { duration: 5000, panelClass: ['error-snackbar'] });
        this.isLoading.set(false);
      }
    });
  }

  // --- Helpers ---
  private toNumberOrZero(x: any): number {
    const n = Number(x);
    return Number.isFinite(n) ? n : 0;
  }
}
