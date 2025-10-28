import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { SeasonService } from './season-service';
import { environment } from '../../../environments/environment';

describe('SeasonService', () => {
  let service: SeasonService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(SeasonService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getCurrent should map Season from response (camelCase)', () => {
    const mock = { success: true, message: 'ok', data: { SeasonID: 2, Label: 'NFL 2025', Year: 2025, StartDate: '2025-09-01T00:00:00', EndDate: '2026-02-28T00:00:00', IsCurrent: true, CreatedAt: '2025-10-27T20:51:40' } };
    let label: string | undefined;

    service.getCurrent().subscribe(season => (label = season.Label));

    const req = httpMock.expectOne(`${environment.apiUrl}/Seasons/current`);
    expect(req.request.method).toBe('GET');
    req.flush(mock);

    expect(label).toBe('NFL 2025');
  });

  it('getCurrent should map Season from response (PascalCase)', () => {
    const mock = { Success: true, Message: 'ok', Data: { SeasonID: 2, Label: 'NFL 2025', Year: 2025, StartDate: '2025-09-01T00:00:00', EndDate: '2026-02-28T00:00:00', IsCurrent: true, CreatedAt: '2025-10-27T20:51:40' } } as any;
    let year: number | undefined;

    service.getCurrent().subscribe(season => (year = season.Year));

    const req = httpMock.expectOne(`${environment.apiUrl}/Seasons/current`);
    expect(req.request.method).toBe('GET');
    req.flush(mock);

    expect(year).toBe(2025);
  });
});
