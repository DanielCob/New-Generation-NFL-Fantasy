// src/app/pages/engineer-dashboard/engineer-dashboard.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatGridListModule } from '@angular/material/grid-list';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-engineer-dashboard',
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatGridListModule
  ],
  templateUrl: './engineer-dashboard.html',
  styleUrl: './engineer-dashboard.css'
})
export class EngineerDashboard {
  auth = inject(Auth);
  
  currentUser = this.auth.currentUser;
  
  menuItems = [
    { title: 'Projects', icon: 'engineering', description: 'View your projects', color: 'primary' },
    { title: 'Tasks', icon: 'assignment', description: 'Manage tasks', color: 'accent' },
    { title: 'Team', icon: 'groups', description: 'Team collaboration', color: 'warn' },
    { title: 'Documents', icon: 'description', description: 'Technical documents', color: 'primary' },
    { title: 'Schedule', icon: 'calendar_today', description: 'View schedule', color: 'accent' },
    { title: 'Resources', icon: 'folder', description: 'Engineering resources', color: 'warn' }
  ];

  handleAction(item: any): void {
    console.log('Action clicked:', item.title);
    // Placeholder for future navigation
  }
}