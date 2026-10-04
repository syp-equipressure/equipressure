export interface Sattler {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  companyName?: string;
  address?: string;
  memberSince: string;
  avatarUrl?: string;
}

export function sattlerFullName(s: Sattler): string {
  return `${s.firstName} ${s.lastName}`;
}
