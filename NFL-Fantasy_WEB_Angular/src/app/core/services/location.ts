// src/app/core/services/location.ts
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Api } from './api';
import { Province, Canton, District } from '../models/location.model';

@Injectable({
  providedIn: 'root'
})
export class Location {
  private readonly api = inject(Api);

  getProvinces(): Observable<Province[]> {
    return this.api.get<Province[]>('/Location/provinces');
  }

  getCantonsByProvince(provinceId: number): Observable<Canton[]> {
    return this.api.get<Canton[]>(`/Location/cantons/by-province/${provinceId}`);
  }

  getDistrictsByCanton(cantonId: number): Observable<District[]> {
    return this.api.get<District[]>(`/Location/districts/by-canton/${cantonId}`);
  }
}