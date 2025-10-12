// src/app/pages/profile/full-profile/full-profile.ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';

import { UserService } from '../../../core/services/user.service';
import { UserProfile } from '../../../core/models/user.model';

@Component({
  selector: 'app-full-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatListModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatButtonModule,
  ],
  templateUrl: './full-profile.html',
  styleUrl: './full-profile.css'
})
export class FullProfile implements OnInit {
  private userSvc = inject(UserService);

  loading = signal(true);
  error   = signal<string | null>(null);
  profile = signal<UserProfile | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.userSvc.getHeader().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: (e) => {
        const msg =
          e?.error?.Message || e?.error?.message || 'No se pudo cargar el perfil.';
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }

  // Helpers para vista
  statusChipColor(status: number): 'primary' | 'accent' | 'warn' {
    // Ajusta según tus códigos: 1=Activa, 2=Pausada, 3=Cerrada… (ejemplo)
    if (status === 1) return 'primary';
    if (status === 2) return 'accent';
    return 'warn';
  }
}
