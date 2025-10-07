// src/app/core/interceptors/auth-interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Storage } from '../services/storage';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(Storage);
  
  // List of public endpoints that don't need authentication
  const publicEndpoints = [
    '/api/User/clients',
    '/api/User/engineers', 
    '/api/User/administrators',
    '/api/Auth/login',
    '/api/Location/provinces',
    '/api/Location/cantons',
    '/api/Location/districts'
  ];
  
  // Check if this is a public endpoint
  const isPublicEndpoint = publicEndpoints.some(endpoint => 
    req.url.toLowerCase().includes(endpoint.toLowerCase())
  );
  
  // Only add auth header for protected endpoints AND if we have a token
  if (!isPublicEndpoint) {
    const token = storage.getToken();
    
    if (token) {
      // The token is just the GUID, add "Bearer " prefix for the API
      const clonedRequest = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(clonedRequest);
    }
  }
  
  // For public endpoints or when no token, send request without auth header
  return next(req);
};