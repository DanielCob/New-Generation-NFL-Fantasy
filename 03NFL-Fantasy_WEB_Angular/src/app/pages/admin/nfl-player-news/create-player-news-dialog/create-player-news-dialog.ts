// 10. create-player-news-dialog.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import { CreatePlayerNewsDTO, NFLPlayerListItem, INJURY_DESIGNATIONS } from '../../../../core/models/nfl-player-model';

@Component({
  selector: 'app-create-player-news-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './create-player-news-dialog.html',
  styleUrl: './create-player-news-dialog.css'
})
export class CreatePlayerNewsDialog {
  private dialogRef = inject(MatDialogRef<CreatePlayerNewsDialog>);
  private data = inject(MAT_DIALOG_DATA);
  private svc = inject(NFLPlayerService);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  player: NFLPlayerListItem = this.data.player;
  loading = signal(false);
  
  readonly designations = INJURY_DESIGNATIONS;

  form = this.fb.group({
    NewsText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(300)]],
    IsInjury: [false],
    InjurySummary: ['', [Validators.maxLength(30)]],
    Designation: ['']
  });

  ngOnInit(): void {
    // Watch IsInjury changes
    this.form.get('IsInjury')?.valueChanges.subscribe(isInjury => {
      if (isInjury) {
        this.form.get('InjurySummary')?.setValidators([Validators.required, Validators.maxLength(30)]);
        this.form.get('Designation')?.setValidators([Validators.required]);
      } else {
        this.form.get('InjurySummary')?.clearValidators();
        this.form.get('Designation')?.clearValidators();
        this.form.patchValue({ InjurySummary: '', Designation: '' });
      }
      this.form.get('InjurySummary')?.updateValueAndValidity();
      this.form.get('Designation')?.updateValueAndValidity();
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snack.open('Por favor completa todos los campos requeridos', 'OK', { duration: 3000 });
      return;
    }

    const formValue = this.form.value;
    const dto: CreatePlayerNewsDTO = {
      NFLPlayerID: this.player.NFLPlayerID,
      NewsText: formValue.NewsText!,
      IsInjury: formValue.IsInjury || false,
      InjurySummary: formValue.IsInjury ? formValue.InjurySummary! : undefined,
      Designation: formValue.IsInjury ? formValue.Designation as any : undefined
    };

    console.log('📤 [CreateNewsDialog] Enviando:', dto);
    this.loading.set(true);

    this.svc.createPlayerNews(dto).subscribe({
      next: (response) => {
        console.log('✅ [CreateNewsDialog] Noticia creada:', response);
        this.snack.open(response.Message || 'Noticia creada exitosamente', 'OK', { duration: 3000 });
        this.dialogRef.close({ created: true, newsId: response.Data?.NewsID });
      },
      error: (err) => {
        console.error('❌ [CreateNewsDialog] Error:', err);
        const msg = err?.error?.message || err?.error?.Message || 'Error creando noticia';
        this.snack.open(msg, 'OK', { duration: 4000 });
        this.loading.set(false);
      }
    });
  }

  get isInjury(): boolean {
    return this.form.get('IsInjury')?.value || false;
  }

  getCharCount(field: string): string {
    const value = this.form.get(field)?.value || '';
    const max = field === 'NewsText' ? 300 : 30;
    return `${value.length}/${max}`;
  }
}