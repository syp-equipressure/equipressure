import { Injectable, inject } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from '../auth/auth.config';

interface IdentityClaims {
  given_name?: string;
  family_name?: string;
  email?: string;
}

/** Login over Keycloak (Google, Microsoft, GitHub), the backend only validates the access token */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);

  /** Called once on app start: processes the redirect back from Keycloak and keeps the token fresh */
  async init(): Promise<void> {
    this.oauth.configure(authConfig);
    await this.oauth.loadDiscoveryDocumentAndTryLogin();
    this.oauth.setupAutomaticSilentRefresh();
  }

  get isLoggedIn(): boolean {
    return this.oauth.hasValidAccessToken();
  }

  /** Redirects to the Keycloak login page, afterwards the user comes back to the start page */
  login(): void {
    this.oauth.initCodeFlow();
  }

  logout(): void {
    this.oauth.revokeTokenAndLogout();
  }

  get firstName(): string {
    return this.claims?.given_name ?? '';
  }

  get lastName(): string {
    return this.claims?.family_name ?? '';
  }

  get email(): string {
    return this.claims?.email ?? '';
  }

  private get claims(): IdentityClaims | null {
    return this.oauth.getIdentityClaims() as IdentityClaims | null;
  }
}
