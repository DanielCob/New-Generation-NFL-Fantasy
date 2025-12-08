// src/app/core/services/league.service.spec.ts

import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController
} from '@angular/common/http/testing';

import { environment } from '../../../environments/environment';

import {
  CreateLeagueRequest,
  EditLeagueConfigRequest,
  UpdateLeagueStatusRequest,
  CreateLeagueResponse,
  EditLeagueConfigResponse,
  UpdateLeagueStatusResponse,
  LeagueSummaryResponse,
  LeagueDirectoryResponse,
  LeagueMembersResponse,
  LeagueTeamsResponse,
  JoinLeagueRequest,
  JoinLeagueResponse
} from '../models/league-model';
import { LeagueService } from './league-service';

describe('LeagueService', () => {
  let service: LeagueService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiUrl}/League`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [LeagueService]
    });

    service = TestBed.inject(LeagueService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('create() debe hacer POST a /League', () => {
    const payload: CreateLeagueRequest = {
      name: 'Test League'
    } as any;

    const mockResponse: CreateLeagueResponse = {
      leagueId: 1
    } as any;

    let received!: CreateLeagueResponse;

    service.create(payload).subscribe(r => (received = r));

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);

    req.flush(mockResponse);

    expect(received).toEqual(mockResponse);
  });

  it('getSummary() debe hacer GET a /League/{id}/summary', () => {
    const id = 10;
    const mock: LeagueSummaryResponse = { leagueId: id } as any;

    let received!: LeagueSummaryResponse;

    service.getSummary(id).subscribe(r => (received = r));

    const req = httpMock.expectOne(`${baseUrl}/${id}/summary`);
    expect(req.request.method).toBe('GET');

    req.flush(mock);

    expect(received).toEqual(mock);
  });

  it('getMembers() debe hacer GET a /League/{id}/members', () => {
    const id = 7;
    const mock: LeagueMembersResponse = { leagueId: id } as any;

    let received!: LeagueMembersResponse;

    service.getMembers(id).subscribe(r => (received = r));

    const req = httpMock.expectOne(`${baseUrl}/${id}/members`);
    expect(req.request.method).toBe('GET');

    req.flush(mock);

    expect(received).toEqual(mock);
  });

  it('getDirectory() sin filtros debe llamar /League/directory sin query params', () => {
    const mock: LeagueDirectoryResponse = { total: 0 } as any;

    let received!: LeagueDirectoryResponse;

    service.getDirectory().subscribe(r => (received = r));

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/directory`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);

    req.flush(mock);

    expect(received).toEqual(mock);
  });

  it('getDirectory() con filtros debe enviar query params', () => {
    const mock: LeagueDirectoryResponse = { total: 0 } as any;

    let received!: LeagueDirectoryResponse;

    service.getDirectory({ seasonId: 2025, status: 1 }).subscribe(r => (received = r));

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/directory`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('SeasonId')).toBe('2025');
    expect(req.request.params.get('Status')).toBe('1');

    req.flush(mock);

    expect(received).toEqual(mock);
  });

  it('joinLeague() debe hacer POST a /League/join', () => {
    const payload: JoinLeagueRequest = {
      leaguePublicId: 'ABC',
      password: '1234'
    } as any;

    const mock: JoinLeagueResponse = { success: true } as any;

    let received!: JoinLeagueResponse;

    service.joinLeague(payload).subscribe(r => (received = r));

    const req = httpMock.expectOne(`${baseUrl}/join`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);

    req.flush(mock);

    expect(received).toEqual(mock);
  });

  it('searchLeagues() debe construir correctamente los query params', () => {
    const request = {
      SearchTerm: 'test',
      SeasonID: 2025,
      MinSlots: 4,
      MaxSlots: 12,
      PageNumber: 1,
      PageSize: 20
    };

    const mockResponse = { items: [] };

    let received: any;
    service.searchLeagues(request).subscribe(r => (received = r));

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/search`);
    expect(req.request.method).toBe('GET');

    const params = req.request.params;
    expect(params.get('SearchTerm')).toBe('test');
    expect(params.get('SeasonID')).toBe('2025');
    expect(params.get('MinSlots')).toBe('4');
    expect(params.get('MaxSlots')).toBe('12');
    expect(params.get('PageNumber')).toBe('1');
    expect(params.get('PageSize')).toBe('20');

    req.flush(mockResponse);

    expect(received).toEqual(mockResponse);
  });
});
