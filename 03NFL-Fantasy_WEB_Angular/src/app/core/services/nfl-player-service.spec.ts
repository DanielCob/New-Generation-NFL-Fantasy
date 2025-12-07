import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NFLPlayerService } from './nfl-player-service';
import { environment } from '../../../environments/environment';
import {
  CreatePlayerNewsDTO,
  CreatePlayerNewsResponse,
  DeletePlayerNewsResponse,
  ListPlayerNewsRequest,
  ListPlayerNewsResponse,
  PlayerNewsDetails,
  PlayerNewsItem
} from '../models/nfl-player-model';
import { ApiResponse } from '../models/common-model';

/**
 * Tests unitarios para NFLPlayerService - Player News
 * 
 * COBERTURA:
 * - createPlayerNews: Crear noticias de jugadores
 * - deletePlayerNews: Eliminar noticias de jugadores
 * - getPlayerNews: Listar noticias de un jugador (paginado)
 * - getPlayerNewsDetails: Obtener detalles de una noticia específica
 * 
 * ESTRATEGIA:
 * - HttpClientTestingModule para mockear HTTP requests
 * - HttpTestingController para verificar requests y responses
 * - Validación de URLs, métodos HTTP, y parámetros
 */
describe('NFLPlayerService - Player News', () => {
  let service: NFLPlayerService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/NFLPlayer`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NFLPlayerService]
    });

    service = TestBed.inject(NFLPlayerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Verifica que no haya requests HTTP pendientes
    httpMock.verify();
  });

  // ==========================================
  // TESTS BÁSICOS
  // ==========================================

  describe('Service Creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should have correct base URL', () => {
      expect(baseUrl).toContain('/NFLPlayer');
    });
  });

  // ==========================================
  // CREATE PLAYER NEWS TESTS
  // ==========================================

  describe('createPlayerNews', () => {
    it('should create a regular news successfully', (done) => {
      // Arrange
      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Mahomes tuvo una excelente práctica hoy.',
        IsInjury: false,
        InjurySummary: undefined,
        Designation: undefined
      };

      const mockResponse: CreatePlayerNewsResponse = {
        Success: true,
        Message: 'Noticia creada exitosamente.',
        Data: {
          NewsID: 123,
          Message: 'Noticia creada exitosamente.'
        }
      };

      // Act
      service.createPlayerNews(dto).subscribe({
        next: (response) => {
          // Assert
          expect(response).toEqual(mockResponse);
          expect(response.Success).toBeTrue();
          expect(response.Data?.NewsID).toBe(123);
          done();
        },
        error: () => fail('Should not have failed')
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(`${baseUrl}/news`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);

      req.flush(mockResponse);
    });

    it('should create an injury news with designation successfully', (done) => {
      // Arrange
      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Mahomes se lesionó el tobillo en práctica.',
        IsInjury: true,
        InjurySummary: 'Tobillo derecho',
        Designation: 'Q'
      };

      const mockResponse: CreatePlayerNewsResponse = {
        Success: true,
        Message: 'Noticia de lesión creada exitosamente.',
        Data: {
          NewsID: 456,
          Message: 'Noticia de lesión creada exitosamente.'
        }
      };

      // Act
      service.createPlayerNews(dto).subscribe({
        next: (response) => {
          // Assert
          expect(response.Success).toBeTrue();
          expect(response.Data?.NewsID).toBe(456);
          done();
        },
        error: () => fail('Should not have failed')
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(`${baseUrl}/news`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.IsInjury).toBeTrue();
      expect(req.request.body.InjurySummary).toBe('Tobillo derecho');
      expect(req.request.body.Designation).toBe('Q');

      req.flush(mockResponse);
    });

    it('should log request to console', (done) => {
      // Arrange
      spyOn(console, 'log');

      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Test news',
        IsInjury: false
      };

      const mockResponse: CreatePlayerNewsResponse = {
        Success: true,
        Message: 'OK',
        Data: { NewsID: 1, Message: 'OK' }
      };

      // Act
      service.createPlayerNews(dto).subscribe({
        next: () => {
          // Assert
          expect(console.log).toHaveBeenCalledWith(
            '📤 [NFLPlayerService] createPlayerNews - Request:',
            dto
          );
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news`);
      req.flush(mockResponse);
    });

    it('should handle validation error response', (done) => {
      // Arrange
      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Corto', // Muy corto
        IsInjury: false
      };

      const mockErrorResponse = {
        Success: false,
        Message: 'El texto debe tener al menos 10 caracteres.',
        Data: null
      };

      // Act
      service.createPlayerNews(dto).subscribe({
        next: (response) => {
          // Assert
          expect(response.Success).toBeFalse();
          expect(response.Message).toContain('10 caracteres');
          done();
        },
        error: () => fail('Should handle as response, not error')
      });

      const req = httpMock.expectOne(`${baseUrl}/news`);
      req.flush(mockErrorResponse);
    });

    it('should handle HTTP error', (done) => {
      // Arrange
      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Test news text here',
        IsInjury: false
      };

      const mockError = {
        status: 500,
        statusText: 'Internal Server Error'
      };

      // Act
      service.createPlayerNews(dto).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(500);
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news`);
      req.flush('Server error', mockError);
    });

    it('should send correct URL for createPlayerNews', () => {
      // Arrange
      const dto: CreatePlayerNewsDTO = {
        NFLPlayerID: 100,
        NewsText: 'Test news text here',
        IsInjury: false
      };

      // Act
      service.createPlayerNews(dto).subscribe();

      // Assert
      const req = httpMock.expectOne(`${baseUrl}/news`);
      expect(req.request.url).toBe(`${baseUrl}/news`);
      expect(req.request.urlWithParams).toBe(`${baseUrl}/news`);
      req.flush({ Success: true, Message: 'OK', Data: { NewsID: 1, Message: 'OK' } });
    });

    it('should send all designation types correctly', () => {
      const designations: Array<'O' | 'D' | 'Q' | 'P' | 'FP' | 'IR' | 'PUP' | 'SUS'> = 
        ['O', 'D', 'Q', 'P', 'FP', 'IR', 'PUP', 'SUS'];

      designations.forEach((designation, index) => {
        // Arrange
        const dto: CreatePlayerNewsDTO = {
          NFLPlayerID: 100,
          NewsText: `Test news for designation ${designation}`,
          IsInjury: true,
          InjurySummary: `Injury ${designation}`,
          Designation: designation
        };

        // Act
        service.createPlayerNews(dto).subscribe();

        // Assert
        const req = httpMock.expectOne(`${baseUrl}/news`);
        expect(req.request.body.Designation).toBe(designation);
        req.flush({
          Success: true,
          Message: 'OK',
          Data: { NewsID: index + 1, Message: 'OK' }
        });
      });
    });
  });

  // ==========================================
  // DELETE PLAYER NEWS TESTS
  // ==========================================

  describe('deletePlayerNews', () => {
    it('should delete news successfully', (done) => {
      // Arrange
      const newsId = 123;

      const mockResponse: DeletePlayerNewsResponse = {
        Success: true,
        Message: 'Noticia eliminada exitosamente.',
        Data: {
          Message: 'Noticia eliminada exitosamente.',
          RevertedDesignation: 'Q'
        }
      };

      // Act
      service.deletePlayerNews(newsId).subscribe({
        next: (response) => {
          // Assert
          expect(response).toEqual(mockResponse);
          expect(response.Success).toBeTrue();
          expect(response.Data?.RevertedDesignation).toBe('Q');
          done();
        },
        error: () => fail('Should not have failed')
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      expect(req.request.method).toBe('DELETE');

      req.flush(mockResponse);
    });

    it('should delete news with null reverted designation', (done) => {
      // Arrange
      const newsId = 456;

      const mockResponse: DeletePlayerNewsResponse = {
        Success: true,
        Message: 'Noticia eliminada exitosamente.',
        Data: {
          Message: 'Noticia eliminada exitosamente.',
          RevertedDesignation: null
        }
      };

      // Act
      service.deletePlayerNews(newsId).subscribe({
        next: (response) => {
          // Assert
          expect(response.Data?.RevertedDesignation).toBeNull();
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush(mockResponse);
    });

    it('should log request to console', (done) => {
      // Arrange
      spyOn(console, 'log');
      const newsId = 789;

      const mockResponse: DeletePlayerNewsResponse = {
        Success: true,
        Message: 'OK',
        Data: { Message: 'OK', RevertedDesignation: null }
      };

      // Act
      service.deletePlayerNews(newsId).subscribe({
        next: () => {
          // Assert
          expect(console.log).toHaveBeenCalledWith(
            '📤 [NFLPlayerService] deletePlayerNews - NewsID:',
            newsId
          );
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush(mockResponse);
    });

    it('should handle not found error', (done) => {
      // Arrange
      const newsId = 999;

      const mockError = {
        status: 404,
        statusText: 'Not Found'
      };

      // Act
      service.deletePlayerNews(newsId).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush('Not found', mockError);
    });

    it('should send correct URL with newsId', () => {
      // Arrange
      const newsId = 12345;

      // Act
      service.deletePlayerNews(newsId).subscribe();

      // Assert
      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      expect(req.request.url).toBe(`${baseUrl}/news/${newsId}`);
      req.flush({ Success: true, Message: 'OK', Data: { Message: 'OK' } });
    });

    it('should handle different newsId values', () => {
      const newsIds = [1, 999, 123456789];

      newsIds.forEach(newsId => {
        // Act
        service.deletePlayerNews(newsId).subscribe();

        // Assert
        const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
        expect(req.request.url).toContain(`/news/${newsId}`);
        req.flush({
          Success: true,
          Message: 'OK',
          Data: { Message: 'OK' }
        });
      });
    });
  });

  // ==========================================
  // GET PLAYER NEWS TESTS
  // ==========================================

  describe('getPlayerNews', () => {
    it('should get player news with pagination', (done) => {
      // Arrange
      const playerId = 100;
      const request: ListPlayerNewsRequest = {
        PageNumber: 1,
        PageSize: 10
      };

      const mockNews: PlayerNewsItem[] = [
        {
          NewsID: 1,
          NFLPlayerID: 100,
          NewsText: 'News 1',
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
          NewsText: 'News 2',
          IsInjury: true,
          InjurySummary: 'Tobillo',
          Designation: 'Q',
          CreatedByUserID: 1,
          CreatedByName: 'Admin',
          CreatedAt: '2024-12-05T10:00:00Z'
        }
      ];

      const mockResponse: ApiResponse<ListPlayerNewsResponse> = {
        success: true,
        message: 'OK',
        data: {
          News: mockNews,
          TotalRecords: 2,
          CurrentPage: 1,
          PageSize: 10,
          TotalPages: 1
        }
      };

      // Act
      service.getPlayerNews(playerId, request).subscribe({
        next: (response) => {
          // Assert
          expect(response.success).toBeTrue();
          expect(response.data?.News.length).toBe(2);
          expect(response.data?.TotalRecords).toBe(2);
          expect(response.data?.CurrentPage).toBe(1);
          done();
        },
        error: () => fail('Should not have failed')
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=1&pageSize=10`
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');

      req.flush(mockResponse);
    });

    it('should get player news with default page size', (done) => {
      // Arrange
      const playerId = 100;
      const request: ListPlayerNewsRequest = {
        PageNumber: 2
        // PageSize es opcional
      };

      const mockResponse: ApiResponse<ListPlayerNewsResponse> = {
        success: true,
        message: 'OK',
        data: {
          News: [],
          TotalRecords: 0,
          CurrentPage: 2,
          PageSize: 20, // Default del backend
          TotalPages: 0
        }
      };

      // Act
      service.getPlayerNews(playerId, request).subscribe({
        next: (response) => {
          // Assert
          expect(response.data?.CurrentPage).toBe(2);
          done();
        }
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=2`
      );
      expect(req.request.params.has('pageSize')).toBeFalse();

      req.flush(mockResponse);
    });

    it('should log request to console', (done) => {
      // Arrange
      spyOn(console, 'log');
      const playerId = 100;
      const request: ListPlayerNewsRequest = {
        PageNumber: 1,
        PageSize: 5
      };

      const mockResponse: ApiResponse<ListPlayerNewsResponse> = {
        success: true,
        message: 'OK',
        data: {
          News: [],
          TotalRecords: 0,
          CurrentPage: 1,
          PageSize: 5,
          TotalPages: 0
        }
      };

      // Act
      service.getPlayerNews(playerId, request).subscribe({
        next: () => {
          // Assert
          expect(console.log).toHaveBeenCalledWith(
            '📤 [NFLPlayerService] getPlayerNews - PlayerID:',
            playerId,
            'Request:',
            request
          );
          done();
        }
      });

      const req = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=1&pageSize=5`
      );
      req.flush(mockResponse);
    });

    it('should handle empty news list', (done) => {
      // Arrange
      const playerId = 999;
      const request: ListPlayerNewsRequest = {
        PageNumber: 1,
        PageSize: 10
      };

      const mockResponse: ApiResponse<ListPlayerNewsResponse> = {
        success: true,
        message: 'No hay noticias para este jugador.',
        data: {
          News: [],
          TotalRecords: 0,
          CurrentPage: 1,
          PageSize: 10,
          TotalPages: 0
        }
      };

      // Act
      service.getPlayerNews(playerId, request).subscribe({
        next: (response) => {
          // Assert
          expect(response.data?.News).toEqual([]);
          expect(response.data?.TotalRecords).toBe(0);
          done();
        }
      });

      const req = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=1&pageSize=10`
      );
      req.flush(mockResponse);
    });

    it('should build correct URL with parameters', () => {
      // Arrange
      const playerId = 200;
      const request: ListPlayerNewsRequest = {
        PageNumber: 3,
        PageSize: 15
      };

      // Act
      service.getPlayerNews(playerId, request).subscribe();

      // Assert
      const req = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=3&pageSize=15`
      );
      expect(req.request.url).toBe(`${baseUrl}/${playerId}/news`);
      expect(req.request.params.get('pageNumber')).toBe('3');
      expect(req.request.params.get('pageSize')).toBe('15');
      
      req.flush({
        Success: true,
        Message: 'OK',
        Data: {
          News: [],
          TotalRecords: 0,
          CurrentPage: 3,
          PageSize: 15,
          TotalPages: 0
        }
      });
    });

    it('should handle different pagination scenarios', () => {
      const scenarios = [
        { playerId: 100, pageNumber: 1, pageSize: 10 },
        { playerId: 200, pageNumber: 5, pageSize: 20 },
        { playerId: 300, pageNumber: 10, pageSize: 50 }
      ];

      scenarios.forEach(scenario => {
        // Act
        service.getPlayerNews(scenario.playerId, {
          PageNumber: scenario.pageNumber,
          PageSize: scenario.pageSize
        }).subscribe();

        // Assert
        const req = httpMock.expectOne(
          `${baseUrl}/${scenario.playerId}/news?pageNumber=${scenario.pageNumber}&pageSize=${scenario.pageSize}`
        );
        expect(req.request.method).toBe('GET');
        
        req.flush({
          Success: true,
          Message: 'OK',
          Data: {
            News: [],
            TotalRecords: 0,
            CurrentPage: scenario.pageNumber,
            PageSize: scenario.pageSize,
            TotalPages: 0
          }
        });
      });
    });
  });

  // ==========================================
  // GET PLAYER NEWS DETAILS TESTS
  // ==========================================

  describe('getPlayerNewsDetails', () => {
    it('should get news details successfully', (done) => {
      // Arrange
      const newsId = 123;

      const mockDetails: PlayerNewsDetails = {
        NewsID: 123,
        NFLPlayerID: 100,
        PlayerFirstName: 'Patrick',
        PlayerLastName: 'Mahomes',
        PlayerFullName: 'Patrick Mahomes',
        NewsText: 'Mahomes tuvo una excelente práctica.',
        IsInjury: false,
        InjurySummary: undefined,
        Designation: undefined,
        CreatedByUserID: 1,
        CreatedByName: 'Admin User',
        CreatedAt: '2024-12-06T10:00:00Z',
        IsDeleted: false
      };

      const mockResponse: ApiResponse<PlayerNewsDetails> = {
        success: true,
        message: 'OK',
        data: mockDetails
      };

      // Act
      service.getPlayerNewsDetails(newsId).subscribe({
        next: (response) => {
          // Assert
          expect(response.success).toBeTrue();
          expect(response.data?.NewsID).toBe(123);
          expect(response.data?.PlayerFullName).toBe('Patrick Mahomes');
          expect(response.data?.IsInjury).toBeFalse();
          done();
        },
        error: () => fail('Should not have failed')
      });

      // Assert HTTP Request
      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      expect(req.request.method).toBe('GET');

      req.flush(mockResponse);
    });

    it('should get injury news details with designation', (done) => {
      // Arrange
      const newsId = 456;

      const mockDetails: PlayerNewsDetails = {
        NewsID: 456,
        NFLPlayerID: 100,
        PlayerFirstName: 'Patrick',
        PlayerLastName: 'Mahomes',
        PlayerFullName: 'Patrick Mahomes',
        NewsText: 'Mahomes se lesionó el tobillo.',
        IsInjury: true,
        InjurySummary: 'Tobillo derecho',
        Designation: 'Q',
        CreatedByUserID: 1,
        CreatedByName: 'Admin User',
        CreatedAt: '2024-12-06T10:00:00Z',
        IsDeleted: false
      };

      const mockResponse: ApiResponse<PlayerNewsDetails> = {
        success: true,
        message: 'OK',
        data: mockDetails
      };

      // Act
      service.getPlayerNewsDetails(newsId).subscribe({
        next: (response) => {
          // Assert
          expect(response.data?.IsInjury).toBeTrue();
          expect(response.data?.InjurySummary).toBe('Tobillo derecho');
          expect(response.data?.Designation).toBe('Q');
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush(mockResponse);
    });

    it('should log request to console', (done) => {
      // Arrange
      spyOn(console, 'log');
      const newsId = 789;

      const mockResponse: ApiResponse<PlayerNewsDetails> = {
        success: true,
        message: 'OK',
        data: {} as PlayerNewsDetails
      };

      // Act
      service.getPlayerNewsDetails(newsId).subscribe({
        next: () => {
          // Assert
          expect(console.log).toHaveBeenCalledWith(
            '📤 [NFLPlayerService] getPlayerNewsDetails - NewsID:',
            newsId
          );
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush(mockResponse);
    });

    it('should handle not found error', (done) => {
      // Arrange
      const newsId = 999;

      const mockError = {
        status: 404,
        statusText: 'Not Found'
      };

      // Act
      service.getPlayerNewsDetails(newsId).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          // Assert
          expect(error.status).toBe(404);
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush('News not found', mockError);
    });

    it('should send correct URL with newsId', () => {
      // Arrange
      const newsId = 12345;

      // Act
      service.getPlayerNewsDetails(newsId).subscribe();

      // Assert
      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      expect(req.request.url).toBe(`${baseUrl}/news/${newsId}`);
      expect(req.request.method).toBe('GET');
      
      req.flush({
        Success: true,
        Message: 'OK',
        Data: {} as PlayerNewsDetails
      });
    });

    it('should handle deleted news', (done) => {
      // Arrange
      const newsId = 100;

      const mockDetails: PlayerNewsDetails = {
        NewsID: 100,
        NFLPlayerID: 50,
        PlayerFirstName: 'Test',
        PlayerLastName: 'Player',
        PlayerFullName: 'Test Player',
        NewsText: 'Deleted news',
        IsInjury: false,
        CreatedByUserID: 1,
        CreatedByName: 'Admin',
        CreatedAt: '2024-12-01T10:00:00Z',
        IsDeleted: true  // Noticia eliminada
      };

      const mockResponse: ApiResponse<PlayerNewsDetails> = {
        success: true,
        message: 'OK',
        data: mockDetails
      };

      // Act
      service.getPlayerNewsDetails(newsId).subscribe({
        next: (response) => {
          // Assert
          expect(response.data?.IsDeleted).toBeTrue();
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
      req.flush(mockResponse);
    });
  });

  // ==========================================
  // INTEGRATION TESTS
  // ==========================================

  describe('Integration Scenarios', () => {
    it('should create, retrieve, and delete news in sequence', (done) => {
    const createDto: CreatePlayerNewsDTO = {
      NFLPlayerID: 100,
      NewsText: 'Integration test news',
      IsInjury: false
    };

    service.createPlayerNews(createDto).subscribe({
      next: (createResponse) => {
        expect(createResponse.Success).toBeTrue();
        const newsId = createResponse.Data?.NewsID!;

        service.getPlayerNewsDetails(newsId).subscribe({
          next: (detailsResponse) => {
            expect(detailsResponse.success).toBeTrue();
            expect(detailsResponse.data?.NewsID).toBe(newsId);

            service.deletePlayerNews(newsId).subscribe({
              next: (deleteResponse) => {
                expect(deleteResponse.Success).toBeTrue();
                done();
              }
            });

            const deleteReq = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
            deleteReq.flush({
              Success: true,
              Message: 'OK',
              Data: { Message: 'OK', RevertedDesignation: null }
            });
          }
        });

        const detailsReq = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
        detailsReq.flush({
          success: true,
          message: 'OK',
          data: {
            NewsID: newsId,
            NewsText: 'Integration test news'
          } as PlayerNewsDetails
        });
      }
    });

    const createReq = httpMock.expectOne(`${baseUrl}/news`);
    createReq.flush({
      Success: true,
      Message: 'OK',
      Data: { NewsID: 999, Message: 'OK' }
    });
    });

    it('should handle multiple concurrent requests', () => {
      // Arrange
      const playerId = 100;
      const newsIds = [1, 2, 3];

      // Act - Hacer múltiples requests simultáneos
      newsIds.forEach(newsId => {
        service.getPlayerNewsDetails(newsId).subscribe();
      });

      service.getPlayerNews(playerId, { PageNumber: 1 }).subscribe();

      // Assert - Verificar que todos los requests se hicieron
      newsIds.forEach(newsId => {
        const req = httpMock.expectOne(`${baseUrl}/news/${newsId}`);
        req.flush({
          Success: true,
          Message: 'OK',
          Data: { NewsID: newsId } as PlayerNewsDetails
        });
      });

      const listReq = httpMock.expectOne(
        `${baseUrl}/${playerId}/news?pageNumber=1`
      );
      listReq.flush({
        Success: true,
        Message: 'OK',
        Data: {
          News: [],
          TotalRecords: 0,
          CurrentPage: 1,
          PageSize: 20,
          TotalPages: 0
        }
      });
    });
  });
});