// src/app/core/models/user.model.ts
export interface CreateClientDTO {
  username: string;
  firstName: string;
  lastSurname: string;
  secondSurname?: string;
  email: string;
  password: string;
  birthDate: Date;
  provinceID: number;
  cantonID: number;
  districtID?: number;
}

export interface CreateEngineerDTO extends CreateClientDTO {
  career: string;
  specialization?: string;
}

export interface UserResponse {
  userID: number;
  username: string;
  firstName: string;
  lastSurname: string;
  secondSurname?: string;
  email: string;
  birthDate: Date;
  age: number;
  userType: string;
  provinceName: string;
  cantonName: string;
  districtName?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface EngineerResponse extends UserResponse {
  career: string;
  specialization?: string;
}

export interface CurrentUser {
  userId: number;
  userType: 'CLIENT' | 'ENGINEER' | 'ADMIN';
  sessionToken: string;
  email: string;
  firstName: string;
  lastSurname: string;
}