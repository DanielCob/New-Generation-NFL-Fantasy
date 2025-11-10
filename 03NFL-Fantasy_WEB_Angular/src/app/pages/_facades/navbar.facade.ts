// src/app/pages/_facades/navbar.facade.ts
import { Injectable, effect, signal, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService, AuthSession } from '../../core/services/auth-service';
import { UserService } from '../../core/services/user-service';
import { AuthzService } from '../../core/services/authz/authz.service';

export type LeagueRow = { LeagueID: number; LeagueName: string; Status: number };

@Injectable({ providedIn: 'root' })
export class NavbarFacade {
  private auth  = inject(AuthService);
  private users = inject(UserService);
  private authz = inject(AuthzService);

  readonly session        = signal<AuthSession | null>(this.auth.session);
  readonly leagues        = signal<LeagueRow[]>([]);
  readonly leaguesLoading = signal(false);
  readonly leaguesError   = signal<string | null>(null);

  // Fuente única de verdad: admin viene del AuthzService (cacheado/compartido)
  readonly isAdmin = toSignal(this.authz.isAdmin$, { initialValue: false });

  constructor() {
    // Mantener sesión reactiva
    this.auth.session$.subscribe(s => this.session.set(s));

    // Reaccionar al login/logout y cargar datos necesarios
    effect(() => {
      const s = this.session();
      if (s?.SessionID) {
        // Garantiza que el AuthzService tenga header fresco si hace falta
        this.authz.refresh();

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

  loadMyLeagues(): void {
    this.leaguesLoading.set(true);
    this.leaguesError.set(null);

    this.users.getProfile().subscribe({
      next: (p: any) => {
        const rows: LeagueRow[] = (p?.CommissionedLeagues ?? []).map((x: any) => ({
          LeagueID: x.LeagueID, LeagueName: x.LeagueName, Status: x.Status
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
}
