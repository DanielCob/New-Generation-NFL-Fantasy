import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NFLTeamListItem } from '../../../../core/models/nfl-team-model';
import { AuthzService } from '../../../../core/services/authz/authz.service';

@Component({
  selector: 'app-team-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './team-card.html',
  styleUrls: ['./team-card.css']
})
export class TeamCard {
  // Datos del equipo
  @Input({ required: true }) team!: NFLTeamListItem;

  // Permiso directo desde AuthzService (misma “verdad” que el Create)
  private authz = inject(AuthzService);
  readonly isAdmin = toSignal(this.authz.isAdmin$, { initialValue: false });

  @Output() view = new EventEmitter<void>();
  @Output() edit = new EventEmitter<void>();

  /** Fallback embebido (no requiere assets) */
  readonly fallbackSrc =
    'data:image/svg+xml;charset=UTF-8,' +
    encodeURIComponent(
      `<svg xmlns='http://www.w3.org/2000/svg' width='320' height='180' viewBox='0 0 320 180'>
        <rect width='320' height='180' fill='#e5e7eb'/>
        <text x='50%' y='50%' font-family='Arial, Helvetica, sans-serif' font-size='18'
              text-anchor='middle' dominant-baseline='middle' fill='#6b7280'>NFL Team</text>
      </svg>`
    );

  get imgUrl(): string {
    return this.team?.ThumbnailUrl || this.team?.TeamImageUrl || this.fallbackSrc;
  }

  onImgError(ev: Event) {
    const el = ev.target as HTMLImageElement;
    if (!el.src.startsWith('data:image/svg+xml')) {
      el.src = this.fallbackSrc;
    }
  }
}
