// src/app/pages/_facades/navbar.facade.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { AuthService } from '../../core/services/auth-service';
import { UserService } from '../../core/services/user-service';
import { toSignal } from '@angular/core/rxjs-interop';

interface LeagueRow {
  LeagueID: number;
  LeaguePublicID: number;
  LeagueName: string;
  Status: number;
}

@Injectable({ providedIn: 'root' })
export class NavbarFacade {
  private auth = inject(AuthService);
  private users = inject(UserService);

  session = toSignal(this.auth.session$, { initialValue: null });
  
  // ✅ DETECTAR SI ES ADMIN desde la sesión
  isAdmin = computed(() => {
    const s = this.session();
    console.log('🔍 [NavbarFacade] Verificando admin - session:', s);
    console.log('🔍 [NavbarFacade] SystemRoleCode:', s?.SystemRoleCode);
    
    // Ajusta el nombre del campo según lo que devuelve tu API
    return s?.SystemRoleCode === 'ADMIN' || s?.SystemRoleCode === 'ADMINISTRATOR';
  });

  leagues = signal<LeagueRow[]>([]);
  leaguesLoading = signal(false);
  leaguesError = signal<string | null>(null);

  loadMyLeagues(): void {
    if (this.leaguesLoading()) {
      return;
    }
    
    console.log('📡 [NavbarFacade] Iniciando petición HTTP...');
    this.leaguesLoading.set(true);
    this.leaguesError.set(null);

    this.users.getProfile().subscribe({
      next: (p: any) => {
        const rawLeagues = p?.CommissionedLeagues ?? [];
        
        const rows: LeagueRow[] = rawLeagues.map((x: any) => ({
          LeagueID: x.LeagueID,
          LeaguePublicID: x.LeaguePublicID,
          LeagueName: x.LeagueName,
          Status: x.Status
        }));
        
        this.leagues.set(rows);
        this.leaguesLoading.set(false);
      },
      error: (err) => {
        this.leagues.set([]);
        this.leaguesLoading.set(false);
        this.leaguesError.set('No se pudieron cargar tus ligas');
      }
    });
  }
}