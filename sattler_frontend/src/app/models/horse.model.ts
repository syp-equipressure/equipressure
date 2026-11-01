export interface Horse {
  id: string;
  name: string;
  age: number;
  breed: string;
  imageUrl: string;
  /** ISO timestamp when the image was set in the backend, missing without an image */
  imageUpdatedAt?: string;
  ownerId: string;
  heightCm: number;
  weightKg: number;
  sex?: 'Männlich' | 'Weiblich';
  birthDate?: string;
  address?: string;
}
