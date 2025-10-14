import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { redirectStoredTeamsGuard } from './redirect-stored-teams-guard';

describe('redirectStoredTeamsGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => redirectStoredTeamsGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
