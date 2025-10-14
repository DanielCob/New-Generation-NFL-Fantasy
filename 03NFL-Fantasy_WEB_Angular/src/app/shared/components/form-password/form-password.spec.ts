import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormPassword } from './form-password';

describe('FormPassword', () => {
  let component: FormPassword;
  let fixture: ComponentFixture<FormPassword>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormPassword]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
