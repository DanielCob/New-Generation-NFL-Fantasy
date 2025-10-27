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
import { AuthService } from '../../../core/services/auth-service';

@Component({
  selector: 'app-request-reset',
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
  templateUrl: './request-reset.html',
  styleUrl: './request-reset.css'
})
export class RequestReset {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private router = inject(Router);

  isLoading = signal(false);

  form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit(): void {
    if (this.isLoading()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const email = this.form.value.email as string;

    this.isLoading.set(true);
    this.auth.requestReset(email).subscribe({
      next: () => this.showGenericMessageAndGoLogin(),
      error: (err) => {
        console.error('request-reset error:', err);
        this.showGenericMessageAndGoLogin();
      }
    });
  }

  private showGenericMessageAndGoLogin(): void {
    this.isLoading.set(false);
    this.snack.open(
      'Si la cuenta existe, será desbloqueada. Intenta iniciar sesión nuevamente.',
      'Cerrar',
      { duration: 5000 }
    );
    // opcional: redirigir al login tras unos ms
    setTimeout(() => this.router.navigate(['/login']), 300);
  }
}
