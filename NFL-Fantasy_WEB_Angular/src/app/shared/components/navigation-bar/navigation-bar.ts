// src/app/shared/components/navigation-bar/navigation-bar.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-navigation-bar',
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
  private auth = inject(Auth);
  private router = inject(Router);

  currentUser = this.auth.currentUser;
  userType = this.auth.userType;

  getNavigationItems() {
    const userType = this.userType();
    
    switch (userType) {
      case 'ADMIN':
        return [
          { label: 'Dashboard', route: '/admin', icon: 'dashboard' },
          { label: 'Section 1', route: '/admin/section1', icon: 'folder' },
          { label: 'Section 2', route: '/admin/section2', icon: 'settings' },
          { label: 'Section 3', route: '/admin/section3', icon: 'analytics' }
        ];
      case 'ENGINEER':
        return [
          { label: 'Dashboard', route: '/engineer', icon: 'dashboard' },
          { label: 'Section A', route: '/engineer/sectionA', icon: 'build' },
          { label: 'Section B', route: '/engineer/sectionB', icon: 'code' },
          { label: 'Section C', route: '/engineer/sectionC', icon: 'layers' }
        ];
      case 'CLIENT':
        return [
          { label: 'Dashboard', route: '/client', icon: 'dashboard' },
          { label: 'Option 1', route: '/client/option1', icon: 'home' },
          { label: 'Option 2', route: '/client/option2', icon: 'explore' },
          { label: 'Option 3', route: '/client/option3', icon: 'bookmark' }
        ];
      default:
        return [];
    }
  }

  logout(): void {
    this.auth.logout().subscribe();
  }

  navigateToProfile(): void {
    const userType = this.userType();
    this.router.navigate([`/${userType?.toLowerCase()}/profile`]);
  }

  navigateToSettings(): void {
    const userType = this.userType();
    this.router.navigate([`/${userType?.toLowerCase()}/settings`]);
  }
}