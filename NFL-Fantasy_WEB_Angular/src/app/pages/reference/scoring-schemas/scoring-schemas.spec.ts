import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScoringSchemas } from './scoring-schemas';

describe('ScoringSchemas', () => {
  let component: ScoringSchemas;
  let fixture: ComponentFixture<ScoringSchemas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScoringSchemas]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ScoringSchemas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
