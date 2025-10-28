import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OwnedTeamOption } from '../models/team-model';
import { map } from 'rxjs/operators';
import {
  MyTeamResponse,
  AddPlayerToRosterDTO,
  RosterDistribution,
  FantasyTeamDetails
} from '../models/team-model';
import { ApiUpdateTeamBrandingBody, UpdateTeamBrandingDTO } from '../models/team-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/Team`;

  /**
   * Actualiza el branding de un equipo fantasy
   * PUT /api/team/{id}/branding
   * Feature 3.1 - Solo el dueño puede editar
   */
  // team.service.ts

  getMyTeam(teamId: number, filterPosition?: string, searchPlayer?: string) {
    const params: any = {};
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
   private mapBrandingToApi(dto: UpdateTeamBrandingDTO): ApiUpdateTeamBrandingBody {
    const body: ApiUpdateTeamBrandingBody = {};
    const trim = (s?: string | null) => (s?.trim() ? s.trim() : undefined);
    const isNum = (n: unknown): n is number => typeof n === 'number' && Number.isFinite(n);

    const name = trim(dto.teamName);
    if (name) body.TeamName = name;

    const imgUrl = trim(dto.teamImageUrl);
    if (imgUrl) {
      body.TeamImageUrl = imgUrl;
      if (isNum(dto.teamImageWidth))  body.TeamImageWidth  = dto.teamImageWidth;
      if (isNum(dto.teamImageHeight)) body.TeamImageHeight = dto.teamImageHeight;
      if (isNum(dto.teamImageBytes))  body.TeamImageBytes  = dto.teamImageBytes;

      const thUrl = trim(dto.thumbnailUrl);
      if (thUrl) body.ThumbnailUrl = thUrl;
      if (isNum(dto.thumbnailWidth))  body.ThumbnailWidth  = dto.thumbnailWidth;
      if (isNum(dto.thumbnailHeight)) body.ThumbnailHeight = dto.thumbnailHeight;
      if (isNum(dto.thumbnailBytes))  body.ThumbnailBytes  = dto.thumbnailBytes;
    }
    return body;
  }

 updateBranding(teamId: number, dto: UpdateTeamBrandingDTO): Observable<ApiResponse<void>> {
  const body: ApiUpdateTeamBrandingBody = this.mapBrandingToApi(dto); // ✅ tipa bien
  return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${teamId}/branding`, body);
}

}
