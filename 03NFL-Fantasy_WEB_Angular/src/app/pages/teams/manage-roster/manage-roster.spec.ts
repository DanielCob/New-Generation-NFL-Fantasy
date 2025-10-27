import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageRoster } from './manage-roster';

describe('ManageRoster', () => {
  let component: ManageRoster;
  let fixture: ComponentFixture<ManageRoster>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageRoster]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManageRoster);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
