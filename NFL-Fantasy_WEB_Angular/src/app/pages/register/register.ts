// src/app/pages/register/register.ts
import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatStepperModule } from '@angular/material/stepper';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { User } from '../../core/services/user';
import { Location } from '../../core/services/location';
import { Province, Canton, District } from '../../core/models/location.model';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatStepperModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSlideToggleModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register implements OnInit {
  private fb = inject(FormBuilder);
  private userService = inject(User);
  private locationService = inject(Location);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  hidePassword = signal(true);
  isLoading = signal(false);
  isEngineer = signal(false);
  
  provinces = signal<Province[]>([]);
  cantons = signal<Canton[]>([]);
  districts = signal<District[]>([]);

  maxDate = new Date();
  minDate = new Date(1900, 0, 1);

  personalInfoForm: FormGroup = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    firstName: ['', [Validators.required]],
    lastSurname: ['', [Validators.required]],
    secondSurname: [''],
    birthDate: ['', [Validators.required, this.ageValidator()]]
  });

  accountForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, this.emailValidator()]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  locationForm: FormGroup = this.fb.group({
    provinceID: ['', Validators.required],
    cantonID: ['', Validators.required],
    districtID: ['']
  });

  professionalForm: FormGroup = this.fb.group({
    career: [''],
    specialization: ['']
  });

  ngOnInit(): void {
    this.maxDate.setFullYear(this.maxDate.getFullYear() - 18);
    this.loadProvinces();
    this.setupLocationSubscriptions();
    this.setupProfessionalValidation();
  }

  private setupLocationSubscriptions(): void {
    this.locationForm.get('provinceID')?.valueChanges.subscribe(provinceId => {
      if (provinceId) {
        this.loadCantons(provinceId);
        // Reset to empty values instead of empty strings
        this.locationForm.patchValue({ 
          cantonID: null, 
          districtID: null 
        });
        this.cantons.set([]);
        this.districts.set([]);
      }
    });

    this.locationForm.get('cantonID')?.valueChanges.subscribe(cantonId => {
      if (cantonId) {
        this.loadDistricts(cantonId);
        // Reset to null instead of empty string
        this.locationForm.patchValue({ 
          districtID: null 
        });
        this.districts.set([]);
      }
    });
  }

  private setupProfessionalValidation(): void {
    // Update validators when user type changes
    this.professionalForm.get('career')?.setValidators(
      this.isEngineer() ? [Validators.required] : []
    );
  }

  toggleUserType(): void {
    this.isEngineer.update(value => !value);
    
    // Update email validators based on user type
    const emailControl = this.accountForm.get('email');
    if (this.isEngineer()) {
      emailControl?.setValidators([Validators.required, this.engineerEmailValidator()]);
      this.professionalForm.get('career')?.setValidators([Validators.required]);
    } else {
      emailControl?.setValidators([Validators.required, this.clientEmailValidator()]);
      this.professionalForm.get('career')?.clearValidators();
    }
    emailControl?.updateValueAndValidity();
    this.professionalForm.get('career')?.updateValueAndValidity();
  }

  private loadProvinces(): void {
    this.locationService.getProvinces().subscribe({
      next: (provinces) => this.provinces.set(provinces),
      error: () => this.snackBar.open('Error loading provinces', 'Close', { duration: 3000 })
    });
  }

  private loadCantons(provinceId: number): void {
    this.locationService.getCantonsByProvince(provinceId).subscribe({
      next: (cantons) => this.cantons.set(cantons),
      error: () => this.snackBar.open('Error loading cantons', 'Close', { duration: 3000 })
    });
  }

  private loadDistricts(cantonId: number): void {
    this.locationService.getDistrictsByCanton(cantonId).subscribe({
      next: (districts) => this.districts.set(districts),
      error: () => this.snackBar.open('Error loading districts', 'Close', { duration: 3000 })
    });
  }

  private ageValidator() {
    return (control: AbstractControl) => {
      if (!control.value) return null;
      const birthDate = new Date(control.value);
      const today = new Date();
      const age = today.getFullYear() - birthDate.getFullYear();
      return age >= 18 ? null : { underage: true };
    };
  }

  private emailValidator() {
    return (control: AbstractControl) => {
      if (!control.value) return null;
      const email = control.value.toLowerCase();
      
      if (this.isEngineer() && !email.endsWith('@ing.com')) {
        return { invalidEngineerEmail: true };
      }
      if (!this.isEngineer() && (email.endsWith('@ing.com') || email.endsWith('@admin.com'))) {
        return { invalidClientEmail: true };
      }
      return null;
    };
  }

  private engineerEmailValidator() {
    return (control: AbstractControl) => {
      if (!control.value) return null;
      return control.value.toLowerCase().endsWith('@ing.com') ? null : { invalidEngineerEmail: true };
    };
  }

  private clientEmailValidator() {
    return (control: AbstractControl) => {
      if (!control.value) return null;
      const email = control.value.toLowerCase();
      if (email.endsWith('@ing.com') || email.endsWith('@admin.com')) {
        return { invalidClientEmail: true };
      }
      return null;
    };
  }

  private passwordMatchValidator(group: AbstractControl) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  onSubmit(): void {
    if (this.isFormValid() && !this.isLoading()) {
      this.isLoading.set(true);

      const formData = {
        ...this.personalInfoForm.value,
        ...this.accountForm.value,
        ...this.locationForm.value,
        ...(this.isEngineer() ? this.professionalForm.value : {})
      };

      delete formData.confirmPassword;

      const request = this.isEngineer()
        ? this.userService.createEngineer(formData)
        : this.userService.createClient(formData);

      request.subscribe({
        next: (response) => {
          if (response.success) {
            this.snackBar.open('Registration successful! Please login.', 'Close', {
              duration: 5000,
              panelClass: ['success-snackbar']
            });
            this.router.navigate(['/login']);
          } else {
            this.snackBar.open(response.message || 'Registration failed', 'Close', {
              duration: 5000,
              panelClass: ['error-snackbar']
            });
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          this.isLoading.set(false);
          this.snackBar.open(error.error?.message || 'Registration failed', 'Close', {
            duration: 5000,
            panelClass: ['error-snackbar']
          });
        }
      });
    }
  }

  public isFormValid(): boolean {
    const baseValid = this.personalInfoForm.valid && 
                     this.accountForm.valid && 
                     this.locationForm.valid;
    
    if (this.isEngineer()) {
      return baseValid && this.professionalForm.valid;
    }
    
    return baseValid;
  }
}