// src/app/pages/teams/my-team/subcomponents/roster-filters/roster-filters.component.ts
import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-roster-filters',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule],
  template: `
  <div class="filters">
    <mat-form-field appearance="outline">
      <mat-label>Position</mat-label>
      <mat-select [(ngModel)]="position">
        <mat-option [value]="undefined">Any</mat-option>
        <mat-option value="QB">QB</mat-option>
        <mat-option value="RB">RB</mat-option>
        <mat-option value="WR">WR</mat-option>
        <mat-option value="TE">TE</mat-option>
        <mat-option value="K">K</mat-option>
        <mat-option value="DEF">DEF</mat-option>
      </mat-select>
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Search</mat-label>
      <input matInput [(ngModel)]="search" (keyup.enter)="apply()" />
      <button mat-icon-button matSuffix (click)="search = ''; apply()" *ngIf="search">
        <mat-icon>close</mat-icon>
      </button>
    </mat-form-field>

    <button mat-flat-button color="primary" (click)="apply()">
      <mat-icon>filter_alt</mat-icon> Apply
    </button>
  </div>
  `,
  styles: [`.filters{display:flex;gap:12px;align-items:center;flex-wrap:wrap}`]
})
export class RosterFiltersComponent {
  position: string | undefined;
  search: string | undefined;

  @Output() filtersChange = new EventEmitter<{ position?: string; search?: string }>();

  apply() {
    this.filtersChange.emit({ position: this.position, search: this.search });
  }
}
