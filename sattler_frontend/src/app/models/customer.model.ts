export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  websiteLink?: string;
  description?: string;
  birthDate?: string;
  /** ISO timestamp when the person was added, shown as "member since" */
  createdAt?: string;
  /** ISO timestamp when the image was set in the backend, missing without an image */
  imageUpdatedAt?: string;
  address?: string;
  heightCm: number;
  weightKg: number;
  isMe?: boolean;
  horseIds: string[];
}

export function fullName(c: Customer): string {
  return `${c.firstName} ${c.lastName}`;
}
