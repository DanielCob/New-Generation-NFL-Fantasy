import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditBranding } from './edit-branding';

describe('EditBranding', () => {
  let component: EditBranding;
  let fixture: ComponentFixture<EditBranding>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditBranding]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditBranding);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
