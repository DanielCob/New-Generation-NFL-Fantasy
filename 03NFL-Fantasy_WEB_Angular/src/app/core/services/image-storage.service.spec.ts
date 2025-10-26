import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ImageStorageService } from './image-storage.service';
import { environment } from '../../../environments/environment';

describe('ImageStorageService', () => {
  let service: ImageStorageService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ImageStorageService]
    });
    service = TestBed.inject(ImageStorageService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('debería subir una imagen correctamente', () => {
    const dummyResponse = {
      Success: true,
      Message: 'Imagen cargada exitosamente.',
      Data: { ImageUrl: 'http://server.com/image.png' }
    };
    const file = new File(['data'], 'test.png', { type: 'image/png' });

    service.uploadImage(file).subscribe(res => {
      expect(res.imageUrl).toBe(dummyResponse.Data.ImageUrl);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/Storage/upload-image`);
    expect(req.request.method).toBe('POST');
    req.flush(dummyResponse);
  });

  it('debería eliminar una imagen correctamente', () => {
    const url = 'http://server.com/image.png';

    service.deleteImage(url).subscribe(res => {
      expect(res).toBeTrue();
    });

    const req = httpMock.expectOne(r => r.url.includes(`${environment.apiUrl}/Storage/delete-image`));
    expect(req.request.method).toBe('DELETE');
    req.flush({ Success: true });
  });
});
