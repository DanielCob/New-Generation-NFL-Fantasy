import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError, Subject } from 'rxjs';

// Material Modules
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

import { DeletePlayerNewsDialog } from './delete-player-news-dialog';
import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import { NFLPlayerListItem, PlayerNewsItem } from '../../../../core/models/nfl-player-model';

/**
 * Tests unitarios para DeletePlayerNewsDialog
 *
 * COBERTURA:
 * - Creación del componente
 * - Carga de noticias del jugador
 * - Selección de noticia a eliminar
 * - Confirmación y eliminación
 * - Manejo de errores
 * - Estado de loading
 */
describe('DeletePlayerNewsDialog', () => {
  let component: DeletePlayerNewsDialog;
  let fixture: ComponentFixture<DeletePlayerNewsDialog>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<DeletePlayerNewsDialog>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockService: jasmine.SpyObj<NFLPlayerService>;

  const mockPlayer: NFLPlayerListItem = {
    NFLPlayerID: 100,
    FirstName: 'Patrick',
    LastName: 'Mahomes',
    FullName: 'Patrick Mahomes',
    Position: 'QB',
    NFLTeamID: 1,
    InjuryStatus: 'Healthy',
    IsActive: true,
    PhotoUrl: '',
    ThumbnailUrl: '',
    CreatedAt: '2024-01-01',
    UpdatedAt: '2024-01-01'
  };

  const mockNewsList: PlayerNewsItem[] = [
    {
      NewsID: 1,
      NFLPlayerID: 100,
      NewsText: 'Mahomes tuvo una gran práctica hoy.',
      IsInjury: false,
      InjurySummary: undefined,
      Designation: undefined,
      CreatedByUserID: 1,
      CreatedByName: 'Admin',
      CreatedAt: '2024-12-06T10:00:00Z'
    },
    {
      NewsID: 2,
      NFLPlayerID: 100,
      NewsText: 'Mahomes se lesionó el tobillo.',
      IsInjury: true,
      InjurySummary: 'Tobillo derecho',
      Designation: 'Q',
      CreatedByUserID: 1,
      CreatedByName: 'Admin',
      CreatedAt: '2024-12-05T10:00:00Z'
    }
  ];

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockService = jasmine.createSpyObj('NFLPlayerService', [
      'getPlayerNews',
      'deletePlayerNews'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        DeletePlayerNewsDialog,
        ReactiveFormsModule,
        BrowserAnimationsModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatSelectModule,
        MatProgressSpinnerModule,
        MatTableModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { player: mockPlayer } },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: NFLPlayerService, useValue: mockService }
      ]
    }).compileComponents();

    // Setup default mock response (ApiResponse camelCase)
    mockService.getPlayerNews.and.returnValue(
      of({
        success: true,
        message: 'OK',
        data: {
          News: mockNewsList,
          TotalRecords: 2,
          CurrentPage: 1,
          PageSize: 20,
          TotalPages: 1
        }
      })
    );

    fixture = TestBed.createComponent(DeletePlayerNewsDialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ==========================================
  // COMPONENT CREATION
  // ==========================================

  describe('Component Creation', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize with player data', () => {
      expect(component.player).toEqual(mockPlayer);
      expect(component.player.FullName).toBe('Patrick Mahomes');
    });

    it('should load news on init', () => {
      expect(mockService.getPlayerNews).toHaveBeenCalledWith(100, {
        PageNumber: 1,
        PageSize: 20
      });
    });

    it('should initialize loading signal as false after load', done => {
      setTimeout(() => {
        expect(component.loadingNews()).toBe(false);
        done();
      }, 100);
    });

    it('should populate news list after load', done => {
      setTimeout(() => {
        expect(component.news().length).toBe(2);
        expect(component.news()[0].NewsID).toBe(1);
        done();
      }, 100);
    });
  });

  // ==========================================
  // LOAD NEWS
  // ==========================================

  describe('Load News', () => {
    it('should set loading to true while loading', () => {
      const pending$ = new Subject<any>();
      mockService.getPlayerNews.and.returnValue(pending$);

      component.loadNews();

      expect(component.loadingNews()).toBeTrue();
    });

    it('should load news successfully', done => {
      component.loadNews();

      setTimeout(() => {
        expect(component.news().length).toBe(2);
        expect(component.loadingNews()).toBe(false);
        done();
      }, 100);
    });

    it('should handle empty news list', done => {
      mockService.getPlayerNews.and.returnValue(
        of({
          success: true,
          message: 'No hay noticias',
          data: {
            News: [],
            TotalRecords: 0,
            CurrentPage: 1,
            PageSize: 20,
            TotalPages: 0
          }
        })
      );

      component.loadNews();

      setTimeout(() => {
        expect(component.news().length).toBe(0);
        expect(component.loadingNews()).toBe(false);
        done();
      }, 100);
    });

    it('should handle load error', done => {
      mockService.getPlayerNews.and.returnValue(
        throwError(() => new Error('Load error'))
      );

      component.loadNews();

      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          jasmine.stringContaining('Error'),
          'OK',
          { duration: 3000 }
        );
        expect(component.loadingNews()).toBe(false);
        done();
      }, 100);
    });

    it('should request first 20 news items', () => {
      component.loadNews();

      expect(mockService.getPlayerNews).toHaveBeenCalledWith(
        100,
        jasmine.objectContaining({
          PageNumber: 1,
          PageSize: 20
        })
      );
    });
  });

  // ==========================================
  // SELECT NEWS
  // ==========================================

  describe('Select News', () => {
    it('should select news when selectedNewsId changes', () => {
      component.form.patchValue({ selectedNewsId: 1 });
      fixture.detectChanges();

      expect(component.selectedNewsId).toBe(1);
    });

    it('should find selected news from list', () => {
      component.form.patchValue({ selectedNewsId: 2 });

      const selectedNews = component.news().find(n => n.NewsID === 2);
      expect(selectedNews).toBeDefined();
      expect(selectedNews?.Designation).toBe('Q');
    });

    it('should allow deselection', () => {
      component.form.patchValue({ selectedNewsId: 1 });
      component.form.patchValue({ selectedNewsId: null });

      expect(component.selectedNewsId).toBeNull();
    });
  });

  // ==========================================
  // DELETE NEWS - SUCCESS
  // ==========================================

  describe('Delete News - Success', () => {
    beforeEach(() => {
      component.form.patchValue({ selectedNewsId: 1 });
    });

    it('should delete selected news successfully', done => {
      mockService.deletePlayerNews.and.returnValue(
        of({
          Success: true,
          Message: 'Noticia eliminada exitosamente.',
          Data: {
            Message: 'Noticia eliminada exitosamente.',
            RevertedDesignation: null
          }
        })
      );

      component.deleteNews();

      setTimeout(() => {
        expect(mockService.deletePlayerNews).toHaveBeenCalledWith(1);
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Noticia eliminada exitosamente.',
          'OK',
          { duration: 3000 }
        );
        expect(mockDialogRef.close).toHaveBeenCalledWith({ deleted: true, newsId: 1 });
        done();
      }, 100);
    });

    it('should delete injury news with reverted designation', done => {
      component.form.patchValue({ selectedNewsId: 2 });

      mockService.deletePlayerNews.and.returnValue(
        of({
          Success: true,
          Message: 'OK',
          Data: {
            Message: 'OK',
            RevertedDesignation: 'Q'
          }
        })
      );

      component.deleteNews();

      setTimeout(() => {
        expect(mockService.deletePlayerNews).toHaveBeenCalledWith(2);
        expect(mockDialogRef.close).toHaveBeenCalledWith({ deleted: true, newsId: 2 });
        done();
      }, 100);
    });

    it('should set deleting to true during delete', () => {
      mockService.deletePlayerNews.and.returnValue(
        of({
          Success: true,
          Message: 'OK',
          Data: { Message: 'OK' }
        })
      );

      component.deleteNews();
      expect(component.deleting()).toBe(true);
    });
  });

  // ==========================================
  // DELETE NEWS - VALIDATION
  // ==========================================

  describe('Delete News - Validation', () => {
    it('should not delete if no news is selected', () => {
      component.form.patchValue({ selectedNewsId: null });

      component.deleteNews();

      expect(mockService.deletePlayerNews).not.toHaveBeenCalled();
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Por favor selecciona una noticia para eliminar',
        'OK',
        { duration: 3000 }
      );
    });

    it('should not delete while already deleting', () => {
      component.form.patchValue({ selectedNewsId: 1 });
      component.deleting.set(true);

      component.deleteNews();

      expect(mockService.deletePlayerNews).not.toHaveBeenCalled();
    });
  });

  // ==========================================
  // DELETE NEWS - ERROR HANDLING
  // ==========================================

  describe('Delete News - Error Handling', () => {
    beforeEach(() => {
      component.form.patchValue({ selectedNewsId: 1 });
    });

    it('should handle delete error', done => {
      const errorResponse = {
        error: {
          message: 'Noticia no encontrada'
        }
      };

      mockService.deletePlayerNews.and.returnValue(
        throwError(() => errorResponse)
      );

      component.deleteNews();

      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Noticia no encontrada',
          'OK',
          { duration: 4000 }
        );
        expect(component.deleting()).toBe(false);
        done();
      }, 100);
    });

    it('should handle generic error', done => {
      mockService.deletePlayerNews.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      component.deleteNews();

      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          jasmine.stringContaining('Error'),
          'OK',
          { duration: 4000 }
        );
        done();
      }, 100);
    });

    it('should set deleting to false after error', done => {
      mockService.deletePlayerNews.and.returnValue(
        throwError(() => new Error('Test error'))
      );

      component.deleteNews();

      setTimeout(() => {
        expect(component.deleting()).toBe(false);
        done();
      }, 100);
    });
  });

  // ==========================================
  // CLOSE DIALOG
  // ==========================================

  describe('Close Dialog', () => {
    it('should close dialog without data', () => {
      component.close();
      expect(mockDialogRef.close).toHaveBeenCalledWith();
    });

    it('should not change deleting flag on close', () => {
      component.deleting.set(true);
      component.close();

      expect(component.deleting()).toBe(true);
    });
  });

  // ==========================================
  // DISPLAY HELPERS
  // ==========================================

  describe('Display Helpers', () => {
    it('should format date correctly', () => {
      const dateStr = '2024-12-06T10:30:00Z';
      const formatted = component.formatDate(dateStr);

      expect(formatted).toContain('2024');
      expect(formatted).toContain('12');
      expect(formatted).toContain('06');
    });

    it('should show injury badge for injury news', () => {
      const injuryNews = component.news().find(n => n.IsInjury);
      expect(injuryNews).toBeDefined();
      expect(injuryNews?.Designation).toBe('Q');
    });

    it('should not show injury info for regular news', () => {
      const regularNews = component.news().find(n => !n.IsInjury);
      expect(regularNews).toBeDefined();
      expect(regularNews?.Designation).toBeUndefined();
    });
  });

  // ==========================================
  // INTEGRATION TESTS
  // ==========================================

  describe('Integration Tests', () => {
    it('should handle complete flow: load, select, delete', done => {
      mockService.deletePlayerNews.and.returnValue(
        of({
          Success: true,
          Message: 'Noticia eliminada exitosamente.',
          Data: { Message: 'OK' }
        })
      );

      setTimeout(() => {
        expect(component.news().length).toBe(2);

        component.form.patchValue({ selectedNewsId: 1 });
        expect(component.selectedNewsId).toBe(1);

        component.deleteNews();

        setTimeout(() => {
          expect(mockService.deletePlayerNews).toHaveBeenCalledWith(1);
          expect(mockDialogRef.close).toHaveBeenCalledWith({ deleted: true, newsId: 1 });
          done();
        }, 100);
      }, 100);
    });

    it('should keep news loaded after failed delete', done => {
      component.form.patchValue({ selectedNewsId: 1 });

      mockService.deletePlayerNews.and.returnValue(
        throwError(() => new Error('Delete failed'))
      );

      component.deleteNews();

      setTimeout(() => {
        expect(component.deleting()).toBe(false);
        expect(component.news().length).toBe(2);
        done();
      }, 100);
    });
  });
});
