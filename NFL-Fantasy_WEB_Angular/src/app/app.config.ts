// src/app/app.config.ts
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';

// ⬇️ Usa el nombre real del archivo: auth.interceptor.ts
import { authInterceptor } from './core/interceptors/auth-interceptor';
// (Opcional) solo si ya tienes este archivo:
import { errorInterceptor } from './core/interceptors/error-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      // Si no tienes errorInterceptor aún, deja solo [authInterceptor]
      withInterceptors([authInterceptor, errorInterceptor])
    ),
    provideAnimationsAsync()
  ]
};
