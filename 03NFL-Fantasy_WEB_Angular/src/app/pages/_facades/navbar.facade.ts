// pages/_facades/navbar.facade.ts
import { Injectable, effect, signal, computed, inject } from '@angular/core';
import { AuthService, AuthSession } from '../../core/services/auth-service';
import { UserService } from '../../core/services/user-service';
import { RoleService } from '../../core/services/authz/role.service';
import { environment } from '../../../environments/environment';

export type LeagueRow = { LeagueID: number; LeagueName: string; Status: number };

@Injectable({ providedIn: 'root' })
export class NavbarFacade {
  private auth  = inject(AuthService);
  private users = inject(UserService);
  private roles = inject(RoleService);

  readonly session        = signal<AuthSession | null>(this.auth.session);
  readonly leagues        = signal<LeagueRow[]>([]);
  readonly leaguesLoading = signal(false);
  readonly leaguesError   = signal<string | null>(null);

  private _isAdmin = signal(false);
  readonly isAdmin = computed(() => this._isAdmin() || environment.enableAdmin === true);

  constructor() {
    this.auth.session$.subscribe(s => this.session.set(s));

    effect(() => {
      const s = this.session();
      if (s?.SessionID) {
        this.refreshAdminFlag();
        if (!this.leaguesLoading() && this.leagues().length === 0 && !this.leaguesError()) {
          this.loadMyLeagues();
        }
      } else {
        this._isAdmin.set(false);
        this.leagues.set([]);
        this.leaguesError.set(null);
        this.leaguesLoading.set(false);
      }
    });
  }

  refreshAdminFlag(): void {
    this.users.getHeader().subscribe({
      next: (resp: any) => {
        const role = this.roles.fromHeader(resp);
        this._isAdmin.set(this.roles.isAdminRole(role, resp));
      },
      error: () => {}
    });
  }

  loadMyLeagues(): void {
    this.leaguesLoading.set(true);
    this.leaguesError.set(null);

    this.users.getProfile().subscribe({
      next: (p: any) => {
        const role = this.roles.fromHeader(p);
        this._isAdmin.set(this.roles.isAdminRole(role, p));

        const rows: LeagueRow[] = (p?.CommissionedLeagues ?? []).map((x: any) => ({
          LeagueID: x.LeagueID, LeagueName: x.LeagueName, Status: x.Status
        }));
        this.leagues.set(rows);
        this.leaguesLoading.set(false);
      },
      error: () => {
        this._isAdmin.set(false);
        this.leagues.set([]);
        this.leaguesLoading.set(false);
        this.leaguesError.set('No se pudieron cargar tus ligas');
      }
    });
  }
}
