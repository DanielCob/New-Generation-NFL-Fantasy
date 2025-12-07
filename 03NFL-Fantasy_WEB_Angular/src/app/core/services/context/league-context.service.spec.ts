// src/app/core/context/league-context.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { LeagueContextService } from './league-context.service';

describe('LeagueContextService', () => {

  beforeEach(() => {
    // Limpiar storage antes de cada prueba
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [LeagueContextService],
    });
  });

  it('should be created', () => {
    const service = TestBed.inject(LeagueContextService);
    expect(service).toBeTruthy();
  });

  it('debe inicializar las signals desde localStorage cuando hay valores válidos', () => {
    // Arrange: dejamos valores válidos en localStorage ANTES de crear el servicio
    localStorage.setItem('xnf.currentLeagueId', '10');
    localStorage.setItem('xnf.currentTeamId', '5');

    const service = TestBed.inject(LeagueContextService);

    // Act + Assert
    expect(service.currentLeagueId()).toBe(10);
    expect(service.currentTeamId()).toBe(5);
  });

  it('debe inicializar en null cuando localStorage tiene valores inválidos o no existen', () => {
    // Valores inválidos
    localStorage.setItem('xnf.currentLeagueId', '-1');
    localStorage.setItem('xnf.currentTeamId', 'abc');

    const service = TestBed.inject(LeagueContextService);

    expect(service.currentLeagueId()).toBeNull();
    expect(service.currentTeamId()).toBeNull();
  });

  it('setLeague() debe actualizar signal y escribir en localStorage', () => {
    const service = TestBed.inject(LeagueContextService);

    service.setLeague(42);

    expect(service.currentLeagueId()).toBe(42);
    expect(localStorage.getItem('xnf.currentLeagueId')).toBe('42');
  });

  it('setTeam() debe actualizar signal y escribir en localStorage', () => {
    const service = TestBed.inject(LeagueContextService);

    service.setTeam(99);

    expect(service.currentTeamId()).toBe(99);
    expect(localStorage.getItem('xnf.currentTeamId')).toBe('99');
  });
});
