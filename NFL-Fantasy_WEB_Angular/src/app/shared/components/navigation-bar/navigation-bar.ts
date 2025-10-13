// src/app/shared/components/navigation-bar/navigation-bar.ts
import { Component, computed, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService, AuthSession } from '../../../core/services/auth.service';
import { Subscription } from 'rxjs';

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
export class NavigationBar implements OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);

  // Sesión actual (observa cambios)
  session = signal<AuthSession | null>(this.auth.session);
  private sub: Subscription;

  constructor() {
    // Vincula el signal con el observable del servicio
    this.sub = this.auth.session$.subscribe(s => this.session.set(s));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  isLoggedIn = computed(() => !!this.session()?.SessionID);
  userName = computed(() => this.session()?.Name ?? 'User');

  logout(): void {
    // ✅ Redirige al login tanto si el endpoint responde OK como si falla
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  navigateToProfile(): void {
    this.router.navigate(['/profile/header']);
  }
  
  navigateToFullProfile(): void {
  this.router.navigate(['/profile/full-profile']);
}
  navigateToSessions(): void {
    this.router.navigate(['/profile/sessions']);
  }

}
