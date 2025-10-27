import { Component, computed, inject, signal, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SelectTeamDialog } from '../select-team-dialog/select-team-dialog';
import { AuthService, AuthSession } from '../../../core/services/auth-service';
import { UserService } from '../../../core/services/user-service';

type LeagueRow = { LeagueID: number; LeagueName: string; Status: number };


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
  private users = inject(UserService);

  session = signal<AuthSession | null>(this.auth.session);
  private sub: Subscription;

    // estado del dropdown "My Leagues"
  leagues = signal<LeagueRow[]>([]);
  leaguesLoading = signal(false);
  leaguesError = signal<string | null>(null);

  constructor() {
      this.sub = this.auth.session$.subscribe(s => this.session.set(s));

  effect(() => {
    const s = this.session();
    if (s?.SessionID) {
      // evitá recargar si ya hay datos
      if (!this.leaguesLoading() && this.leagues().length === 0 && !this.leaguesError()) {
        this.loadMyLeagues();
      }
    } else {
      this.leagues.set([]);
      this.leaguesError.set(null);
      this.leaguesLoading.set(false);
    }
  });
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
    localStorage.getItem('xnf.currentTeamId')  // debe ser "1", "2", ...
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

  // navigation-bar.ts (solo agrega lo de League)
private getCurrentLeagueId(): number | null {
  const raw = localStorage.getItem('xnf.currentLeagueId');
  const id = raw ? Number(raw) : NaN;
  return Number.isFinite(id) && id > 0 ? id : null;
}

navigateToCreateLeague(): void {
  this.router.navigate(['/league/create']);
}

goLeague(path: 'summary'|'edit'|'members'|'teams'): void {
  const id = this.getCurrentLeagueId();
  if (!id) { this.snack.open('Seleccioná un League ID primero', 'OK', {duration:3000}); return; }
  this.router.navigate(['/league', id, path === 'edit' ? 'edit' : path]);
}
navigateToLeagues(): void {
  this.router.navigate(['/leagues']);
}
loadMyLeagues(): void {
    this.leaguesLoading.set(true);
    this.leaguesError.set(null);
    this.users.getProfile().subscribe({
      next: (p) => {
        // HOY: solo las que vienen como comisionado. FUTURO: todas las del user.
        const rows = (p?.CommissionedLeagues ?? []).map(x => ({
          LeagueID: x.LeagueID,
          LeagueName: x.LeagueName,
          Status: x.Status
        }));
        this.leagues.set(rows);
        this.leaguesLoading.set(false);
      },
      error: () => {
        this.leagues.set([]);
        this.leaguesLoading.set(false);
        this.leaguesError.set('No se pudieron cargar tus ligas');
      }
    });
  }

  selectLeague(l: LeagueRow): void {
    localStorage.setItem('xnf.currentLeagueId', String(l.LeagueID));
    localStorage.setItem('xnf.currentLeagueName', l.LeagueName);
    this.router.navigate(['/league', l.LeagueID, 'actions']);
  }
}


