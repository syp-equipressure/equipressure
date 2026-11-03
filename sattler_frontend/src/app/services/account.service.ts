import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, SADDLER_DEVICE_CATEGORY_ID } from '../auth/auth.config';
import { AccountPerson, AccountState, CreateAccountRequest } from '../models/account.model';
import { BackendPersonContextService } from './backend-person-context.service';

/** Person of the logged-in user: profile setup and device claim after the first login */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly personContext = inject(BackendPersonContextService);

  readonly me = signal<AccountPerson | null>(null);

  /** Loads the person of the logged-in user and tells if the onboarding is finished */
  async loadState(): Promise<AccountState> {
    try {
      const me = await firstValueFrom(this.http.get<AccountPerson>(`${API_BASE_URL}/account/me`));
      this.setMe(me);
      return me.roles.includes('Saddler') ? 'ready' : 'no-device';
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.me.set(null);
        return 'no-profile';
      }
      throw error;
    }
  }

  async createAccount(request: CreateAccountRequest): Promise<void> {
    const me = await firstValueFrom(this.http.post<AccountPerson>(`${API_BASE_URL}/account`, request));
    this.setMe(me);
  }

  /** Claims the saddler device with the given serial number, the user becomes saddler */
  async claimDevice(serialNumber: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API_BASE_URL}/devices/${encodeURIComponent(serialNumber)}/claim`, {
        categoryId: SADDLER_DEVICE_CATEGORY_ID,
      }),
    );
    await this.loadState();
  }

  private setMe(me: AccountPerson): void {
    this.me.set(me);
    // all existing pages load their data for this person id
    this.personContext.savePersonId(String(me.id));
  }
}
