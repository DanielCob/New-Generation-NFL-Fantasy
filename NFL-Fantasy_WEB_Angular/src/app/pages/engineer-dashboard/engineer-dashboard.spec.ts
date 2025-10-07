import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EngineerDashboard } from './engineer-dashboard';

describe('EngineerDashboard', () => {
  let component: EngineerDashboard;
  let fixture: ComponentFixture<EngineerDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EngineerDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EngineerDashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
