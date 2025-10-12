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

  loading = signal(true);
  saving  = signal(false);
  editing = signal(false);

  profile = signal<UserProfile | null>(null);

  // Idiomas de ejemplo (si luego tienes catálogo, lo reemplazas)
  languages = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
  ];

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    alias: ['', [Validators.required, Validators.minLength(2)]],
    languageCode: ['en', [Validators.required]],
    profileImageUrl: ['']
  });

  ngOnInit(): void {
    this.loadHeader();
  }

  private loadHeader(): void {
    this.loading.set(true);
    this.userService.getHeader().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.form.patchValue({
          name: p.Name,
          alias: p.Alias,
          languageCode: p.LanguageCode,
          profileImageUrl: '' // si deseas precargar la URL, añade el campo a tu modelo header
        });
        this.loading.set(false);
      },
      error: (err) => {
        console.error('getHeader error:', err);
        this.snack.open('No se pudo cargar el perfil.', 'Cerrar', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  enableEdit(): void { this.editing.set(true); }
  cancelEdit(): void {
    const p = this.profile();
    if (p) {
      this.form.patchValue({
        name: p.Name,
        alias: p.Alias,
        languageCode: p.LanguageCode,
        profileImageUrl: ''
      });
    }
    this.editing.set(false);
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;

    const v = this.form.value;

    // Construimos el request con las MAYÚSCULAS que espera la API
    const body: EditUserProfileRequest = {
      Name: v.name!,
      Alias: v.alias!,
      LanguageCode: v.languageCode!,
      ProfileImageUrl: (v.profileImageUrl ?? '').trim(),
      ProfileImageWidth: 0,     // si no calculas metadatos, envía 0
      ProfileImageHeight: 0,
      ProfileImageBytes: 0
    };

    this.saving.set(true);
    this.userService.updateProfile(body).subscribe({
      next: (res) => {
        this.saving.set(false);
        if (res.success ?? (res as any).Success) {
          this.snack.open(res.message || 'Perfil actualizado.', 'Cerrar', { duration: 3000 });
          // actualiza datos visibles
          const p = this.profile();
          if (p) {
            this.profile.set({
              ...p,
              Name: body.Name,
              Alias: body.Alias,
              LanguageCode: body.LanguageCode
            });
          }
          this.editing.set(false);
        } else {
          this.snack.open(res.message || 'No se pudo actualizar el perfil.', 'Cerrar', { duration: 4000 });
        }
      },
      error: (err) => {
        console.error('updateProfile error:', err);
        const msg = err?.error?.message || err?.error?.Message || 'Error al actualizar el perfil';
        this.snack.open(msg, 'Cerrar', { duration: 5000 });
        this.saving.set(false);
      }
    });
  }
}
