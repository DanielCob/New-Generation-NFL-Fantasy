// src/app/shared/components/navigation-bar/navigation-bar.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';

import { NavigationBar } from './navigation-bar';
import { NavbarFacade } from '../../../pages/_facades/navbar.facade';
import { LeagueContextService } from '../../../core/services/context/league-context.service';
import { AuthService } from '../../../core/services/auth-service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';

// ---- Mocks ---- //

class MockNavbarFacade {
  session = signal<any>({
    SessionID: 'ABC',
    Name: 'Test User'
  });
  isAdmin = signal(false);
  leagues = signal<any[]>([]);
  leaguesLoading = signal(false);
  leaguesError = signal<string | null>(null);

  loadMyLeagues = jasmine.createSpy('loadMyLeagues');
}

class MockLeagueContextService {
  currentLeagueId = signal<number | null>(null);
  setLeague = jasmine.createSpy('setLeague');
  setTeam = jasmine.createSpy('setTeam');
}

class MockAuthService {
  logout = jasmine.createSpy('logout').and.returnValue(of({ success: true }));
}

const snackBarSpy = {
  open: jasmine.createSpy('open')
};

const dialogSpy = {
  open: jasmine.createSpy('open').and.returnValue({
    afterClosed: () => of(undefined)
  })
};

describe('NavigationBar', () => {
  let component: NavigationBar;
  let fixture: ComponentFixture<NavigationBar>;
  let router: Router;
  let facade: MockNavbarFacade;
  let ctx: MockLeagueContextService;
  let auth: MockAuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        NavigationBar,
        RouterTestingModule.withRoutes([])
      ],
      providers: [
        { provide: NavbarFacade, useClass: MockNavbarFacade },
        { provide: LeagueContextService, useClass: MockLeagueContextService },
        { provide: AuthService, useClass: MockAuthService },
        { provide: MatSnackBar, useValue: snackBarSpy },
        { provide: MatDialog, useValue: dialogSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NavigationBar);
    component = fixture.componentInstance;

    router = TestBed.inject(Router);
    facade = TestBed.inject(NavbarFacade) as unknown as MockNavbarFacade;
    ctx = TestBed.inject(LeagueContextService) as unknown as MockLeagueContextService;
    auth = TestBed.inject(AuthService) as unknown as MockAuthService;

    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    snackBarSpy.open.calls.reset();
    dialogSpy.open.calls.reset();

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('isLoggedIn y userName deben derivarse de la session', () => {
    expect(component.isLoggedIn()).toBeTrue();
    expect(component.userName()).toBe('Test User');

    // Sin sesión
    facade.session.set(null);
    expect(component.isLoggedIn()).toBeFalse();
    expect(component.userName()).toBe('User');
  });

  it('logout debe llamar AuthService.logout y navegar a /login', () => {
    component.logout();

    expect(auth.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('loadMyLeagues debe delegar en facade cuando no está cargando', () => {
    facade.leaguesLoading.set(false);
    component.loadMyLeagues();

    expect(facade.loadMyLeagues).toHaveBeenCalled();
  });

  it('loadMyLeagues no debe llamar facade si ya está cargando', () => {
    facade.leaguesLoading.set(true);
    component.loadMyLeagues();

    expect(facade.loadMyLeagues).not.toHaveBeenCalled();
  });

  it('selectLeague debe setear league en el contexto y navegar usando LeaguePublicID', () => {
    const league = { LeagueID: 1, LeaguePublicID: 999, LeagueName: 'Liga X' };

    component.selectLeague(league);

    expect(ctx.setLeague).toHaveBeenCalledWith(1);
    expect(router.navigate).toHaveBeenCalledWith(['/league', 999, 'actions']);
  });

  it('selectLeague debe mostrar error si LeaguePublicID falta', () => {
    const league: any = { LeagueID: 1, LeagueName: 'Liga X' };

    component.selectLeague(league);

    expect(snackBarSpy.open).toHaveBeenCalled();
    const [msg] = snackBarSpy.open.calls.mostRecent().args;
    expect(msg).toContain('LeaguePublicID no disponible');
    expect(ctx.setLeague).not.toHaveBeenCalled();
  });

  it('goLeague debe navegar cuando hay currentLeagueId', () => {
    ctx.currentLeagueId.set(5);

    component.goLeague('summary');
    expect(router.navigate).toHaveBeenCalledWith(['/league', 5, 'summary']);

    component.goLeague('edit');
    expect(router.navigate).toHaveBeenCalledWith(['/league', 5, 'edit']);
  });

  it('goLeague debe mostrar snack si no hay currentLeagueId', () => {
    ctx.currentLeagueId.set(null);

    component.goLeague('summary');

    expect(snackBarSpy.open).toHaveBeenCalled();
    const [msg] = snackBarSpy.open.calls.mostRecent().args;
    expect(msg).toContain('Seleccioná un League ID primero');
  });

  it('openSetTeamIdDialog debe abrir el diálogo', () => {
    component.openSetTeamIdDialog();
    expect(dialogSpy.open).toHaveBeenCalled();
  });

  it('adminCapability debe navegar a la ruta correcta', async () => {
    component.adminCapability('manage-season');
    expect(router.navigate).toHaveBeenCalledWith(['/seasons/admin']);

    (router.navigate as jasmine.Spy).calls.reset();

    component.adminCapability('manage-nfl-players');
    expect(router.navigate).toHaveBeenCalledWith(['/admin/nfl-player-actions']);
  });
});
