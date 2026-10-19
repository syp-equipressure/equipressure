export interface Horse {
  id: string;
  name: string;
  age: number;
  breed: string;
  imageUrl: string;
  ownerId: string;
  heightCm: number;
  weightKg: number;
  sex?: 'Männlich' | 'Weiblich';
  birthDate?: string;
  address?: string;
}
