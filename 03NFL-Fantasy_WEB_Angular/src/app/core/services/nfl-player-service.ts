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
  NFLPlayerBasic
} from '../models/nfl-player-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class NFLPlayerService {
  private http = inject(HttpClient);
  // ❗ Respeta el casing del backend
  private baseUrl = `${environment.apiUrl}/NFLPlayer`;

  /** 1) POST /api/nflplayer */
  create(dto: CreateNFLPlayerDTO): Observable<ApiResponse<{ nflPlayerID: number; fullName?: string }>> {
    return this.http.post<ApiResponse<{ nflPlayerID: number; fullName?: string }>>(`${this.baseUrl}`, dto);
  }

  /** 2) GET /api/nflplayer?PageNumber=&PageSize=&SearchTerm=&FilterPosition=&FilterNFLTeamID=&FilterIsActive= */
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

  /** 3) GET /api/nflplayer/{id} */
  getDetails(nflPlayerId: number): Observable<ApiResponse<NFLPlayerDetails>> {
    return this.http.get<ApiResponse<NFLPlayerDetails>>(`${this.baseUrl}/${nflPlayerId}`);
  }

  /** 4) PUT /api/nflplayer/{id} */
  update(nflPlayerId: number, dto: UpdateNFLPlayerDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}`, dto);
  }

  /** 5) POST /api/nflplayer/{id}/deactivate */
  deactivate(nflPlayerId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}/deactivate`, {});
  }

  /** 6) POST /api/nflplayer/{id}/reactivate */
  reactivate(nflPlayerId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflPlayerId}/reactivate`, {});
  }

  /** 7) GET /api/nflplayer/active */
  getActive(): Observable<ApiResponse<NFLPlayerBasic[]>> {
    return this.http.get<ApiResponse<NFLPlayerBasic[]>>(`${this.baseUrl}/active`);
  }
}
