export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  websiteLink?: string;
  description?: string;
  birthDate?: string;
  address?: string;
  heightCm: number;
  weightKg: number;
  isMe?: boolean;
  horseIds: string[];
}

export function fullName(c: Customer): string {
  return `${c.firstName} ${c.lastName}`;
}
