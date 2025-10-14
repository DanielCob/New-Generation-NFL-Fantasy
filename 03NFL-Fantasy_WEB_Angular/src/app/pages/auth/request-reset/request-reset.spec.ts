import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequestReset } from './request-reset';

describe('RequestReset', () => {
  let component: RequestReset;
  let fixture: ComponentFixture<RequestReset>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestReset]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RequestReset);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
