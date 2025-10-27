import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectTeamDialog } from './select-team-dialog';

describe('SelectTeamDialog', () => {
  let component: SelectTeamDialog;
  let fixture: ComponentFixture<SelectTeamDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SelectTeamDialog]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SelectTeamDialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
