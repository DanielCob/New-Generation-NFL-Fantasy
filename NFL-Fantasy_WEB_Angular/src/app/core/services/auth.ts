// src/app/core/services/auth.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, switchMap } from 'rxjs';
import { Api } from './api';
import { Storage } from './storage';
import { LoginRequest, LoginResponse } from '../models/auth.model';
import { CurrentUser } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class Auth {
  private readonly api = inject(Api);
  private readonly storage = inject(Storage);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  private currentUserSignal = signal<CurrentUser | null>(null);
  
  currentUser = computed(() => this.currentUserSignal());
  isAuthenticated = computed(() => !!this.currentUserSignal());
  userType = computed(() => this.currentUserSignal()?.userType);

  constructor() {
    this.loadStoredUser();
  }

  private loadStoredUser(): void {
    const storedUser = this.storage.getUser();
    const token = this.storage.getToken();
    
    if (storedUser && token) {
      this.currentUserSignal.set(storedUser);
      this.validateToken().subscribe();
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('/Auth/login', credentials).pipe(
      tap(response => {
        if (response.success && response.sessionToken) {
          this.storage.setToken(response.sessionToken);
          
          const user: CurrentUser = {
            userId: response.userID!,
            userType: response.userType as 'CLIENT' | 'ENGINEER' | 'ADMIN',
            sessionToken: response.sessionToken,
            email: credentials.email,
            firstName: '',
            lastSurname: ''
          };
          
          this.storage.setUser(user);
          this.currentUserSignal.set(user);
          
          // Fetch additional user details
          this.fetchUserDetails().subscribe();
          
          // Navigate based on user type
          this.navigateByUserType(user.userType);
        }
      })
    );
  }

  logout(): Observable<any> {
    return this.api.post('/auth/logout', {}).pipe(
      tap(() => this.clearSession()),
      catchError(() => {
        this.clearSession();
        return of(null);
      })
    );
  }

  private clearSession(): void {
    this.storage.clear();
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  private validateToken(): Observable<any> {
    return this.api.get('/auth/me').pipe(
      catchError(() => {
        this.clearSession();
        return of(null);
      })
    );
  }

  private fetchUserDetails(): Observable<any> {
    const user = this.currentUserSignal();
    if (!user) return of(null);

    const endpoint = user.userType === 'CLIENT' ? '/user/me/details' :
                     user.userType === 'ENGINEER' ? '/user/me/details' :
                     '/user/me/details';

    return this.api.get(endpoint).pipe(
      tap((details: any) => {
        const updatedUser = {
          ...user,
          firstName: details.firstName || '',
          lastSurname: details.lastSurname || ''
        };
        this.storage.setUser(updatedUser);
        this.currentUserSignal.set(updatedUser);
      })
    );
  }

  private navigateByUserType(userType: string): void {
    switch (userType) {
      case 'ADMIN':
        this.router.navigate(['/admin']);
        break;
      case 'ENGINEER':
        this.router.navigate(['/engineer']);
        break;
      case 'CLIENT':
        this.router.navigate(['/client']);
        break;
      default:
        this.router.navigate(['/']);
    }
  }

  isAdmin(): boolean {
    return this.userType() === 'ADMIN';
  }

  isEngineer(): boolean {
    return this.userType() === 'ENGINEER';
  }

  isClient(): boolean {
    return this.userType() === 'CLIENT';
  }
}