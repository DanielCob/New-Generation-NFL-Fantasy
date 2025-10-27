import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserProfile, EditUserProfileRequest } from '../../../core/models/user-model';
import { UserService } from '../../../core/services/user-service';
import { ImageStorageService, UploadResponse } from '../../../core/services/image-storage.service';

@Component({
  selector: 'app-profile-header',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatSelectModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile-header.html',
  styleUrls: ['./profile-header.css']
})
export class ProfileHeader implements OnInit {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  private imageService = inject(ImageStorageService);
  private snack = inject(MatSnackBar);

  readonly avatarFallback =
    'data:image/svg+xml;charset=UTF-8,' +
    encodeURIComponent(
      `<svg xmlns='http://www.w3.org/2000/svg' width='128' height='128' viewBox='0 0 128 128'>
        <rect width='128' height='128' fill='#e5e7eb'/>
        <circle cx='64' cy='48' r='22' fill='#cbd5e1'/>
        <rect x='24' y='80' width='80' height='28' rx='14' fill='#cbd5e1'/>
      </svg>`
    );

  loading = signal(true);
  saving  = signal(false);
  editing = signal(false);

  selectedFile: File | null = null;
  previewUrl = signal<string | null>(null);
  imageMeta = signal<{ width: number; height: number; bytes: number } | null>(null);
  computingImageMeta = signal(false);
  showAvatar = signal(true);

  profile = signal<UserProfile | null>(null);
  triedFullProfile = false;

  languages = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
  ];

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    alias: ['', [Validators.required, Validators.minLength(2)]],
    languageCode: ['en', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadHeader();
  }

  private loadHeader(): void {
    this.loading.set(true);
    this.userService.getHeader().subscribe({
      next: (p) => {
        console.log('Header loaded: ', p);
        this.profile.set(p);
        this.patchFormFromProfile(p);
        this.loading.set(false);

        const noImg = !p.ProfileImageUrl || !String(p.ProfileImageUrl).trim();
        if (noImg && !this.triedFullProfile) {
          this.triedFullProfile = true;
          this.userService.getProfile().subscribe({
            next: (fp) => {
              console.log('Full profile loaded: ', fp);
              if (fp?.ProfileImageUrl) {
                const curr = this.profile();
                this.profile.set({ ...(curr || fp), ProfileImageUrl: fp.ProfileImageUrl });
                this.previewUrl.set(fp.ProfileImageUrl);
              }
            },
            error: () => {}
          });
        }
      },
      error: (err) => {
        console.error('getHeader error:', err);
        this.snack.open('No se pudo cargar el perfil.', 'Cerrar', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  private patchFormFromProfile(p: UserProfile): void {
    this.form.patchValue({
      name: p.Name,
      alias: p.Alias,
      languageCode: p.LanguageCode
    });
    this.previewUrl.set(p.ProfileImageUrl ?? null);
  }

  enableEdit(): void { this.editing.set(true); }

  cancelEdit(): void {
    const p = this.profile();
    if (p) this.patchFormFromProfile(p);
    this.editing.set(false);
    this.selectedFile = null;
    this.previewUrl.set(p?.ProfileImageUrl ?? null);
    this.imageMeta.set(null);
  }

  avatarUrl(): string {
    return this.previewUrl() ?? this.profile()?.ProfileImageUrl ?? this.avatarFallback;
  }

  onAvatarError(ev: Event) {
    const el = ev.target as HTMLImageElement;
    if (!el.src.startsWith('data:image/svg+xml')) {
      el.src = this.avatarFallback;
    }
  }

  onFileSelected(ev: Event) {
    const input = ev.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    this.selectedFile = file;

    const reader = new FileReader();
    reader.onload = e => this.previewUrl.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  private async getImageMeta(file: File): Promise<{ width: number; height: number; bytes: number }> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => {
        const w = img.naturalWidth;
        const h = img.naturalHeight;
        if (w < 300 || w > 1024 || h < 300 || h > 1024) {
          reject(new Error('La imagen debe tener entre 300 y 1024 px de ancho y alto.'));
        } else {
          resolve({ width: w, height: h, bytes: file.size });
        }
      };
      img.onerror = () => reject(new Error('No se pudo cargar la imagen.'));
      img.src = URL.createObjectURL(file);
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);

    try {
      let imageUrl: string | null = null;

      if (this.selectedFile) {
        this.imageMeta.set(await this.getImageMeta(this.selectedFile));

        const res: UploadResponse | undefined = await this.imageService.uploadImage(this.selectedFile).toPromise();
        console.log('Image upload response: ', res);

        if (!res?.imageUrl) {
          console.warn('Image upload returned undefined or missing URL.');
          throw new Error('No se pudo obtener la URL de la imagen.');
        }

        imageUrl = res.imageUrl;
        this.previewUrl.set(imageUrl);
      }

      const body: EditUserProfileRequest = {
        Name: this.form.value.name,
        Alias: this.form.value.alias,
        LanguageCode: this.form.value.languageCode,
      };

      if (imageUrl && this.imageMeta()) {
        body.ProfileImageUrl = imageUrl;
        body.ProfileImageWidth = this.imageMeta()!.width;
        body.ProfileImageHeight = this.imageMeta()!.height;
        body.ProfileImageBytes = this.imageMeta()!.bytes;
      }

      console.log('PUT /User/profile body: ', body);

      this.userService.updateProfile(body).subscribe({
        next: (resp) => {
          console.log('Profile updated response: ', resp);
          this.saving.set(false);
          const p = this.profile();
          if (p) {
            this.profile.set({
              ...p,
              ...body
            });
          }
          this.editing.set(false);
          this.snack.open('Perfil actualizado.', 'Cerrar', { duration: 3000 });
        },
        error: (err) => {
          this.saving.set(false);
          console.error('Error updating profile: ', err);
          this.snack.open(err?.error?.message || 'Error al actualizar el perfil.', 'Cerrar', { duration: 5000 });
        }
      });

    } catch (e: any) {
      this.saving.set(false);
      console.error('Save error: ', e);
      this.snack.open(e.message ?? 'Error con la imagen.', 'Cerrar', { duration: 5000 });
    }
  }
}
