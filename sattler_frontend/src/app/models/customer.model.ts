export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  heightCm: number;
  weightKg: number;
  isMe?: boolean;
  horseIds: string[];
}

export function fullName(c: Customer): string {
  return `${c.firstName} ${c.lastName}`;
}
