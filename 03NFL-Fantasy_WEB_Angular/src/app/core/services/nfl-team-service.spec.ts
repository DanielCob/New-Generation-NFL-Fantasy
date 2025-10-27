import { TestBed } from '@angular/core/testing';

import { NFLTeamService } from './nfl-team-service';

describe('NFLTeamService', () => {
  let service: NFLTeamService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NFLTeamService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
