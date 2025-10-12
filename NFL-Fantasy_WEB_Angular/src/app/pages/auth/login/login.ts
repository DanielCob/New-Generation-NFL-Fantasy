// src/app/pages/login/login.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest, LoginResponse } from '../../../core/models/auth.model';

@Component({
  selector: 'app-login',
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
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  hidePassword = signal(true);
  isLoading = signal(false);

  loginForm: FormGroup = this.fb.group<LoginRequest>({
    email: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] })
  } as any);

  togglePasswordVisibility(): void {
    this.hidePassword.update(v => !v);
  }

  onSubmit(): void {
    if (this.isLoading()) return;

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      this.snackBar.open('Por favor completa los campos requeridos.', 'Cerrar', { duration: 3000, panelClass: ['error-snackbar'] });
      return;
    }

    const payload: LoginRequest = this.loginForm.value as LoginRequest;

    this.isLoading.set(true);
    this.auth.login(payload).subscribe({
      next: (res: LoginResponse) => {
        if (res.Success) {
          // El servicio ya persiste la sesión internamente.
          this.snackBar.open(res.Message || 'Inicio de sesión exitoso', 'Cerrar', {
            duration: 2500, panelClass: ['success-snackbar']
          });
          // Navega a donde corresponda tras login
          this.router.navigateByUrl('/profile/header'); // ajusta la ruta si usas otra
        } else {
          this.snackBar.open(res.Message || 'Credenciales inválidas', 'Cerrar', {
            duration: 4500, panelClass: ['error-snackbar']
          });
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.Message || err?.error?.message || 'No se pudo iniciar sesión';
        this.snackBar.open(msg, 'Cerrar', { duration: 5000, panelClass: ['error-snackbar'] });
        this.isLoading.set(false);
      }
    });
  }
}
