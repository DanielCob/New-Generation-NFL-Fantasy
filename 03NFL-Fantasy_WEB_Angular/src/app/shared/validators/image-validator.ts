import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { environment } from '../../../environments/environment';

/**
 * Valida dimensiones de imagen (300-1024px)
 */
export function imageDimensionsValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const width = control.get('teamImageWidth')?.value;
    const height = control.get('teamImageHeight')?.value;

    if (!width || !height) {
      return null; // Opcional, no validar si no hay imagen
    }

    const min = environment.minImageDimension;
    const max = environment.maxImageDimension;

    if (width < min || width > max || height < min || height > max) {
      return {
        invalidDimensions: {
          min,
          max,
          actual: { width, height }
        }
      };
    }

    return null;
  };
}

/**
 * Valida tamaño de imagen (max 5MB)
 */
export function imageSizeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const bytes = control.value;

    if (!bytes) {
      return null;
    }

    const maxBytes = environment.maxImageSizeMB * 1024 * 1024;

    if (bytes > maxBytes) {
      return {
        imageTooBig: {
          maxMB: environment.maxImageSizeMB,
          actualMB: (bytes / (1024 * 1024)).toFixed(2)
        }
      };
    }

    return null;
  };
}
/**
 * Valida tipo de imagen (JPEG/PNG)
 */
export function imageTypeValidator(file: File): boolean {
  return environment.allowedImageTypes.includes(file.type);
}

/**
 * Helper: obtiene dimensiones de una imagen
 */
export function getImageDimensions(file: File): Promise<{ width: number; height: number; bytes: number }> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const reader = new FileReader();

    reader.onload = (e: any) => {
      img.src = e.target.result;
      img.onload = () => {
        resolve({
          width: img.naturalWidth,
          height: img.naturalHeight,
          bytes: file.size
        });
      };
      img.onerror = reject;
    };

    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}
