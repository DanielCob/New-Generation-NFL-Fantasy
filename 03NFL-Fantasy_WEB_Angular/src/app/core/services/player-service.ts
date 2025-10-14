import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerBasic, AvailablePlayer, PlayerFilters } from '../models/player-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class PlayerService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/player`;

  /**
   * Lista todos los jugadores con filtros opcionales
   * GET /api/player
   */
  list(filters?: PlayerFilters): Observable<ApiResponse<PlayerBasic[]>> {
    let params = new HttpParams();

    if (filters?.position) params = params.set('position', filters.position);
    if (filters?.nflTeamId) params = params.set('nflTeamId', filters.nflTeamId.toString());
    if (filters?.injuryStatus) params = params.set('injuryStatus', filters.injuryStatus);

    return this.http.get<ApiResponse<PlayerBasic[]>>(`${this.baseUrl}`, { params });
  }

  /**
   * Lista jugadores disponibles (no en roster activo)
   * GET /api/player/available
   * Para draft y free agency
   */
  getAvailable(position?: string): Observable<ApiResponse<AvailablePlayer[]>> {
    let params = new HttpParams();
    if (position) params = params.set('position', position);

    return this.http.get<ApiResponse<AvailablePlayer[]>>(`${this.baseUrl}/available`, { params });
  }

  /**
   * Obtiene jugadores de un equipo NFL específico
   * GET /api/player/by-nfl-team/{nflTeamId}
   */
  getByNflTeam(nflTeamId: number): Observable<ApiResponse<PlayerBasic[]>> {
    return this.http.get<ApiResponse<PlayerBasic[]>>(`${this.baseUrl}/by-nfl-team/${nflTeamId}`);
  }
}
