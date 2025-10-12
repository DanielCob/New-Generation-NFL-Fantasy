import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DistributionPanel } from './distribution-panel';

describe('DistributionPanel', () => {
  let component: DistributionPanel;
  let fixture: ComponentFixture<DistributionPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DistributionPanel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DistributionPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
