import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';

export interface FiltersChange {
  searchTeam?: string;
  city?: string;
  isActive?: boolean; // undefined => no enviar el parámetro
}

@Component({
  selector: 'app-filters-panel',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatSelectModule
  ],
  templateUrl: './filters-panel.html',
  styleUrls: ['./filters-panel.css']
})
export class FiltersPanel {
  @Input() initialSearch: string | undefined;
  @Input() initialCity: string | undefined;
  @Input() initialIsActive: boolean | undefined;

  @Output() filtersChange = new EventEmitter<FiltersChange>();

  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      searchTeam: [''],
      city: [''],
      isActive: [undefined as boolean | undefined] // tri-state: undefined/true/false
    });
  }

  ngOnInit(): void {
    this.form.patchValue({
      searchTeam: this.initialSearch ?? '',
      city: this.initialCity ?? '',
      isActive: this.initialIsActive // puede ser undefined/true/false
    });
  }

  apply(): void {
    const v = this.form.value;
    const searchTeam = (v.searchTeam || '').trim();
    const city = (v.city || '').trim();

    // mantener undefined cuando el usuario deja "Any"
    const isActive: boolean | undefined =
      v.isActive === true ? true : (v.isActive === false ? false : undefined);

    this.filtersChange.emit({
      searchTeam: searchTeam.length ? searchTeam : undefined,
      city: city.length ? city : undefined,
      isActive
    });
  }

  clear(): void {
    this.form.reset({ searchTeam: '', city: '', isActive: undefined });
    this.apply();
  }
}
