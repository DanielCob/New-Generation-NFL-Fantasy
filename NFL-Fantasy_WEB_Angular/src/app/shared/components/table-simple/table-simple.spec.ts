import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TableSimple } from './table-simple';

describe('TableSimple', () => {
  let component: TableSimple;
  let fixture: ComponentFixture<TableSimple>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableSimple]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TableSimple);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
