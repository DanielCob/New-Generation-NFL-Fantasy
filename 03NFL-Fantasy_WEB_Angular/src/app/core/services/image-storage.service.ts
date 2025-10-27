// src/app/core/services/image-storage.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadResponse {
  imageUrl: string;
}

@Injectable({
  providedIn: 'root'
})
export class ImageStorageService {
  private readonly baseUrl = `${environment.apiUrl}/Storage`;

  constructor(private http: HttpClient) {}

  uploadImage(file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<any>(`${this.baseUrl}/upload-image`, formData).pipe(
      map(res => {
        if (!res?.Data?.ImageUrl) {
          throw new Error('No se pudo obtener la URL de la imagen.');
        }
        return { imageUrl: res.Data.ImageUrl };
      })
    );
  }

  deleteImage(imageUrl: string): Observable<boolean> {
    return this.http.delete<any>(`${this.baseUrl}/delete-image`, { body: { ImageUrl: imageUrl } }).pipe(
      map(res => !!res?.Success)
    );
  }
}
