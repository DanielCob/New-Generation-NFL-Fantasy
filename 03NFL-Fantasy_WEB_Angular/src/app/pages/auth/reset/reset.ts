import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth-service';

const normalizeResponse = (r: any) => ({
  success: (r?.success ?? r?.Success) ?? false,
  message: (r?.message ?? r?.Message) ?? '',
  data: (r?.data ?? r?.Data) ?? ''
});

@Component({
  selector: 'app-reset',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reset.html',
  styleUrls: ['./reset.css']
})
export class Reset implements OnInit {
  token = signal<string>('');
  newPassword = signal<string>('');
  confirmPassword = signal<string>('');
  isLoading = signal<boolean>(false);
  message = signal<string>('');
  isError = signal<boolean>(false);
  isSuccess = signal<boolean>(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    // Obtener el token de los query parameters
    this.route.queryParams.subscribe(params => {
      const tokenParam = params['token'];
      if (tokenParam) {
        this.token.set(tokenParam);
      } else {
        this.message.set('Token de restablecimiento no válido');
        this.isError.set(true);
      }
    });
  }

  onSubmit() {
    // Validaciones
    if (!this.token()) {
      this.message.set('Token no válido');
      this.isError.set(true);
      return;
    }

    if (!this.newPassword() || !this.confirmPassword()) {
      this.message.set('Por favor, completa todos los campos');
      this.isError.set(true);
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.message.set('Las contraseñas no coinciden');
      this.isError.set(true);
      return;
    }

    if (this.newPassword().length < 8) {
      this.message.set('La contraseña debe tener al menos 6 caracteres');
      this.isError.set(true);
      return;
    }

    this.isLoading.set(true);
    this.message.set('');
    this.isError.set(false);


    // Usar el método existente del AuthService
    this.authService.resetWithToken(
      this.token(),
      this.newPassword(),
      this.confirmPassword()
    ).subscribe({
      next: (response) => {
        this.isLoading.set(false);

        const normalizedResponse = normalizeResponse(response);
        
        if (normalizedResponse.success) {
          this.isSuccess.set(true);
          this.message.set(response.message || 'Contraseña restablecida exitosamente');
          
          // Redirigir al login después de 3 segundos
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 3000);
        } else {
          this.isError.set(true);
          this.message.set(response.message || 'Error al restablecer la contraseña');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.isError.set(true);
        this.message.set(error.error?.message || 'Error del servidor. Intenta nuevamente.');
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}