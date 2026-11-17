export interface Sattler {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  companyName?: string;
  address?: string;
  website?: string;
  description?: string;
  memberSince: string;
  avatarUrl?: string;
}

export function sattlerFullName(s: Sattler): string {
  return `${s.firstName} ${s.lastName}`;
}
