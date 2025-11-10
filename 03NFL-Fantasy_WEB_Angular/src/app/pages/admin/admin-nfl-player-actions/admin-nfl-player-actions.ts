import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-nfl-player-actions',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './admin-nfl-player-actions.html',
  styleUrls: ['./admin-nfl-player-actions.css']
})
export class NFLPlayerActionsPage {
  private router = inject(Router);
  private snack = inject(MatSnackBar);

  /**
   * Redirige según la acción seleccionada
   * @param action 'create' | 'list' | 'edit'
   */
  goTo(action: 'create' | 'list' | 'edit'): void {
  let route = '';
  let queryParams: any = null;

  switch (action) {
    case 'create':
      route = '/admin/nfl-player-create';
      break;

    case 'list':
      route = '/admin/nfl-player-list';
      break;
  }

  this.snack.open('Loading section...', 'OK', { duration: 1000 });

  // ✅ navegación segura
  this.router.navigate([route], { queryParams }).catch(err => {
    console.error('Navigation error:', err);
    this.snack.open('Error while navigating', 'OK', { duration: 3000 });
  });
}

}
