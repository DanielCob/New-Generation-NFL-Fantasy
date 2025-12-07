// src/app/shared/components/select-team-dialog/select-team-dialog.spec.ts
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';

import { MatDialogRef } from '@angular/material/dialog';

import { SelectTeamDialog } from './select-team-dialog';
import { UserService } from '../../../core/services/user-service';
import { UserProfile, UserTeam } from '../../../core/models/user-model';

describe('SelectTeamDialog', () => {
  let component: SelectTeamDialog;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<SelectTeamDialog>>;
  let router: Router;

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['getProfile']);
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef<SelectTeamDialog>', ['close']);

    await TestBed.configureTestingModule({
      imports: [
        SelectTeamDialog,
        RouterTestingModule.withRoutes([]),
      ],
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: MatDialogRef, useValue: dialogRefSpy },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(SelectTeamDialog);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('ngOnInit should load and sort teams', () => {
    const profile: Partial<UserProfile> = {
      Teams: [
        { TeamID: 2, TeamName: 'B Team', LeagueName: 'B League' } as UserTeam,
        { TeamID: 1, TeamName: 'A Team', LeagueName: 'A League' } as UserTeam,
      ],
    };

    userServiceSpy.getProfile.and.returnValue(of(profile as UserProfile));

    component.ngOnInit();

    const result = component.teams();
    expect(result.length).toBe(2);
    // ordenado por LeagueName luego TeamName
    expect(result[0].TeamID).toBe(1);
    expect(result[1].TeamID).toBe(2);

    expect(userServiceSpy.getProfile).toHaveBeenCalled();
  });

  it('select should persist team, close dialog and navigate', async () => {
    const t: UserTeam = {
      TeamID: 10,
      TeamName: 'My Team',
      LeagueName: 'My League',
    } as any;

    const navSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));

    spyOn(localStorage, 'setItem');

    component.select(t);

    expect(localStorage.setItem)
      .toHaveBeenCalledWith('xnf.currentTeamId', '10');
    expect(dialogRefSpy.close).toHaveBeenCalledWith('10');
    expect(navSpy).toHaveBeenCalledWith(['/teams', 10, 'my-team']);
  });

  it('select should do nothing if TeamID is falsy', () => {
    const t: UserTeam = { TeamID: 0 } as any;

    const navSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    spyOn(localStorage, 'setItem');

    component.select(t);

    expect(localStorage.setItem).not.toHaveBeenCalled();
    expect(dialogRefSpy.close).not.toHaveBeenCalled();
    expect(navSpy).not.toHaveBeenCalled();
  });

  it('close should close dialog', () => {
    component.close();
    expect(dialogRefSpy.close).toHaveBeenCalled();
  });
});
