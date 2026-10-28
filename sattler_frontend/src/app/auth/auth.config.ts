import { AuthConfig } from 'angular-oauth2-oidc';

export const API_BASE_URL = 'http://localhost:5200/api';

/** Keycloak realm, see backend/keycloak (client "sattler-web") */
export const authConfig: AuthConfig = {
  issuer: 'http://localhost:8180/realms/equipressure',
  clientId: 'sattler-web',
  redirectUri: window.location.origin + '/',
  postLogoutRedirectUri: window.location.origin + '/',
  responseType: 'code',
  scope: 'openid profile email offline_access',
  // local Keycloak runs without HTTPS
  requireHttps: false,
  showDebugInformation: false,
};

/**
 * Device category of the saddler version (Professional), claiming such a device makes the person a saddler.
 * TODO: read the category from the QR code / device once it is known there
 */
export const SADDLER_DEVICE_CATEGORY_ID = 5;
