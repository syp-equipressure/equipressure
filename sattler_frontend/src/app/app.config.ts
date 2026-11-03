import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideOAuthClient } from 'angular-oauth2-oidc';

import { routes } from './app.routes';
import { API_BASE_URL } from './auth/auth.config';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    // sends the Keycloak access token with every request to the backend
    provideOAuthClient({ resourceServer: { allowedUrls: [API_BASE_URL], sendAccessToken: true } }),
    // applies the saved theme on start, before the first page is shown
    provideAppInitializer(() => { inject(ThemeService); }),
    provideAppInitializer(() => inject(AuthService).init()),
  ]
};
