/**
 * user.service.ts
 * ---------------------------------------------------------
 * Cambios:
 * - Se añade getProfile(): llama a /api/User/profile para traer
 *   el perfil COMPLETO (incluye CommissionedLeagues y Teams).
 * - Se mantiene getHeader(): útil para pings/guards rápidos.
 */

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { ApiResponse } from '../models/auth-model';
import {
  UserProfile,
  UserSession,
  EditUserProfileRequest,
  EditUserProfileResponse
} from '../models/user-model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly userUrl = `${environment.apiUrl}/User`; // /api/User

  constructor(private http: HttpClient) {}

  /**
   * GET /api/User/header
   * Perfil corto (header). Útil para guards y navbar.
   */
  getHeader(): Observable<UserProfile> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.userUrl}/header`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserProfile));
  }

  /**
   * GET /api/User/profile
   * Perfil COMPLETO desde sp_GetUserProfile:
   * - Datos del usuario
   * - CommissionedLeagues
   * - Teams
   */
  getProfile(): Observable<UserProfile> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.userUrl}/profile`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserProfile));
  }

  /**
   * GET /api/User/sessions
   * Sesiones activas del usuario.
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
