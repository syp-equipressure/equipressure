export type RoleName = 'Equestrian' | 'Saddler' | 'Guest';

/** Person of the logged-in user, see GET /api/account/me */
export interface AccountPerson {
  id: number;
  firstName: string;
  lastName: string;
  email: string | null;
  roles: RoleName[];
}

/** Profile setup after the first login, see POST /api/account */
export interface CreateAccountRequest {
  firstName: string;
  lastName: string;
  height: number;
  weight: number;
  dateOfBirth: string;
  address: {
    addressName: string | null;
    cityName: string;
    plz: string;
  };
}

/** Where the logged-in user stands in the onboarding */
export type AccountState = 'no-profile' | 'no-device' | 'ready';
