import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RosterList } from './roster-list';

describe('RosterList', () => {
  let component: RosterList;
  let fixture: ComponentFixture<RosterList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RosterList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RosterList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
