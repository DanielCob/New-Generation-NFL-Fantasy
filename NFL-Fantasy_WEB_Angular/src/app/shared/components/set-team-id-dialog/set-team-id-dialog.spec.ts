import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SetTeamIdDialog } from './set-team-id-dialog';

describe('SetTeamIdDialog', () => {
  let component: SetTeamIdDialog;
  let fixture: ComponentFixture<SetTeamIdDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SetTeamIdDialog]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SetTeamIdDialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
