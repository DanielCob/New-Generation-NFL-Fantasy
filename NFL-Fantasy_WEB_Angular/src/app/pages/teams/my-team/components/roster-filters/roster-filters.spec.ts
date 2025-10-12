import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RosterFilters } from './roster-filters';

describe('RosterFilters', () => {
  let component: RosterFilters;
  let fixture: ComponentFixture<RosterFilters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RosterFilters]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RosterFilters);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
