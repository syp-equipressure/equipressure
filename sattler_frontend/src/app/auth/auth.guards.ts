import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account.service';
import { AuthService } from '../services/auth.service';

/** Pages of the app: requires a login and a finished onboarding (profile + saddler device) */
export const accountGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const account = inject(AccountService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    auth.login();
    return false;
  }

  if (account.me()?.roles.includes('Saddler')) {
    return true;
  }

  const state = await account.loadState();
  return state === 'ready' ? true : router.createUrlTree(['/onboarding']);
};

/** Onboarding page: requires a login, users that are already set up go to the app */
export const onboardingGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const account = inject(AccountService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    auth.login();
    return false;
  }

  const state = await account.loadState();
  return state === 'ready' ? router.createUrlTree(['/']) : true;
};
