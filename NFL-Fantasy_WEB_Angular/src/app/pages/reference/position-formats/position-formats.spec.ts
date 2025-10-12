import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PositionFormats } from './position-formats';

describe('PositionFormats', () => {
  let component: PositionFormats;
  let fixture: ComponentFixture<PositionFormats>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PositionFormats]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PositionFormats);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
