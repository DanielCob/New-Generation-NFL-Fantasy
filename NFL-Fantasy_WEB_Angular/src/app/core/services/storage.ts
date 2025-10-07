// src/app/core/services/storage.ts
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class Storage {
  private readonly TOKEN_KEY = 'sessionToken';
  private readonly USER_KEY = 'currentUser';

  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  removeToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
  }

  setUser(user: any): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  getUser(): any {
    const user = localStorage.getItem(this.USER_KEY);
    return user ? JSON.parse(user) : null;
  }

  removeUser(): void {
    localStorage.removeItem(this.USER_KEY);
  }

  clear(): void {
    localStorage.clear();
  }
}