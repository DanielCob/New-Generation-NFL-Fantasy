import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { TeamService } from '../../../core/services/team.service';
import { ApiResponse } from '../../../core/models/common.model';
import { UpdateTeamBrandingDTO, MyTeamResponse } from '../../../core/models/team.model';

@Component({
  selector: 'app-edit-branding',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatProgressSpinnerModule,
     MatFormFieldModule, MatInputModule
  ],
  templateUrl: './edit-branding.html',
  styleUrls: ['./edit-branding.css'],
})
export class EditBrandingComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snack = inject(MatSnackBar);
  private teamSrv = inject(TeamService);

  teamId = signal<number>(0);
  saving = signal(false);

  form = this.fb.group({
    teamName: ['', [Validators.required, Validators.minLength(2)]],
    // metadatos opcionales
    teamImageUrl: [''],
    teamImageWidth: [''],
    teamImageHeight: [''],
    teamImageBytes: [''],
    thumbnailUrl: [''],
    thumbnailWidth: [''],
    thumbnailHeight: [''],
    thumbnailBytes: [''],
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.teamId.set(id);
    // Prefill nombre e imagen desde /my-team para mejor UX
    this.teamSrv.getMyTeam(id).subscribe({
      next: (res: ApiResponse<MyTeamResponse>) => {
        if (res?.data) {
          this.form.patchValue({
            teamName: res.data.teamName ?? '',
            teamImageUrl: res.data.teamImageUrl ?? '',
            thumbnailUrl: res.data.thumbnailUrl ?? ''
          });
        }
      }
    });
  }

  private toNumberOrUndef(x: any): number | undefined {
    const n = Number(x);
    return Number.isFinite(n) ? n : undefined;
  }

  save(): void {
    if (this.saving() || this.form.invalid) return;
    const v = this.form.value;

    // Solo enviar opcionales con valor real
    const dto: UpdateTeamBrandingDTO = {
      ...(v.teamName?.trim() ? { teamName: v.teamName.trim() } : {}),
      ...(v.teamImageUrl?.trim()
        ? {
            teamImageUrl: v.teamImageUrl.trim(),
            ...(this.toNumberOrUndef(v.teamImageWidth) !== undefined ? { teamImageWidth: this.toNumberOrUndef(v.teamImageWidth)! } : {}),
            ...(this.toNumberOrUndef(v.teamImageHeight) !== undefined ? { teamImageHeight: this.toNumberOrUndef(v.teamImageHeight)! } : {}),
            ...(this.toNumberOrUndef(v.teamImageBytes)  !== undefined ? { teamImageBytes:  this.toNumberOrUndef(v.teamImageBytes)! }  : {}),
            ...(v.thumbnailUrl?.trim() ? { thumbnailUrl: v.thumbnailUrl.trim() } : {}),
            ...(this.toNumberOrUndef(v.thumbnailWidth)  !== undefined ? { thumbnailWidth:  this.toNumberOrUndef(v.thumbnailWidth)! }  : {}),
            ...(this.toNumberOrUndef(v.thumbnailHeight) !== undefined ? { thumbnailHeight: this.toNumberOrUndef(v.thumbnailHeight)! } : {}),
            ...(this.toNumberOrUndef(v.thumbnailBytes)  !== undefined ? { thumbnailBytes:  this.toNumberOrUndef(v.thumbnailBytes)! }  : {}),
          }
        : {})
    };

    this.saving.set(true);
    this.teamSrv.updateBranding(this.teamId(), dto).subscribe({
      next: (res) => {
        if (res.success) {
          this.snack.open(res.message || 'Branding updated', 'Close', { duration: 2500 });
          this.router.navigate(['/teams', this.teamId(), 'my-team']);
        } else {
          this.snack.open(res.message || 'Could not update branding', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        }
        this.saving.set(false);
      },
      error: () => {
        this.snack.open('Could not update branding', 'Close', { duration: 3500, panelClass: ['error-snackbar'] });
        this.saving.set(false);
      }
    });
  }
}
