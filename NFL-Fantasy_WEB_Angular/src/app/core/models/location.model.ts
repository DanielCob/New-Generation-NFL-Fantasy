// src/app/core/models/location.model.ts
export interface Province {
  provinceID: number;
  provinceName: string;
  createdAt: Date;
}

export interface Canton {
  cantonID: number;
  provinceID: number;
  cantonName: string;
  provinceName: string;
  createdAt: Date;
}

export interface District {
  districtID: number;
  cantonID: number;
  districtName: string;
  cantonName: string;
  provinceID: number;
  provinceName: string;
  createdAt: Date;
}