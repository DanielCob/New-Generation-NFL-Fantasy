import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageRoaster } from './manage-roaster';

describe('ManageRoaster', () => {
  let component: ManageRoaster;
  let fixture: ComponentFixture<ManageRoaster>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageRoaster]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManageRoaster);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
