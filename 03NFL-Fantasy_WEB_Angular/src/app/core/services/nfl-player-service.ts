// 2. ACTUALIZAR nfl-player.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateNFLPlayerDTO,
  UpdateNFLPlayerDTO,
  ListNFLPlayersRequest,
  ListNFLPlayersResponse,
  NFLPlayerDetails,
  NFLPlayerBasic,
  BatchCreatePlayersResponse,
  CreateBatchReportRequest,
  CreateBatchReportResponse,
  ListBatchReportsRequest,
  ListBatchReportsResponse,
  BatchReportDetails,
  CreatePlayerNewsDTO,
  CreatePlayerNewsResponse,
  DeletePlayerNewsResponse,
  ListPlayerNewsRequest,
  ListPlayerNewsResponse,
  PlayerNewsDetails
} from '../models/nfl-player-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class NFLPlayerService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/NFLPlayer`;

  create(dto: CreateNFLPlayerDTO): Observable<ApiResponse<{ nflPlayerID: number; fullName?: string }>> {
    return this.http.post<ApiResponse<{ nflPlayerID: number; fullName?: string }>>(`${this.baseUrl}`, dto);
  }

  // ✅ NUEVO: Crear jugadores en lote
  createBatch(players: CreateNFLPlayerDTO[]): Observable<BatchCreatePlayersResponse> {
    console.log('📤 [NFLPlayerService] createBatch - Total jugadores:', players.length);
    console.log('📋 [NFLPlayerService] createBatch - Payload:', JSON.stringify(players, null, 2));
    
    return this.http.post<BatchCreatePlayersResponse>(`${this.baseUrl}/batch`, players);
  }

  // ✅ NUEVO: Crear registro de batch report
  createBatchReport(request: CreateBatchReportRequest): Observable<CreateBatchReportResponse> {
    console.log('📤 [NFLPlayerService] createBatchReport - Request:', request);
    
    return this.http.post<CreateBatchReportResponse>(`${this.baseUrl}/batch-report`, request);
  }

  list(request: ListNFLPlayersRequest): Observable<ApiResponse<ListNFLPlayersResponse>> {
    let params = new HttpParams()
      .set('PageNumber', String(request.PageNumber))
      .set('PageSize', String(request.PageSize));

    if (request.SearchTerm?.trim())           params = params.set('SearchTerm', request.SearchTerm.trim());
    if (request.FilterPosition?.trim())       params = params.set('FilterPosition', request.FilterPosition.trim());
    if (typeof request.FilterNFLTeamID === 'number')
                                             params = params.set('FilterNFLTeamID', String(request.FilterNFLTeamID));
    if (typeof request.FilterIsActive === 'boolean')
                                             params = params.set('FilterIsActive', String(request.FilterIsActive));

    return this.http.get<ApiResponse<ListNFLPlayersResponse>>(`${this.baseUrl}`, { params });
  }

  getDetails(nflPlayerId: number): Observable<ApiResponse<NFLPlayerDetails>> {
    return this.http.get<ApiResponse<NFLPlayerDetails>>(`${this.baseUrl}/${nflPlayerId}`);
  }

  update(nflPlayerId: number, dto: UpdateNFLPlayerDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}`, dto);
  }

  deactivate(nflPlayerId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}/deactivate`, {});
  }

  reactivate(nflPlayerId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}/reactivate`, {});
  }

  getActive(): Observable<ApiResponse<NFLPlayerBasic[]>> {
    return this.http.get<ApiResponse<NFLPlayerBasic[]>>(`${this.baseUrl}/active`);
  }

  // ✅ NUEVO: Listar reportes de batch
  getBatchReports(request: ListBatchReportsRequest): Observable<ApiResponse<ListBatchReportsResponse>> {
    console.log('📤 [NFLPlayerService] getBatchReports - Request:', request);
    
    let params = new HttpParams().set('PageNumber', String(request.PageNumber));
    if (request.PageSize) {
      params = params.set('PageSize', String(request.PageSize));
    }
    
    return this.http.get<ApiResponse<ListBatchReportsResponse>>(`${this.baseUrl}/batch-reports`, { params });
  }

  // ✅ NUEVO: Obtener detalles de un reporte específico
  getBatchReportDetails(batchReportId: number): Observable<ApiResponse<BatchReportDetails>> {
    console.log('📤 [NFLPlayerService] getBatchReportDetails - ID:', batchReportId);
    
    return this.http.get<ApiResponse<BatchReportDetails>>(`${this.baseUrl}/batch-report/${batchReportId}`);
  }
  
  // ===== PLAYER NEWS =====

  // Crear noticia de jugador
  createPlayerNews(dto: CreatePlayerNewsDTO): Observable<CreatePlayerNewsResponse> {
    console.log('📤 [NFLPlayerService] createPlayerNews - Request:', dto);
    return this.http.post<CreatePlayerNewsResponse>(`${this.baseUrl}/news`, dto);
  }

  // Eliminar noticia de jugador
  deletePlayerNews(newsId: number): Observable<DeletePlayerNewsResponse> {
    console.log('📤 [NFLPlayerService] deletePlayerNews - NewsID:', newsId);
    return this.http.delete<DeletePlayerNewsResponse>(`${this.baseUrl}/news/${newsId}`);
  }

  // Obtener noticias de un jugador específico
  getPlayerNews(playerId: number, request: ListPlayerNewsRequest): Observable<ApiResponse<ListPlayerNewsResponse>> {
    console.log('📤 [NFLPlayerService] getPlayerNews - PlayerID:', playerId, 'Request:', request);
    
    let params = new HttpParams().set('pageNumber', String(request.PageNumber));
    if (request.PageSize) {
      params = params.set('pageSize', String(request.PageSize));
    }
    
    return this.http.get<ApiResponse<ListPlayerNewsResponse>>(`${this.baseUrl}/${playerId}/news`, { params });
  }

  // Obtener detalles de una noticia específica
  getPlayerNewsDetails(newsId: number): Observable<ApiResponse<PlayerNewsDetails>> {
    console.log('📤 [NFLPlayerService] getPlayerNewsDetails - NewsID:', newsId);
    return this.http.get<ApiResponse<PlayerNewsDetails>>(`${this.baseUrl}/news/${newsId}`);
  }
}