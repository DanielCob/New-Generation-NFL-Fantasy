// src/app/pages/admin-dashboard/admin-dashboard.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatGridListModule } from '@angular/material/grid-list';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-admin-dashboard',
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatGridListModule
  ],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard {
  auth = inject(Auth);
  
  currentUser = this.auth.currentUser;
  
  menuItems = [
    { title: 'User Management', icon: 'people', description: 'Manage system users', color: 'primary' },
    { title: 'Reports', icon: 'assessment', description: 'View system reports', color: 'accent' },
    { title: 'Settings', icon: 'settings', description: 'System configuration', color: 'warn' },
    { title: 'Activity Log', icon: 'history', description: 'View system activity', color: 'primary' },
    { title: 'Analytics', icon: 'analytics', description: 'System analytics', color: 'accent' },
    { title: 'Security', icon: 'security', description: 'Security settings', color: 'warn' }
  ];

  handleAction(item: any): void {
    console.log('Action clicked:', item.title);
    // Placeholder for future navigation
  }
}