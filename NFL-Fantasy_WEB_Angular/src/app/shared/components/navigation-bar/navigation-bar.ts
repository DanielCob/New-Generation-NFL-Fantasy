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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar'; // 👈
import { AuthService, AuthSession } from '../../../core/services/auth.service';
import { Subscription } from 'rxjs';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SetTeamIdDialog } from '../set-team-id-dialog/set-team-id-dialog';
import { SelectTeamDialog } from '../select-team-dialog/select-team-dialog';

@Component({
  selector: 'app-navigation-bar',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatDividerModule,
    MatSnackBarModule,
    MatDialogModule
     // 👈
  ],
  templateUrl: './navigation-bar.html',
  styleUrl: './navigation-bar.css'
})
export class NavigationBar implements OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  session = signal<AuthSession | null>(this.auth.session);
  private sub: Subscription;

  constructor() {
    this.sub = this.auth.session$.subscribe(s => this.session.set(s));
  }
  ngOnDestroy(): void { this.sub?.unsubscribe(); }

  isLoggedIn = computed(() => !!this.session()?.SessionID);
  userName = computed(() => this.session()?.Name ?? 'User');

  logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  // ---------- NUEVAS NAVEGACIONES ----------
  private getCurrentTeamId(): number | null {
    const raw = localStorage.getItem('xnf.currentTeamId');
    const id = raw ? Number(raw) : NaN;
    return Number.isFinite(id) && id > 0 ? id : null;
  }

  navigateToNFLTeamsList(): void {
    this.router.navigate(['/nfl-teams']);
  }
  navigateToMyTeam(): void {
    this.router.navigate(['/my-team']); // 👈 sin :id
  }
  navigateToEditBranding(): void {
    this.router.navigate(['/teams/edit-branding']); // 👈 sin :id
  }
  navigateToManageRoster(): void {
    this.router.navigate(['/teams/manage-roster']); // 👈 sin :id
  }



  openSetTeamIdDialog(): void {
    const ref = this.dialog.open(SelectTeamDialog, { width: '420px' });
    ref.afterClosed().subscribe((id?: number) => {
      if (!id) return;
      // ya se guardó en el dialog, pero por si acaso:
      localStorage.setItem('xnf.currentTeamId', String(id));
      this.router.navigate(['/teams', id, 'my-team']);
    });
  }

  // --------- ya existentes ----------
  navigateToProfile(): void { this.router.navigate(['/profile/header']); }
  navigateToSettings(): void { this.router.navigate(['/settings']); }
  navigateToFullProfile(): void { this.router.navigate(['/profile/full-profile']); }
  navigateToSessions(): void { this.router.navigate(['/profile/sessions']); }
}
