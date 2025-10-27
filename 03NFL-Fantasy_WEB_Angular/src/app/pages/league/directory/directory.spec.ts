import { ComponentFixture, TestBed } from '@angular/core/testing';

import {LeagueDirectoryComponent } from './directory';

describe('Directory', () => {
  let component: LeagueDirectoryComponent;
  let fixture: ComponentFixture<LeagueDirectoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeagueDirectoryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LeagueDirectoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
