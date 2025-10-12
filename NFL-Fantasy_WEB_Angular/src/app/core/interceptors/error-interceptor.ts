// src/app/core/interceptors/error-interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';

/** Intenta extraer un mensaje útil del error devuelto por la API */
function extractApiMessage(err: HttpErrorResponse): string {
  const e = err?.error ?? err;

  // ApiResponse-like: { success, message, data } o { Success, Message, Data }
  if (e && typeof e === 'object' && ('message' in e || 'Message' in e)) {
    return (e.message ?? e.Message) as string;
  }

  // ProblemDetails (ASP.NET): { title, detail, errors }
  if (e && typeof e === 'object' && ('title' in e || 'detail' in e || 'errors' in e)) {
    const parts: string[] = [];
    if ((e as any).title)  parts.push(String((e as any).title));
    if ((e as any).detail) parts.push(String((e as any).detail));
    if ((e as any).errors && typeof (e as any).errors === 'object') {
      const all = Object.values((e as any).errors as Record<string, string[]>).flat();
      if (all.length) parts.push(all.join(' | '));
    }
    if (parts.length) return parts.join(' - ');
  }

  // string plano
  if (typeof e === 'string') return e;

  // fallback
  return err.message || 'Error inesperado';
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let msg = 'Ocurrió un error';

      if (error.status === 0) {
        // caída de red / CORS / servidor apagado
        msg = 'No se pudo conectar con el servidor.';
      } else {
        switch (error.status) {
          case 401: {
            // limpiar token + enviar a login
            auth.clearLocalSession?.();
            const returnUrl = location.pathname + location.search;
            router.navigate(['/login'], { queryParams: { returnUrl } });
            msg = 'Tu sesión expiró. Inicia sesión nuevamente.';
            break;
          }
          case 403:
          case 409: {
            // mostrar mensaje que envía el backend
            msg = extractApiMessage(error) || `Error ${error.status}`;
            break;
          }
          default: {
            // intenta extraer mensaje útil; si no, uno genérico
            msg = extractApiMessage(error) || `Error ${error.status}`;
            break;
          }
        }
      }

      snackBar.open(msg, 'Cerrar', {
        duration: 6000,
        horizontalPosition: 'end',
        verticalPosition: 'top',
        panelClass: ['error-snackbar'],
      });

      return throwError(() => error);
    })
  );
};
