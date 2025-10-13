import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Reset } from './reset';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { of, throwError } from 'rxjs';

describe('ResetComponent', () => {
  let component: Reset;
  let fixture: ComponentFixture<Reset>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['resetWithToken']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockActivatedRoute = {
      queryParams: of({ token: 'test-token-123' })
    };

    await TestBed.configureTestingModule({
      imports: [Reset],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Reset);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should extract token from query parameters on init', () => {
    expect(component.token()).toBe('test-token-123');
  });

  it('should show error when token is missing', () => {
    // Simular que no hay token
    mockActivatedRoute.queryParams = of({});
    fixture = TestBed.createComponent(Reset);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isError()).toBeTrue();
    expect(component.message()).toContain('Token de restablecimiento no válido');
  });

  it('should show error when passwords do not match', () => {
    component.newPassword.set('password123');
    component.confirmPassword.set('differentpassword');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toContain('Las contraseñas no coinciden');
  });

  it('should show error when password is too short', () => {
    component.newPassword.set('123');
    component.confirmPassword.set('123');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toContain('al menos 6 caracteres');
  });

  it('should show error when fields are empty', () => {
    component.newPassword.set('');
    component.confirmPassword.set('');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toContain('completa todos los campos');
  });

  it('should call authService.resetWithToken when form is valid', () => {
    // Usar any para evitar problemas con la interfaz no exportada
    const mockResponse = { 
      success: true, 
      message: 'Password reset successful',
      data: 'success' 
    };
    mockAuthService.resetWithToken.and.returnValue(of(mockResponse));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    expect(mockAuthService.resetWithToken).toHaveBeenCalledWith(
      'test-token',
      'newpassword',
      'newpassword'
    );
  });

  it('should handle reset password success', () => {
    const mockResponse = { 
      success: true, 
      message: 'Password reset successful',
      data: 'success' 
    };
    mockAuthService.resetWithToken.and.returnValue(of(mockResponse));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    expect(component.isSuccess()).toBeTrue();
    expect(component.message()).toBe('Password reset successful');
    expect(component.isLoading()).toBeFalse();
  });

  it('should handle reset password error from server', () => {
    const mockError = { error: { message: 'Token expired' } };
    mockAuthService.resetWithToken.and.returnValue(throwError(() => mockError));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toBe('Token expired');
    expect(component.isLoading()).toBeFalse();
  });

  it('should handle reset password with generic error', () => {
    mockAuthService.resetWithToken.and.returnValue(throwError(() => ({})));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toBe('Error del servidor. Intenta nuevamente.');
    expect(component.isLoading()).toBeFalse();
  });

  it('should handle reset password success but with success false', () => {
    const mockResponse = { 
      success: false, 
      message: 'Token invalid',
      data: 'error' 
    };
    mockAuthService.resetWithToken.and.returnValue(of(mockResponse));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    expect(component.isError()).toBeTrue();
    expect(component.message()).toBe('Token invalid');
    expect(component.isLoading()).toBeFalse();
  });

  it('should redirect to login on success after timeout', (done) => {
    const mockResponse = { 
      success: true, 
      message: 'Password reset successful',
      data: 'success' 
    };
    mockAuthService.resetWithToken.and.returnValue(of(mockResponse));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    component.onSubmit();
    
    setTimeout(() => {
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
      done();
    }, 3000);
  });

  it('should navigate to login when goToLogin is called', () => {
    component.goToLogin();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should not submit when loading', () => {
    const mockResponse = { 
      success: true, 
      message: 'Password reset successful',
      data: 'success' 
    };
    mockAuthService.resetWithToken.and.returnValue(of(mockResponse));

    component.token.set('test-token');
    component.newPassword.set('newpassword');
    component.confirmPassword.set('newpassword');
    
    // Simular que ya está loading
    component.isLoading.set(true);
    
    component.onSubmit();
    
    // No debería llamar al servicio porque ya está loading
    expect(mockAuthService.resetWithToken).not.toHaveBeenCalled();
  });
});