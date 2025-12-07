// src/app/pages/_facades/navbar.facade.spec.ts

import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { NavbarFacade } from './navbar.facade';
import { AuthService } from '../../core/services/auth-service';
import { UserService } from '../../core/services/user-service';

// ---- Mocks sencillos ---- //

class MockAuthService {
  private sessionSubject = new BehaviorSubject<any>(null);
  session$ = this.sessionSubject.asObservable();

  setSession(session: any) {
    this.sessionSubject.next(session);
  }
}

class MockUserService {
  // se sobreescribe en cada test
  getProfile = jasmine.createSpy('getProfile');
}

describe('NavbarFacade', () => {
  let facade: NavbarFacade;
  let auth: MockAuthService;
  let users: MockUserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        NavbarFacade,
        { provide: AuthService, useClass: MockAuthService },
        { provide: UserService, useClass: MockUserService },
      ],
    });

    facade = TestBed.inject(NavbarFacade);
    auth = TestBed.inject(AuthService) as unknown as MockAuthService;
    users = TestBed.inject(UserService) as unknown as MockUserService;
  });

  it('should be created', () => {
    expect(facade).toBeTruthy();
  });

  it('isAdmin debe ser true cuando SystemRoleCode es ADMIN', () => {
    // Arrange
    const session = {
      SessionID: 'ABC',
      UserID: 1,
      Name: 'Admin',
      SystemRoleCode: 'ADMIN',
    };

    // Act
    auth.setSession(session);

    // Assert
    expect(facade.isAdmin()).toBeTrue();
  });

  it('isAdmin debe ser false cuando no hay sesión o el rol no es admin', () => {
    // Sin sesión
    auth.setSession(null);
    expect(facade.isAdmin()).toBeFalse();

    // Rol diferente
    auth.setSession({
      SessionID: 'XYZ',
      UserID: 1,
      SystemRoleCode: 'USER',
    });
    expect(facade.isAdmin()).toBeFalse();
  });

  it('loadMyLeagues debe mapear CommissionedLeagues del perfil y actualizar signals', () => {
    const profile = {
      CommissionedLeagues: [
        { LeagueID: 1, LeaguePublicID: 1001, LeagueName: 'Liga 1', Status: 1 },
        { LeagueID: 2, LeaguePublicID: 1002, LeagueName: 'Liga 2', Status: 2 },
      ],
    };

    users.getProfile.and.returnValue(of(profile));

    // Act
    facade.loadMyLeagues();

    // Assert
    const leagues = facade.leagues();
    expect(leagues.length).toBe(2);
    expect(leagues[0]).toEqual({
      LeagueID: 1,
      LeaguePublicID: 1001,
      LeagueName: 'Liga 1',
      Status: 1,
    });
    expect(leagues[1]).toEqual({
      LeagueID: 2,
      LeaguePublicID: 1002,
      LeagueName: 'Liga 2',
      Status: 2,
    });

    expect(facade.leaguesLoading()).toBeFalse();
    expect(facade.leaguesError()).toBeNull();
  });

  it('loadMyLeagues debe manejar error dejando lista vacía y mensaje de error', () => {
    users.getProfile.and.returnValue(throwError(() => new Error('fail')));

    facade.loadMyLeagues();

    expect(facade.leagues()).toEqual([]);
    expect(facade.leaguesLoading()).toBeFalse();
    expect(facade.leaguesError()).toBe('No se pudieron cargar tus ligas');
  });

  it('loadMyLeagues no debe disparar otra petición si ya está cargando', () => {
    // Forzamos estado "cargando"
    facade.leaguesLoading.set(true);

    // Espiamos getProfile para ver si se llama o no
    users.getProfile.and.returnValue(of({ CommissionedLeagues: [] }));

    facade.loadMyLeagues();

    // Como leaguesLoading ya era true, NO debería haberse llamado
    expect(users.getProfile).not.toHaveBeenCalled();
  });
});
