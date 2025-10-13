import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NFLTeamListItem } from '../../../../core/models/nfl-team.model';

@Component({
  selector: 'app-team-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './team-card.html',
  styleUrls: ['./team-card.css']
})
export class TeamCard {
  @Input() team!: NFLTeamListItem;
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
    // evita loop si ya tiene el fallback
    if (!el.src.startsWith('data:image/svg+xml')) {
      el.src = this.fallbackSrc;
    }
  }
}
