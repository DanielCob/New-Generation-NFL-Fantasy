import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RosterDistributionChart } from './roster-distribution-chart';

describe('RosterDistributionChart', () => {
  let component: RosterDistributionChart;
  let fixture: ComponentFixture<RosterDistributionChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RosterDistributionChart]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RosterDistributionChart);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
