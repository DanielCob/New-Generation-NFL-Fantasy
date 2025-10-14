// src/app/core/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, map, finalize } from 'rxjs';
import { environment } from '../../../environments/environment';

import {
  ApiResponse,
  RegisterRequest,
  LoginRequest,
  LoginResponse,
  SimpleOkResponse,
} from '../models/auth-model';

import {
  UserProfile,
  EditUserProfileRequest,
} from '../models/user-model';

export interface AuthSession {
  SessionID: string;
  Message: string;
  UserID: number;
  Email: string;
  Name: string;
}

  const normalizeApi = <T>(r: any): ApiResponse<T> => ({
    success: (r?.success ?? r?.Success) ?? false,
    message: (r?.message ?? r?.Message) ?? '',
    data: (r?.data ?? r?.Data) as T,
  });

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authUrl = `${environment.apiUrl}/Auth`;
  private readonly userUrl = `${environment.apiUrl}/User`;
  private readonly SESSION_KEY = 'xnf.session';

  private _session$ = new BehaviorSubject<AuthSession | null>(this.readSession());
  readonly session$ = this._session$.asObservable();

  constructor(private http: HttpClient) {}

  // ---------- AUTH ----------

  register(body: RegisterRequest): Observable<ApiResponse<string>> {
  return this.http.post(`${this.authUrl}/register`, body)
    .pipe(map(r => normalizeApi<string>(r)));
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.authUrl}/login`, body).pipe(
      tap(r => {
        if (r.Success && r.Data?.SessionID) {
          const s: AuthSession = r.Data as unknown as AuthSession;
          this.persistSession(s);
          this._session$.next(s);
        }
      })
    );
  }

  // ✅ Siempre limpia sesión (éxito o error)
  logout(): Observable<ApiResponse<string>> {
    return this.http.post(`${this.authUrl}/logout`, {}).pipe(
      map(r => normalizeApi<string>(r)),
      finalize(() => this.clearSession())
    );
  }

  // ✅ Siempre limpia sesión (éxito o error)
  logoutAll(): Observable<ApiResponse<string>> {
    return this.http.post(`${this.authUrl}/logout-all`, {}).pipe(
      map(r => normalizeApi<string>(r)),
      finalize(() => this.clearSession())
    );
  }

  requestReset(email: string): Observable<SimpleOkResponse> {
    return this.http.post<SimpleOkResponse>(`${this.authUrl}/request-reset`, { email });
  }

  resetWithToken(token: string, newPassword: string, confirmPassword: string): Observable<SimpleOkResponse> {
    return this.http.post<SimpleOkResponse>(`${this.authUrl}/reset-with-token`, {
      token, newPassword, confirmPassword
    });
  }

  // ---------- PROFILE ----------

  getProfile(): Observable<UserProfile> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.userUrl}/header`)
      .pipe(map(res => (res.data ?? (res as any).Data) as UserProfile));
  }

  updateProfile(body: EditUserProfileRequest): Observable<SimpleOkResponse> {
    return this.http.post<SimpleOkResponse>(`${this.userUrl}/update-profile`, body);
  }
  getCurrentUserId(): number | null {
    return this._session$.value?.UserID ?? null;
  }

  // ---------- SESIÓN LOCAL ----------

  isAuthenticated(): boolean {
    return !!this._session$.value?.SessionID;
  }

  get session(): AuthSession | null {
    return this._session$.value;
  }

  private persistSession(s: AuthSession): void {
    localStorage.setItem(this.SESSION_KEY, JSON.stringify(s));
  }

  private readSession(): AuthSession | null {
    const raw = localStorage.getItem(this.SESSION_KEY);
    return raw ? (JSON.parse(raw) as AuthSession) : null;
  }

  private clearSession(): void {
    localStorage.removeItem(this.SESSION_KEY);
    this._session$.next(null);
  }
  clearLocalSession(): void { (this as any).clearSession?.(); }


}