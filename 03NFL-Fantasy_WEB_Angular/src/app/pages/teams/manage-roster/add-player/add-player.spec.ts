import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddPlayer } from './add-player';

describe('AddPlayer', () => {
  let component: AddPlayer;
  let fixture: ComponentFixture<AddPlayer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddPlayer]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddPlayer);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
