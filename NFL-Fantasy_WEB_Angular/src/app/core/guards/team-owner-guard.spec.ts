import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { teamOwnerGuard } from './team-owner-guard';

describe('teamOwnerGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => teamOwnerGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
