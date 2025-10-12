import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FullProfile } from './full-profile';

describe('FullProfile', () => {
  let component: FullProfile;
  let fixture: ComponentFixture<FullProfile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FullProfile]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FullProfile);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
