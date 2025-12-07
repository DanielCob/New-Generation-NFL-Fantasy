// src/app/pages/league/create/create.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { Create } from './create';
import { LeagueService } from '../../../core/services/league-service';
import { MatSnackBar } from '@angular/material/snack-bar';

// ---- Mocks ---- //

class MockLeagueService {
  create = jasmine.createSpy('create').and.returnValue(
    of({
      Data: { LeagueID: 1, Name: 'Test League' }
    })
  );
}

const snackBarSpy = {
  open: jasmine.createSpy('open')
};

describe('Create League Component', () => {
  let component: Create;
  let fixture: ComponentFixture<Create>;
  let leagueService: MockLeagueService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Create], // standalone component
      providers: [
        { provide: LeagueService, useClass: MockLeagueService },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Create);
    component = fixture.componentInstance;
    leagueService = TestBed.inject(LeagueService) as unknown as MockLeagueService;
    snackBarSpy.open.calls.reset();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('debe inicializar availablePlayoffTeams según TeamSlots por defecto (8 -> solo 4 equipos)', () => {
    const values = component.availablePlayoffTeams().map(o => o.value);
    expect(values).toEqual([4]);
  });

  it('al cambiar TeamSlots a 10 debe permitir 4 y 6 equipos de playoff', () => {
    component.form.controls.TeamSlots.setValue(10);
    const values = component.availablePlayoffTeams().map(o => o.value);
    expect(values).toEqual([4, 6]);
  });

  it('submit con formulario inválido debe marcar touched y mostrar snack', () => {
    // Dejamos el form vacío -> inválido
    component.form.reset({
      Name: '',
      Description: '',
      InitialTeamName: '',
      LeaguePassword: ''
    });

    component.submit();

    expect(snackBarSpy.open).toHaveBeenCalled();
    const [msg] = snackBarSpy.open.calls.mostRecent().args;
    expect(msg).toContain('Please check required fields');
    // create NO debe haberse llamado
    expect(leagueService.create).not.toHaveBeenCalled();
  });

  it('submit con formulario válido debe llamar LeagueService.create y resetear el formulario', () => {
    component.form.setValue({
      Name: 'My League',
      Description: 'Desc',
      TeamSlots: 10,
      PlayoffTeams: 4,
      AllowDecimals: true,
      PositionFormatID: 1,
      ScoringSchemaID: 1,
      InitialTeamName: 'My Team',
      LeaguePassword: 'Abcdef12'
    });

    component.submit();

    expect(leagueService.create).toHaveBeenCalled();
    expect(snackBarSpy.open).toHaveBeenCalled();

    // Después del éxito el form se resetea con defaults
    const v = component.form.getRawValue();
    expect(v.TeamSlots).toBe(8);
    expect(v.PlayoffTeams).toBe(4);
    expect(v.Name).toBe('');
    expect(v.InitialTeamName).toBe('');
    expect(v.LeaguePassword).toBe('');
  });

  it('submit debe manejar error del backend mostrando mensaje', () => {
    leagueService.create.and.returnValue(
      throwError(() => ({
        error: { message: 'Backend error' }
      }))
    );

    component.form.setValue({
      Name: 'My League',
      Description: 'Desc',
      TeamSlots: 8,
      PlayoffTeams: 4,
      AllowDecimals: true,
      PositionFormatID: 1,
      ScoringSchemaID: 1,
      InitialTeamName: 'My Team',
      LeaguePassword: 'Abcdef12'
    });

    component.submit();

    expect(snackBarSpy.open).toHaveBeenCalled();
    const [msg] = snackBarSpy.open.calls.mostRecent().args;
    expect(msg).toContain('Backend error');
  });
});
