// src/app/core/services/user.service.spec.ts

import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { environment } from '../../../environments/environment';

import { ApiResponse } from '../models/auth-model';
import {
  UserProfile,
  UserSession,
  EditUserProfileRequest,
  EditUserProfileResponse
} from '../models/user-model';
import { UserService } from './user-service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  const userUrl = `${environment.apiUrl}/User`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UserService]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getHeader() debe hacer GET a /User/header y mapear Data', () => {
    const profile: UserProfile = {
      userId: 1,
      email: 'test@example.com',
      name: 'Test'
    } as any;

    const apiResponse: ApiResponse<UserProfile> = {
      success: true,
      message: 'OK',
      data: profile
    };

    let received!: UserProfile;

    service.getHeader().subscribe(p => (received = p));

    const req = httpMock.expectOne(`${userUrl}/header`);
    expect(req.request.method).toBe('GET');

    req.flush(apiResponse);

    expect(received).toEqual(profile);
  });

  it('getProfile() debe hacer GET a /User/profile y mapear Data', () => {
    const profile: UserProfile = {
      userId: 1,
      email: 'profile@example.com',
      name: 'Profile User'
    } as any;

    const apiResponse: ApiResponse<UserProfile> = {
      success: true,
      message: 'OK',
      data: profile
    };

    let received!: UserProfile;

    service.getProfile().subscribe(p => (received = p));

    const req = httpMock.expectOne(`${userUrl}/profile`);
    expect(req.request.method).toBe('GET');

    req.flush(apiResponse);

    expect(received).toEqual(profile);
  });

  it('getActiveSessions() debe hacer GET a /User/sessions y mapear Data', () => {
    const sessions: UserSession[] = [
      { sessionId: 'A', createdAt: '2025-01-01' } as any,
      { sessionId: 'B', createdAt: '2025-01-02' } as any
    ];

    const apiResponse: ApiResponse<UserSession[]> = {
      success: true,
      message: 'OK',
      data: sessions
    };

    let received!: UserSession[];

    service.getActiveSessions().subscribe(s => (received = s));

    const req = httpMock.expectOne(`${userUrl}/sessions`);
    expect(req.request.method).toBe('GET');

    req.flush(apiResponse);

    expect(received).toEqual(sessions);
  });

  it('updateProfile() debe hacer PUT a /User/profile con el body correcto', () => {
    const body: EditUserProfileRequest = {
      name: 'New Name'
    } as any;

    const mockResponse: EditUserProfileResponse = {
      success: true,
      message: 'Updated'
    } as any;

    let received!: EditUserProfileResponse;

    service.updateProfile(body).subscribe(r => (received = r));

    const req = httpMock.expectOne(`${userUrl}/profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);

    req.flush(mockResponse);

    expect(received).toEqual(mockResponse);
  });
});
