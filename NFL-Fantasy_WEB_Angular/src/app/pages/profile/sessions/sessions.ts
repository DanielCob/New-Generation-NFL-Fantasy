// src/app/pages/profile/sessions/sessions.ts
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { UserService } from '../../../core/services/user.service';
import { UserSession } from '../../../core/models/user.model';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './sessions.html',
  styleUrls: ['./sessions.css'],
})
export class Sessions implements OnInit {
  private userService = inject(UserService);

  loading = signal(true);
  sessions = signal<UserSession[]>([]);

  displayedColumns = ['SessionID', 'CreatedAt', 'LastActivityAt', 'ExpiresAt', 'IsValid'];

  ngOnInit(): void {
    this.userService.getActiveSessions().subscribe({
      next: (rows) => {
        this.sessions.set(rows ?? []);
        this.loading.set(false);
      },
      error: () => {
        // El error-interceptor ya muestra el mensaje; aquí sólo cortamos el loading
        this.loading.set(false);
      }
    });
  }
}
