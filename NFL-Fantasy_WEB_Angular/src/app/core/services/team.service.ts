import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OwnedTeamOption } from '../models/team.model';
import {
  MyTeamResponse,
  UpdateTeamBrandingDTO,
  AddPlayerToRosterDTO,
  RosterDistribution,
  FantasyTeamDetails
} from '../models/team.model';
import { ApiResponse } from '../models/common.model';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/team`;

  /**
   * Actualiza el branding de un equipo fantasy
   * PUT /api/team/{id}/branding
   * Feature 3.1 - Solo el dueño puede editar
   */
  updateBranding(teamId: number, dto: UpdateTeamBrandingDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${teamId}/branding`, dto);
  }

  /**
   * Obtiene información completa del equipo con roster
   * GET /api/team/{id}/my-team
   * Feature 3.1 - Con filtros opcionales
   */
  getMyTeam(teamId: number, filterPosition?: string, searchPlayer?: string): Observable<ApiResponse<MyTeamResponse>> {
    let params: any = {};
    if (filterPosition) params.filterPosition = filterPosition;
    if (searchPlayer) params.searchPlayer = searchPlayer;

    return this.http.get<ApiResponse<MyTeamResponse>>(`${this.baseUrl}/${teamId}/my-team`, { params });
  }

  /**
   * Obtiene distribución porcentual del roster
   * GET /api/team/{id}/roster/distribution
   * Feature 3.1
   */
  getRosterDistribution(teamId: number): Observable<ApiResponse<RosterDistribution[]>> {
    return this.http.get<ApiResponse<RosterDistribution[]>>(`${this.baseUrl}/${teamId}/roster/distribution`);
  }

  /**
   * Agrega un jugador al roster
   * POST /api/team/{id}/roster/add
   * Feature 3.1
   */
  addPlayerToRoster(teamId: number, dto: AddPlayerToRosterDTO): Observable<ApiResponse<{ rosterID: number }>> {
    return this.http.post<ApiResponse<{ rosterID: number }>>(`${this.baseUrl}/${teamId}/roster/add`, dto);
  }

  /**
   * Remueve un jugador del roster (soft delete)
   * POST /api/team/roster/{rosterId}/remove
   * Feature 3.1
   */
  removePlayerFromRoster(rosterId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/roster/${rosterId}/remove`, {});
  }
  listOwnedTeams(): Observable<ApiResponse<OwnedTeamOption[]>> {
  return this.http.get<ApiResponse<OwnedTeamOption[]>>(`${this.baseUrl}/my-teams`);
  }
}
