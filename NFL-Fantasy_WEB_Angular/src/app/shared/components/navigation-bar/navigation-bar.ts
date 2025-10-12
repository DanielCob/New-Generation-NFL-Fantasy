// src/app/shared/components/navigation-bar/navigation-bar.ts
import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService, AuthSession } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navigation-bar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule
  ],
  templateUrl: './navigation-bar.html',
  styleUrl: './navigation-bar.css'
})
export class NavigationBar {
  private auth = inject(AuthService);
  private router = inject(Router);

  // Sesión actual (observa cambios)
  session = signal<AuthSession | null>(this.auth.session);

  constructor() {
    // Reactivamente vincula el signal con el observable del servicio
    this.auth.session$.subscribe(s => this.session.set(s));
  }

  isLoggedIn = computed(() => !!this.session()?.SessionID);
  userName = computed(() => this.session()?.Name ?? 'User');

  logout(): void {
    this.auth.logout().subscribe(() => {
      this.router.navigate(['/login']);
    });
  }

  navigateToProfile(): void {
    this.router.navigate(['/profile/header']);
  }

  navigateToSettings(): void {
    this.router.navigate(['/settings']);
  }
}
