// src/app/core/services/user.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { ApiResponse } from '../models/auth.model'; // tu envoltorio genérico
import { UserProfile, UserSession, EditUserProfileRequest, EditUserProfileResponse } from '../models/user.model';
// ^^^ Si tu interfaz para sesiones se llama diferente (p. ej. ActiveSession),
// cámbiala aquí y listo.

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly userUrl = `${environment.apiUrl}/User`; // /api/User

  constructor(private http: HttpClient) {}

  /**
   * GET /api/User/header
   * El backend ya unifica el SP (3 RS -> Data).
   * Se extrae res.data || res.Data y se retorna el UserProfile directamente.
   */
  getHeader(): Observable<UserProfile> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.userUrl}/header`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserProfile));
  }

  /**
   * GET /api/User/sessions
   * Devuelve las sesiones activas del usuario (array).
   */
  getActiveSessions(): Observable<UserSession[]> {
    return this.http
      .get<ApiResponse<UserSession[]>>(`${this.userUrl}/sessions`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserSession[]));
  }
    /** PUT /api/User/profile (actor = yo) */
  updateProfile(body: EditUserProfileRequest): Observable<EditUserProfileResponse> {
    return this.http.put<EditUserProfileResponse>(`${this.userUrl}/profile`, body);
  }
}
