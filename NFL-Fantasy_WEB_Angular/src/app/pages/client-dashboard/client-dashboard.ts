// src/app/pages/client-dashboard/client-dashboard.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatGridListModule } from '@angular/material/grid-list';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-client-dashboard',
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatGridListModule
  ],
  templateUrl: './client-dashboard.html',
  styleUrl: './client-dashboard.css'
})
export class ClientDashboard {
  auth = inject(Auth);
  
  currentUser = this.auth.currentUser;
  
  menuItems = [
    { title: 'Services', icon: 'home_repair_service', description: 'Browse services', color: 'primary' },
    { title: 'Orders', icon: 'shopping_cart', description: 'View your orders', color: 'accent' },
    { title: 'Support', icon: 'support_agent', description: 'Get help', color: 'warn' },
    { title: 'Profile', icon: 'account_circle', description: 'Manage profile', color: 'primary' },
    { title: 'Billing', icon: 'payment', description: 'Billing information', color: 'accent' },
    { title: 'Notifications', icon: 'notifications', description: 'Your notifications', color: 'warn' }
  ];

  handleAction(item: any): void {
    console.log('Action clicked:', item.title);
    // Placeholder for future navigation
  }
}