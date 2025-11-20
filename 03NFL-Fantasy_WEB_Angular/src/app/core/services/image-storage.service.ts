// 1. ACTUALIZAR image-storage.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadResponse {
  imageUrl: string;
}

export interface BatchImageUploadResponse {
  Success: boolean;
  Message: string;
  Data: {
    UploadedImages: Array<{
      ImageUrl: string;
      FileName: string;
      ContentType: string;
      Size: number;
      Success: boolean;
    }>;
    Errors: string[];
    TotalProcessed: number;
    SuccessCount: number;
    ErrorCount: number;
  };
}

export interface UploadJsonResponse {
  Success: boolean;
  Message: string;
  Data: {
    JsonUrl: string;
    FileName: string;
    ContentType: string;
    Size: number;
  };
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

  // ✅ NUEVO: Subida masiva de imágenes
  uploadImages(files: File[]): Observable<BatchImageUploadResponse> {
    console.log('📤 [ImageStorageService] uploadImages - Total archivos:', files.length);
    
    const formData = new FormData();
    files.forEach((file, index) => {
      console.log(`📎 [ImageStorageService] Agregando archivo ${index + 1}:`, file.name, file.size, 'bytes');
      formData.append('files', file);
    });

    return this.http.post<BatchImageUploadResponse>(`${this.baseUrl}/upload-images`, formData).pipe(
      map(res => {
        console.log('✅ [ImageStorageService] uploadImages - Respuesta:', res);
        return res;
      })
    );
  }

  // ✅ NUEVO: Subir JSON
  uploadJson(jsonBlob: Blob, fileName: string): Observable<UploadJsonResponse> {
    console.log('📤 [ImageStorageService] uploadJson - Nombre:', fileName, 'Tamaño:', jsonBlob.size);
    
    const formData = new FormData();
    formData.append('file', jsonBlob, fileName);

    return this.http.post<UploadJsonResponse>(`${this.baseUrl}/upload-json`, formData).pipe(
      map(res => {
        console.log('✅ [ImageStorageService] uploadJson - Respuesta:', res);
        return res;
      })
    );
  }

  deleteImage(imageUrl: string): Observable<boolean> {
    return this.http.delete<any>(`${this.baseUrl}/delete-image`, { body: { ImageUrl: imageUrl } }).pipe(
      map(res => !!res?.Success)
    );
  }
}