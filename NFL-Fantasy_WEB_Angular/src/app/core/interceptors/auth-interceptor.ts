// src/app/core/interceptors/auth-interceptor.ts
import { inject } from '@angular/core';
import {
  HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpErrorResponse
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** Endpoints públicos: NO llevan Authorization */
function isPublicRequest(url: string): boolean {
  // Nota: el req.url viene completo (https://host:port/api/...).
  // Usamos includes() con los paths en el casing exacto del backend.
  const whitelist = [
    '/api/Auth/login',
    '/api/Auth/register',
    '/api/Auth/request-reset',
    '/api/Auth/reset-with-token',
    '/api/Reference/',    // catálogos
    '/api/Scoring/'       // catálogos
  ];
  return whitelist.some(p => url.includes(p));
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // No añadimos token a requests públicos ni a OPTIONS (CORS preflight)
  let request = req;
  if (req.method !== 'OPTIONS' && !isPublicRequest(req.url)) {
    const token = auth.session?.SessionID;
    if (token) {
      request = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
  }

  // Si el backend responde 401 => sesión inválida/expirada
  return next(request).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        // limpiamos sesión local y enviamos a /login manteniendo returnUrl
        auth.clearLocalSession();
        const returnUrl = location.pathname + location.search;
        router.navigate(['/login'], { queryParams: { returnUrl } });
      }
      return throwError(() => err);
    })
  );
};
