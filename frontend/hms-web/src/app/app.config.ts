import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { AuthService } from './core/auth.service';
import { authInterceptor } from './core/auth.interceptor';
import { apiErrorInterceptor } from './core/api-error.interceptor';

// Registers HTTP/auth plumbing and restores a persisted session before the first route renders.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
    provideRouter(routes),
    provideAppInitializer(() => inject(AuthService).restoreSession()),
  ],
};
