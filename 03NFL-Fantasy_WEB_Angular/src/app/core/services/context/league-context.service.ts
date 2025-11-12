// core/context/league-context.service.ts
import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LeagueContextService {
  readonly currentLeagueId = signal<number | null>(this.read('xnf.currentLeagueId'));
  readonly currentTeamId   = signal<number | null>(this.read('xnf.currentTeamId'));

  setLeague(id: number) { this.write('xnf.currentLeagueId', id); this.currentLeagueId.set(id); }
  setTeam(id: number)   { this.write('xnf.currentTeamId', id);   this.currentTeamId.set(id); }

  private read(k: string): number | null {
    const raw = localStorage.getItem(k); const n = raw ? Number(raw) : NaN;
    return Number.isFinite(n) && n > 0 ? n : null;
  }
  private write(k: string, v: number) { localStorage.setItem(k, String(v)); }
}
