import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeagueSummaryAdmin } from './league-summary-admin';

describe('LeagueSummaryAdmin', () => {
  let component: LeagueSummaryAdmin;
  let fixture: ComponentFixture<LeagueSummaryAdmin>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeagueSummaryAdmin]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LeagueSummaryAdmin);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
