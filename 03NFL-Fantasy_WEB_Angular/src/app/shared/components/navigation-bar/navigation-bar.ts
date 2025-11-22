import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule }  from '@angular/material/button';
import { MatIconModule }    from '@angular/material/icon';
import { MatMenuModule }    from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar }      from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { NavbarFacade } from '../../../pages/_facades/navbar.facade';
import { LeagueContextService } from '../../../core/services/context/league-context.service';
import { SelectTeamDialog } from '../select-team-dialog/select-team-dialog';
import { AuthService } from '../../../core/services/auth-service';

@Component({
  selector: 'app-navigation-bar',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatDividerModule,
    MatDialogModule
  ],
  templateUrl: './navigation-bar.html',
  styleUrl: './navigation-bar.css'
})
export class NavigationBar {
  private router = inject(Router);
  private snack  = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private facade = inject(NavbarFacade);
  private ctx    = inject(LeagueContextService);
  private auth   = inject(AuthService); // mantenemos logout real de tu versión actual

  // señales desde la fachada
  session        = this.facade.session;
  isAdmin        = this.facade.isAdmin;
  leagues        = this.facade.leagues;
  leaguesLoading = this.facade.leaguesLoading;
  leaguesError   = this.facade.leaguesError;

  // derivados de UI
  isLoggedIn = computed(() => !!this.session()?.SessionID);
  userName   = computed(() => this.session()?.Name ?? 'User');

  // ---- acciones de sesión ----
  logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  // ---- navegación “pura” (se mantiene tu mapeo actual) ----
  navigateToNFLTeamsList()   { this.router.navigate(['/nfl-teams']); }
  navigateToMyTeam()         { this.router.navigate(['/my-team']); }
  navigateToEditBranding()   { this.router.navigate(['/teams/edit-branding']); }
  navigateToManageRoster()   { this.router.navigate(['/teams/manage-roster']); }
  navigateToLeagueDirectory(){ this.router.navigate(['/league/directory']); }
  navigateToSeasonsAdmin()   { this.router.navigate(['/seasons/admin']); }
  navigateToCreateLeague()   { this.router.navigate(['/league/create']); }
  navigateToLeagues()        { this.router.navigate(['/leagues']); }
  navigateProfileHeader()    { this.router.navigate(['/profile/header']); }
  navigateSettings()         { this.router.navigate(['/settings']); }
  navigateFullProfile()      { this.router.navigate(['/profile/full-profile']); }
  navigateSessions()         { this.router.navigate(['/profile/sessions']); }

  // ---- helpers de selección ----
  openSetTeamIdDialog(): void {
    const ref = this.dialog.open(SelectTeamDialog, { width: '420px' });
    ref.afterClosed().subscribe((id?: number) => {
      if (!id) return;
      this.ctx.setTeam(id);
      this.router.navigate(['/teams', id, 'my-team']);
    });
  }

  selectLeague(l: { LeagueID: number; LeaguePublicID: number; LeagueName: string }): void {
    console.log('🎯 [selectLeague] Liga seleccionada:', l);
    console.log('🎯 [selectLeague] LeagueID:', l.LeagueID);
    console.log('🎯 [selectLeague] LeaguePublicID:', l.LeaguePublicID);
    console.log('🎯 [selectLeague] LeagueName:', l.LeagueName);
    
    if (!l.LeaguePublicID) {
      console.error('❌ [selectLeague] LeaguePublicID es undefined!');
      this.snack.open('Error: LeaguePublicID no disponible', 'OK', { duration: 3000 });
      return;
    }
    
    this.ctx.setLeague(l.LeagueID);
    this.router.navigate(['/league', l.LeaguePublicID, 'actions']);
  }

  goLeague(path: 'summary'|'edit'|'members'|'teams'): void {
    const id = this.ctx.currentLeagueId();
    if (!id) { this.snack.open('Seleccioná un League ID primero', 'OK', { duration: 3000 }); return; }
    this.router.navigate(['/league', id, path === 'edit' ? 'edit' : path]);
  }

  loadMyLeagues(): void {
    if (!this.leaguesLoading()) {
      console.log('✅ [NavigationBar] Llamando a facade.loadMyLeagues()');
      this.facade.loadMyLeagues();
    } else {
      console.log('⏳ [NavigationBar] Ya está cargando, no se hace nada');
    }
  }

  // ---- Admin Capabilities (ajustado a tus rutas) ----
  adminCapability(action: 'manage-season' | 'manage-nfl-players'): void {
    const route = action === 'manage-nfl-players'
      ? '/admin/nfl-player-actions'
      : '/seasons/admin';

    this.router.navigate([route]).catch(() => {
      this.snack.open('Navigation error', 'OK', { duration: 2500 });
    });
  }
}
