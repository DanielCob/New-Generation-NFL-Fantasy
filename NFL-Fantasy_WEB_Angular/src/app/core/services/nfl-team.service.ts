import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateNFLTeamDTO,
  UpdateNFLTeamDTO,
  ListNFLTeamsRequest,
  ListNFLTeamsResponse,
  NFLTeamDetails,
  NFLTeamBasic
} from '../models/nfl-team.model';
import { ApiResponse } from '../models/common.model';

@Injectable({ providedIn: 'root' })
export class NflTeamService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/nflteam`;

  /**
   * Crea un nuevo equipo NFL
   * POST /api/nflteam
   * Feature 10.1
   */
  create(dto: CreateNFLTeamDTO): Observable<ApiResponse<{ nflTeamID: number; teamName: string }>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}`, dto);
  }

  /**
   * Lista equipos NFL con paginación
   * GET /api/nflteam
   * Feature 10.1
   */
  list(request: ListNFLTeamsRequest): Observable<ApiResponse<ListNFLTeamsResponse>> {
    let params = new HttpParams()
      .set('pageNumber', request.PageNumber.toString())
      .set('pageSize', request.PageSize.toString());

    if (request.SearchTerm) params = params.set('searchTerm', request.SearchTerm);
    if (request.FilterCity) params = params.set('filterCity', request.FilterCity);
    if (request.FilterIsActive !== undefined) params = params.set('filterIsActive', request.FilterIsActive.toString());

    return this.http.get<ApiResponse<ListNFLTeamsResponse>>(`${this.baseUrl}`, { params });
  }

  /**
   * Obtiene detalles completos de un equipo NFL
   * GET /api/nflteam/{id}
   * Feature 10.1 - Incluye historial de cambios + jugadores activos
   */
  getDetails(nflTeamId: number): Observable<ApiResponse<NFLTeamDetails>> {
    return this.http.get<ApiResponse<NFLTeamDetails>>(`${this.baseUrl}/${nflTeamId}`);
  }

  /**
   * Actualiza un equipo NFL
   * PUT /api/nflteam/{id}
   * Feature 10.1
   */
  update(nflTeamId: number, dto: UpdateNFLTeamDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}`, dto);
  }

  /**
   * Desactiva un equipo NFL
   * POST /api/nflteam/{id}/deactivate
   * Feature 10.1 - Valida que no tenga partidos programados
   */
  deactivate(nflTeamId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}/deactivate`, {});
  }

  /**
   * Reactiva un equipo NFL
   * POST /api/nflteam/{id}/reactivate
   * Feature 10.1
   */
  reactivate(nflTeamId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}/reactivate`, {});
  }

  /**
   * Obtiene solo equipos NFL activos (para dropdowns)
   * GET /api/nflteam/active
   * Feature 10.1
   */
  getActive(): Observable<ApiResponse<NFLTeamBasic[]>> {
    return this.http.get<ApiResponse<NFLTeamBasic[]>>(`${this.baseUrl}/active`);
  }
}
