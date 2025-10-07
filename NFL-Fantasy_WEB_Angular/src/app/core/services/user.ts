// src/app/core/services/user.ts
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Api } from './api';
import { CreateClientDTO, CreateEngineerDTO } from '../models/user.model';
import { ApiResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class User {
  private readonly api = inject(Api);

  private cleanNullValues(data: any): any {
    const cleaned: any = {};
    
    for (const key in data) {
      if (data[key] !== null && data[key] !== undefined && data[key] !== '') {
        cleaned[key] = data[key];
      }
    }
    
    return cleaned;
  }

  createClient(data: CreateClientDTO): Observable<ApiResponse> {
    // Remove null values before sending
    const cleanedData = this.cleanNullValues(data);
    return this.api.post<ApiResponse>('/User/clients', cleanedData);
  }

  createEngineer(data: CreateEngineerDTO): Observable<ApiResponse> {
    // Remove null values before sending
    const cleanedData = this.cleanNullValues(data);
    return this.api.post<ApiResponse>('/User/engineers', cleanedData);
  }

  changePassword(oldPassword: string, newPassword: string): Observable<ApiResponse> {
    return this.api.post<ApiResponse>('/Auth/change-password', {
      oldPassword,
      newPassword
    });
  }
}