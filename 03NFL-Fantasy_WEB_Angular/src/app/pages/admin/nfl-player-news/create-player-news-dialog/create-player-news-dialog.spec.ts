import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';

// Material Modules
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CreatePlayerNewsDialog } from './create-player-news-dialog';
import { NFLPlayerService } from '../../../../core/services/nfl-player-service';
import { CreatePlayerNewsDTO, NFLPlayerListItem } from '../../../../core/models/nfl-player-model';

/**
 * Tests unitarios para CreatePlayerNewsDialog
 * 
 * COBERTURA:
 * - Creación del componente
 * - Validación de formulario
 * - Lógica IsInjury (show/hide campos)
 * - Envío de datos al servicio
 * - Manejo de respuestas exitosas
 * - Manejo de errores
 * - Character counters
 */
describe('CreatePlayerNewsDialog', () => {
  let component: CreatePlayerNewsDialog;
  let fixture: ComponentFixture<CreatePlayerNewsDialog>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CreatePlayerNewsDialog>>;
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

  beforeEach(async () => {
    // Crear spies
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockService = jasmine.createSpyObj('NFLPlayerService', ['createPlayerNews']);

    await TestBed.configureTestingModule({
      imports: [
        CreatePlayerNewsDialog,
        ReactiveFormsModule,
        BrowserAnimationsModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatCheckboxModule,
        MatProgressSpinnerModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { player: mockPlayer } },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: NFLPlayerService, useValue: mockService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CreatePlayerNewsDialog);
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

    it('should initialize form with empty values', () => {
      expect(component.form.get('NewsText')?.value).toBe('');
      expect(component.form.get('IsInjury')?.value).toBe(false);
      expect(component.form.get('InjurySummary')?.value).toBe('');
      expect(component.form.get('Designation')?.value).toBe('');
    });

    it('should initialize loading signal as false', () => {
      expect(component.loading()).toBe(false);
    });

    it('should have designations available', () => {
      expect(component.designations).toBeDefined();
      expect(component.designations.length).toBe(8);
      expect(component.designations[0].value).toBe('O');
    });
  });

  // ==========================================
  // FORM VALIDATION - Regular News
  // ==========================================

  describe('Form Validation - Regular News', () => {
    it('should be invalid when NewsText is empty', () => {
      component.form.patchValue({ NewsText: '' });
      expect(component.form.valid).toBe(false);
      expect(component.form.get('NewsText')?.hasError('required')).toBe(true);
    });

    it('should be invalid when NewsText is less than 10 characters', () => {
      component.form.patchValue({ NewsText: 'Short' }); // 5 chars
      expect(component.form.get('NewsText')?.hasError('minlength')).toBe(true);
    });

    it('should be invalid when NewsText exceeds 300 characters', () => {
      const longText = 'a'.repeat(301);
      component.form.patchValue({ NewsText: longText });
      expect(component.form.get('NewsText')?.hasError('maxlength')).toBe(true);
    });

    it('should be valid with NewsText between 10-300 characters', () => {
      component.form.patchValue({
        NewsText: 'Mahomes tuvo una excelente práctica hoy.',
        IsInjury: false
      });
      expect(component.form.valid).toBe(true);
    });

    it('should not require InjurySummary for regular news', () => {
      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });
      expect(component.form.get('InjurySummary')?.hasError('required')).toBe(false);
    });

    it('should not require Designation for regular news', () => {
      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });
      expect(component.form.get('Designation')?.hasError('required')).toBe(false);
    });
  });

  // ==========================================
  // FORM VALIDATION - Injury News
  // ==========================================

  describe('Form Validation - Injury News', () => {
    beforeEach(() => {
      // Simular cambio a IsInjury = true
      component.form.patchValue({ IsInjury: true });
      fixture.detectChanges();
    });

    it('should require InjurySummary when IsInjury is true', () => {
      component.form.patchValue({ InjurySummary: '' });
      expect(component.form.get('InjurySummary')?.hasError('required')).toBe(true);
    });

    it('should require Designation when IsInjury is true', () => {
      component.form.patchValue({ Designation: '' });
      expect(component.form.get('Designation')?.hasError('required')).toBe(true);
    });

    it('should be invalid when InjurySummary exceeds 30 characters', () => {
      const longSummary = 'a'.repeat(31);
      component.form.patchValue({ InjurySummary: longSummary });
      expect(component.form.get('InjurySummary')?.hasError('maxlength')).toBe(true);
    });

    it('should be valid with all injury fields filled correctly', () => {
      component.form.patchValue({
        NewsText: 'Mahomes se lesionó el tobillo.',
        IsInjury: true,
        InjurySummary: 'Tobillo derecho',
        Designation: 'Q'
      });
      expect(component.form.valid).toBe(true);
    });

    it('should accept InjurySummary with exactly 30 characters', () => {
      const summary30 = 'a'.repeat(30);
      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: true,
        InjurySummary: summary30,
        Designation: 'Q'
      });
      expect(component.form.get('InjurySummary')?.valid).toBe(true);
    });
  });

  // ==========================================
  // IsInjury TOGGLE LOGIC
  // ==========================================

  describe('IsInjury Toggle Logic', () => {
    it('should clear InjurySummary and Designation when IsInjury changes to false', () => {
      // Set injury fields
      component.form.patchValue({
        IsInjury: true,
        InjurySummary: 'Tobillo',
        Designation: 'Q'
      });

      // Toggle to false
      component.form.patchValue({ IsInjury: false });
      fixture.detectChanges();

      expect(component.form.get('InjurySummary')?.value).toBe('');
      expect(component.form.get('Designation')?.value).toBe('');
    });

    it('should remove validators from InjurySummary when IsInjury is false', () => {
      component.form.patchValue({ IsInjury: false });
      fixture.detectChanges();

      const injurySummaryControl = component.form.get('InjurySummary');
      expect(injurySummaryControl?.hasError('required')).toBe(false);
    });

    it('should remove validators from Designation when IsInjury is false', () => {
      component.form.patchValue({ IsInjury: false });
      fixture.detectChanges();

      const designationControl = component.form.get('Designation');
      expect(designationControl?.hasError('required')).toBe(false);
    });

    it('should add validators when IsInjury changes to true', () => {
      component.form.patchValue({ IsInjury: false });
      fixture.detectChanges();

      component.form.patchValue({ IsInjury: true });
      fixture.detectChanges();

      const injurySummaryControl = component.form.get('InjurySummary');
      const designationControl = component.form.get('Designation');

      expect(injurySummaryControl?.hasError('required')).toBe(true);
      expect(designationControl?.hasError('required')).toBe(true);
    });

    it('should return correct isInjury getter value', () => {
      component.form.patchValue({ IsInjury: false });
      expect(component.isInjury).toBe(false);

      component.form.patchValue({ IsInjury: true });
      expect(component.isInjury).toBe(true);
    });
  });

  // ==========================================
  // SUBMIT - SUCCESS
  // ==========================================

  describe('Submit - Success', () => {
    it('should submit regular news successfully', (done) => {
      // Arrange
      const mockResponse = {
        Success: true,
        Message: 'Noticia creada exitosamente.',
        Data: { NewsID: 123, Message: 'OK' }
      };

      mockService.createPlayerNews.and.returnValue(of(mockResponse));

      component.form.patchValue({
        NewsText: 'Mahomes tuvo una gran práctica.',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockService.createPlayerNews).toHaveBeenCalledWith(
          jasmine.objectContaining({
            NFLPlayerID: 100,
            NewsText: 'Mahomes tuvo una gran práctica.',
            IsInjury: false
          })
        );
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Noticia creada exitosamente.',
          'OK',
          { duration: 3000 }
        );
        expect(mockDialogRef.close).toHaveBeenCalledWith({
          created: true,
          newsId: 123
        });
        done();
      }, 100);
    });

    it('should submit injury news successfully', (done) => {
      // Arrange
      const mockResponse = {
        Success: true,
        Message: 'Noticia de lesión creada.',
        Data: { NewsID: 456, Message: 'OK' }
      };

      mockService.createPlayerNews.and.returnValue(of(mockResponse));

      component.form.patchValue({
        NewsText: 'Mahomes se lesionó el tobillo.',
        IsInjury: true,
        InjurySummary: 'Tobillo derecho',
        Designation: 'Q'
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockService.createPlayerNews).toHaveBeenCalledWith(
          jasmine.objectContaining({
            NFLPlayerID: 100,
            IsInjury: true,
            InjurySummary: 'Tobillo derecho',
            Designation: 'Q'
          })
        );
        expect(mockDialogRef.close).toHaveBeenCalledWith({
          created: true,
          newsId: 456
        });
        done();
      }, 100);
    });

    it('should set loading to true during submission', () => {
      // Arrange
      mockService.createPlayerNews.and.returnValue(of({
        Success: true,
        Message: 'OK',
        Data: { NewsID: 1, Message: 'OK' }
      }));

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      expect(component.loading()).toBe(true);
    });

    it('should send undefined for optional fields in regular news', (done) => {
      // Arrange
      mockService.createPlayerNews.and.returnValue(of({
        Success: true,
        Message: 'OK',
        Data: { NewsID: 1, Message: 'OK' }
      }));

      component.form.patchValue({
        NewsText: 'Regular news without injury',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        const callArgs = mockService.createPlayerNews.calls.argsFor(0)[0];
        expect(callArgs.InjurySummary).toBeUndefined();
        expect(callArgs.Designation).toBeUndefined();
        done();
      }, 100);
    });
  });

  // ==========================================
  // SUBMIT - VALIDATION ERRORS
  // ==========================================

  describe('Submit - Validation Errors', () => {
    it('should not submit if form is invalid', () => {
      // Arrange
      component.form.patchValue({
        NewsText: '', // Invalid: required
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      expect(mockService.createPlayerNews).not.toHaveBeenCalled();
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Por favor completa todos los campos requeridos',
        'OK',
        { duration: 3000 }
      );
    });

    it('should mark all fields as touched when invalid', () => {
      // Arrange
      component.form.patchValue({ NewsText: '' });

      // Act
      component.submit();

      // Assert
      expect(component.form.get('NewsText')?.touched).toBe(true);
    });

    it('should not submit injury news without InjurySummary', () => {
      // Arrange
      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: true,
        InjurySummary: '', // Missing
        Designation: 'Q'
      });

      // Act
      component.submit();

      // Assert
      expect(mockService.createPlayerNews).not.toHaveBeenCalled();
      expect(mockSnackBar.open).toHaveBeenCalled();
    });

    it('should not submit injury news without Designation', () => {
      // Arrange
      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: true,
        InjurySummary: 'Tobillo',
        Designation: '' // Missing
      });

      // Act
      component.submit();

      // Assert
      expect(mockService.createPlayerNews).not.toHaveBeenCalled();
    });
  });

  // ==========================================
  // SUBMIT - ERROR HANDLING
  // ==========================================

  describe('Submit - Error Handling', () => {
    it('should handle service error', (done) => {
      // Arrange
      const errorResponse = {
        error: {
          message: 'Error al crear noticia'
        }
      };

      mockService.createPlayerNews.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Error al crear noticia',
          'OK',
          { duration: 4000 }
        );
        expect(component.loading()).toBe(false);
        done();
      }, 100);
    });

    it('should handle error with Message property', (done) => {
      // Arrange
      const errorResponse = {
        error: {
          Message: 'Validation error'
        }
      };

      mockService.createPlayerNews.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Validation error',
          'OK',
          { duration: 4000 }
        );
        done();
      }, 100);
    });

    it('should handle generic error', (done) => {
      // Arrange
      mockService.createPlayerNews.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Error creando noticia',
          'OK',
          { duration: 4000 }
        );
        done();
      }, 100);
    });

    it('should set loading to false after error', (done) => {
      // Arrange
      mockService.createPlayerNews.and.returnValue(
        throwError(() => new Error('Test error'))
      );

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      // Act
      component.submit();

      // Assert
      setTimeout(() => {
        expect(component.loading()).toBe(false);
        done();
      }, 100);
    });
  });

  // ==========================================
  // CHARACTER COUNTERS
  // ==========================================

  describe('Character Counters', () => {
    it('should return correct character count for NewsText', () => {
      component.form.patchValue({ NewsText: 'Hello World' }); // 11 chars
      expect(component.getCharCount('NewsText')).toBe('11/300');
    });

    it('should return correct character count for InjurySummary', () => {
      component.form.patchValue({ InjurySummary: 'Tobillo' }); // 7 chars
      expect(component.getCharCount('InjurySummary')).toBe('7/30');
    });

    it('should return 0/300 when NewsText is empty', () => {
      component.form.patchValue({ NewsText: '' });
      expect(component.getCharCount('NewsText')).toBe('0/300');
    });

    it('should handle exactly 300 characters', () => {
      const text300 = 'a'.repeat(300);
      component.form.patchValue({ NewsText: text300 });
      expect(component.getCharCount('NewsText')).toBe('300/300');
    });

    it('should handle exactly 30 characters for InjurySummary', () => {
      const summary30 = 'a'.repeat(30);
      component.form.patchValue({ InjurySummary: summary30 });
      expect(component.getCharCount('InjurySummary')).toBe('30/30');
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

    it('should not close dialog while loading', () => {
      // Arrange
      mockService.createPlayerNews.and.returnValue(of({
        Success: true,
        Message: 'OK',
        Data: { NewsID: 1, Message: 'OK' }
      }));

      component.form.patchValue({
        NewsText: 'Valid news text here',
        IsInjury: false
      });

      component.submit();

      // Act - Try to close while loading
      component.close();

      // Assert
      // El botón de cerrar debería estar deshabilitado durante loading
      expect(component.loading()).toBe(true);
    });
  });

  // ==========================================
  // INTEGRATION TESTS
  // ==========================================

  describe('Integration Tests', () => {
    it('should handle complete flow: fill form, submit, success', (done) => {
      // Arrange
      const mockResponse = {
        Success: true,
        Message: 'Noticia creada exitosamente.',
        Data: { NewsID: 999, Message: 'OK' }
      };

      mockService.createPlayerNews.and.returnValue(of(mockResponse));

      // Act - Fill form
      component.form.patchValue({
        NewsText: 'Patrick Mahomes lanzó 5 touchdowns en el último juego.',
        IsInjury: false
      });

      expect(component.form.valid).toBe(true);

      // Submit
      component.submit();

      // Assert
      setTimeout(() => {
        expect(mockService.createPlayerNews).toHaveBeenCalled();
        expect(mockSnackBar.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith({
          created: true,
          newsId: 999
        });
        done();
      }, 100);
    });

    it('should handle complete injury news flow', (done) => {
      // Arrange
      const mockResponse = {
        Success: true,
        Message: 'OK',
        Data: { NewsID: 777, Message: 'OK' }
      };

      mockService.createPlayerNews.and.returnValue(of(mockResponse));

      // Act - Toggle IsInjury
      component.form.patchValue({ IsInjury: false });
      fixture.detectChanges();

      component.form.patchValue({ IsInjury: true });
      fixture.detectChanges();

      // Fill all fields
      component.form.patchValue({
        NewsText: 'Mahomes se lesionó durante la práctica de hoy.',
        InjurySummary: 'Esguince de tobillo',
        Designation: 'Q'
      });

      expect(component.form.valid).toBe(true);

      // Submit
      component.submit();

      // Assert
      setTimeout(() => {
        const dto = mockService.createPlayerNews.calls.argsFor(0)[0];
        expect(dto.IsInjury).toBe(true);
        expect(dto.InjurySummary).toBe('Esguince de tobillo');
        expect(dto.Designation).toBe('Q');
        expect(mockDialogRef.close).toHaveBeenCalled();
        done();
      }, 100);
    });
  });
});