import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AvailablePlayers } from './available-players';

describe('AvailablePlayers', () => {
  let component: AvailablePlayers;
  let fixture: ComponentFixture<AvailablePlayers>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AvailablePlayers]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AvailablePlayers);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
