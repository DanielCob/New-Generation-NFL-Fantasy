import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface FiltersChange {
  searchTeam?: string;
  city?: string;
  isActive?: boolean;
}

@Component({
  selector: 'app-filters-panel',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule
  ],
  templateUrl: './filters-panel.html',
  styleUrls: ['./filters-panel.css']
})
export class FiltersPanel {
  @Input() initialSearch: string | undefined;
  @Input() initialCity: string | undefined;

  @Output() filtersChange = new EventEmitter<FiltersChange>();

  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      searchTeam: [''],
      city: [''],
      isActive: [undefined]
    });
  }

  ngOnInit(): void {
    this.form.patchValue({
      searchTeam: this.initialSearch ?? '',
      city: this.initialCity ?? ''
    });
  }

  apply(): void {
    const v = this.form.value;
    this.filtersChange.emit({
      searchTeam: (v.searchTeam || '').trim() || undefined,
      city: (v.city || '').trim() || undefined,
      isActive: v.isActive
    });
  }

  clear(): void {
    this.form.reset({ searchTeam: '', city: '', isActive: undefined });
    this.apply();
  }
}
