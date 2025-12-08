// src/app/core/services/auth.service.spec.ts

import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { environment } from '../../../environments/environment';

import {
  ApiResponse,
  RegisterRequest,
  LoginRequest,
  LoginResponse,
  SimpleOkResponse
} from '../models/auth-model';
import { UserProfile } from '../models/user-model';
import { AuthService, AuthSession } from './auth-service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const authUrl = `${environment.apiUrl}/Auth`;
  const userUrl = `${environment.apiUrl}/User`;

  beforeEach(() => {
    // Limpia localStorage para que readSession() no devuelva nada raro
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('register() debe hacer POST a /Auth/register y mapear ApiResponse', () => {
    const payload: RegisterRequest = {
      email: 'test@example.com',
      password: 'Passw0rd!',
      confirmPassword: 'Passw0rd!'
    } as any;

    const mockRawResponse = {
      Success: true,
      Message: 'OK',
      Data: 'Registered'
    };

    let actualResponse: ApiResponse<string> | undefined;

    service.register(payload).subscribe((res: ApiResponse<string> | undefined) => {
      actualResponse = res;
    });

    const req = httpMock.expectOne(`${authUrl}/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);

    req.flush(mockRawResponse);

    expect(actualResponse).toEqual({
      success: true,
      message: 'OK',
      data: 'Registered'
    });
  });

  it('login() debe hacer POST a /Auth/login y persistir sesión cuando Success = true', () => {
    const body: LoginRequest = {
      email: 'user@example.com',
      password: '123456'
    } as any;

    const mockSession: AuthSession = {
      SessionID: 'ABC123',
      Message: 'Logged in',
      UserID: 1,
      Email: 'user@example.com',
      Name: 'User Test',
      SystemRoleCode: 'USER'
    };

    const mockResponse: LoginResponse = {
      Success: true,
      Message: 'OK',
      Data: mockSession
    };

    let received: LoginResponse | undefined;
    service.login(body).subscribe((res: LoginResponse | undefined) => (received = res));

    const req = httpMock.expectOne(`${authUrl}/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);

    req.flush(mockResponse);

    // respuesta cruda
    expect(received).toEqual(mockResponse);

    // sesión en servicio
    expect(service.session).toEqual(mockSession);

    // sesión en localStorage
    const raw = localStorage.getItem('xnf.session');
    expect(raw).toBeTruthy();
    if (raw) {
      expect(JSON.parse(raw)).toEqual(mockSession);
    }
  });

  it('logout() debe hacer POST a /Auth/logout y limpiar sesión en finalize', () => {
    // Primero metemos una sesión fake
    const fakeSession: AuthSession = {
      SessionID: 'XYZ',
      Message: 'Logged in',
      UserID: 1,
      Email: 'test@example.com',
      Name: 'Test'
    };
    (service as any)['persistSession'](fakeSession);
    expect(service.isAuthenticated()).toBeTrue();

    let response!: ApiResponse<string>;

    service.logout().subscribe((r: ApiResponse<string>) => (response = r));

    const req = httpMock.expectOne(`${authUrl}/logout`);
    expect(req.request.method).toBe('POST');

    req.flush({ Success: true, Message: 'OK', Data: 'Logged out' });

    // finalize() ya corrió
    expect(service.isAuthenticated()).toBeFalse();
    expect(localStorage.getItem('xnf.session')).toBeNull();
    expect(response.success).toBeTrue();
  });

  it('logoutAll() debe hacer POST a /Auth/logout-all y limpiar sesión', () => {
    const fakeSession: AuthSession = {
      SessionID: 'XYZ',
      Message: 'Logged in',
      UserID: 1,
      Email: 'test@example.com',
      Name: 'Test'
    };
    (service as any)['persistSession'](fakeSession);
    expect(service.isAuthenticated()).toBeTrue();

    let response!: ApiResponse<string>;

    service.logoutAll().subscribe((r: ApiResponse<string>) => (response = r));

    const req = httpMock.expectOne(`${authUrl}/logout-all`);
    expect(req.request.method).toBe('POST');

    req.flush({ Success: true, Message: 'OK', Data: 'Logged out all' });

    expect(service.isAuthenticated()).toBeFalse();
    expect(localStorage.getItem('xnf.session')).toBeNull();
    expect(response.success).toBeTrue();
  });

  it('requestReset() debe hacer POST a /Auth/request-reset', () => {
    const email = 'reset@example.com';
    const mockResponse: SimpleOkResponse = {
      success: true,
      message: 'OK'
    } as any;

    let received!: SimpleOkResponse;

    service.requestReset(email).subscribe((r: SimpleOkResponse) => (received = r));

    const req = httpMock.expectOne(`${authUrl}/request-reset`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email });

    req.flush(mockResponse);

    expect(received).toEqual(mockResponse);
  });

  it('getProfile() debe hacer GET a /User/header y mapear Data', () => {
    const mockProfile: UserProfile = {
      userId: 1,
      email: 'user@example.com',
      name: 'User',
      systemRoleCode: 'USER'
    } as any;

    const apiResponse: ApiResponse<UserProfile> = {
      success: true,
      message: 'OK',
      data: mockProfile
    };

    let received!: UserProfile;

    service.getProfile().subscribe((p: UserProfile) => (received = p));

    const req = httpMock.expectOne(`${userUrl}/header`);
    expect(req.request.method).toBe('GET');

    req.flush(apiResponse);

    expect(received).toEqual(mockProfile);
  });

  it('updateProfile() debe hacer POST a /User/update-profile', () => {
    const body = {
      name: 'New Name'
    } as any;

    const mockResponse: SimpleOkResponse = {
      success: true,
      message: 'Updated'
    } as any;

    let received!: SimpleOkResponse;

    service.updateProfile(body).subscribe((r: SimpleOkResponse) => (received = r));

    const req = httpMock.expectOne(`${userUrl}/update-profile`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);

    req.flush(mockResponse);

    expect(received).toEqual(mockResponse);
  });
});
