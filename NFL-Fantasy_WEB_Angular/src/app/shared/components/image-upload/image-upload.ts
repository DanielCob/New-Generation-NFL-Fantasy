// image-upload.component.ts
import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { getImageDimensions, imageTypeValidator } from '../../validators/image.validator';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-image-upload',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './image-upload.html',
  styleUrls: ['./image-upload.css']
})
export class ImageUploadComponent {
  private snackBar = inject(MatSnackBar);

  @Input() currentImageUrl?: string;
  @Input() label = 'Subir Imagen';
  @Input() acceptedTypes = environment.allowedImageTypes.join(',');

  @Output() imageSelected = new EventEmitter<{
    url: string;
    width: number;
    height: number;
    bytes: number;
  }>();

  previewUrl?: string;
  isUploading = false;

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    // Validar tipo
    if (!imageTypeValidator(file)) {
      this.snackBar.open('Solo se permiten imágenes JPEG o PNG', 'Cerrar', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    try {
      this.isUploading = true;
      const dimensions = await getImageDimensions(file);

      // Validar dimensiones
      if (dimensions.width < environment.minImageDimension ||
          dimensions.width > environment.maxImageDimension ||
          dimensions.height < environment.minImageDimension ||
          dimensions.height > environment.maxImageDimension) {
        this.snackBar.open(
          `La imagen debe tener entre ${environment.minImageDimension}x${environment.minImageDimension}px y ${environment.maxImageDimension}x${environment.maxImageDimension}px`,
          'Cerrar',
          { duration: 5000, panelClass: ['error-snackbar'] }
        );
        this.isUploading = false;
        return;
      }

      // Validar tamaño
      const maxBytes = environment.maxImageSizeMB * 1024 * 1024;
      if (dimensions.bytes > maxBytes) {
        this.snackBar.open(
          `La imagen no debe superar ${environment.maxImageSizeMB}MB`,
          'Cerrar',
          { duration: 3000, panelClass: ['error-snackbar'] }
        );
        this.isUploading = false;
        return;
      }

      // Crear preview
      const reader = new FileReader();
      reader.onload = () => {
        this.previewUrl = reader.result as string;
        this.imageSelected.emit({
          url: this.previewUrl,
          width: dimensions.width,
          height: dimensions.height,
          bytes: dimensions.bytes
        });
      };
      reader.readAsDataURL(file);
    } catch (error) {
      this.snackBar.open('Error al procesar la imagen', 'Cerrar', {
        duration: 3000,
        panelClass: ['error-snackbar']
      });
    } finally {
      this.isUploading = false;
    }
  }

  removeImage(): void {
    this.previewUrl = undefined;
    this.imageSelected.emit({
      url: '',
      width: 0,
      height: 0,
      bytes: 0
    });
  }
}
