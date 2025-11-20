import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';

import { NFLPlayerService } from '../../../core/services/nfl-player-service';
import { NFLTeamService } from '../../../core/services/nfl-team-service';
import { NFLTeamBasic } from '../../../core/models/nfl-team-model';
import { CreateBatchReportRequest, CreateNFLPlayerDTO } from '../../../core/models/nfl-player-model';
import { ImageStorageService } from '../../../core/services/image-storage.service';

import { firstValueFrom } from 'rxjs';

interface RawRecord { [k: string]: any; __row?: number; }
interface ParsedRecord {
  row: number;
  source: RawRecord;
  extId?: string | number;
  name?: string;
  position?: string;
  teamInput?: string | number;
  image?: string;
}
interface ValidRecord extends ParsedRecord {
  firstName: string;
  lastName: string;
  position: string;
  nflTeamID: number;
  image?: string;    // URL de origen (CSV/JSON)
}

// ===== INTERFACES DE VALIDACIÓN =====
interface ValidationResult {
  valid: boolean;
  errors: string[];
}

interface FileValidationConfig {
  maxRecords: number;
  maxFileSize: number; // bytes
  requiredFields: string[];
  allowedImageDomains: string[];
}

@Component({
  selector: 'app-nfl-player-batch-upload',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './nfl-player-batch-upload.page.html',
  styleUrl: './nfl-player-batch-upload.page.css'
})
export class NFLPlayerBatchUploadPage {
  private snack = inject(MatSnackBar);
  private players = inject(NFLPlayerService);
  private teamsSvc = inject(NFLTeamService);
  private imageSvc = inject(ImageStorageService);

  loading = signal(false);
  checking = signal(false);
  uploading = signal(false);

  teams = signal<NFLTeamBasic[]>([]);
  allowedPositions = new Set(['QB','RB','WR','TE','K','DEF']);

  fileName = signal<string>('');
  parsed = signal<ParsedRecord[]>([]);
  valid = signal<ValidRecord[]>([]);
  errors = signal<string[]>([]);
  existingConflicts = signal<string[]>([]);

  report = signal<{ created: number; failed: number; errors: string[] } | null>(null);

  // Para exportar exactamente lo que se creó (con URLs finales)
  createdItems = signal<Array<{
    ExternalID: string | number | null;
    FullName: string;
    Position: string;
    NFLTeamID: number;
    PhotoUrl: string | null;
    ThumbnailUrl: string | null;
    PhotoWidth?: number;
    PhotoHeight?: number;
    PhotoBytes?: number;
  }>>([]);

  ngOnInit(): void {
    this.refreshTeams();
  }

  getTeamName(teamId: number): string {
    const team = this.teams().find(t => t.NFLTeamID === teamId);
    return team?.TeamName || `ID ${teamId}`;
  }

  refreshTeams(): void {
    this.teamsSvc.getActive().subscribe({
      next: (resp: any) => {
        const arr: NFLTeamBasic[] = resp?.data ?? resp?.Data ?? [];
        this.teams.set(Array.isArray(arr) ? arr : []);
      },
      error: _ => {
        this.teams.set([]);
        this.snack.open('No se pudieron cargar los equipos', 'OK', { duration: 3000 });
      }
    });
  }

  // ===== ACTUALIZAR onFileSelected =====
  onFileSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) return;
    
    console.log('\n🔒 ========== INICIANDO VALIDACIONES DE SEGURIDAD ==========');
    
    // VALIDACIÓN 1: Extensión del archivo
    const extension = file.name.toLowerCase().split('.').pop();
    if (!['csv', 'json'].includes(extension || '')) {
      this.snack.open('Formato no soportado. Solo se permiten archivos .csv y .json', 'OK', { duration: 4000 });
      input.value = '';
      return;
    }
    
    // VALIDACIÓN 2: Tamaño del archivo
    const sizeValidation = this.validateFileSize(file);
    if (!sizeValidation.valid) {
      this.snack.open(sizeValidation.errors[0], 'OK', { duration: 4000 });
      input.value = '';
      return;
    }
    
    this.fileName.set(file.name);
    this.report.set(null);
    this.createdItems.set([]);

    const reader = new FileReader();
    reader.onload = () => {
      const text = String(reader.result || '');
      
      // VALIDACIÓN 3: Contenido del archivo
      const contentValidation = this.validateFileContent(text);
      if (!contentValidation.valid) {
        this.snack.open('El archivo contiene contenido no permitido', 'OK', { duration: 4000 });
        this.errors.set(contentValidation.errors);
        input.value = '';
        return;
      }
      
      let records: RawRecord[] = [];
      
      try {
        if (file.name.toLowerCase().endsWith('.csv')) {
          records = this.parseCSV(text);
        } else if (file.name.toLowerCase().endsWith('.json')) {
          const data = JSON.parse(text);
          if (Array.isArray(data)) {
            records = data as RawRecord[];
          } else if (Array.isArray((data as any).data)) {
            records = (data as any).data as RawRecord[];
          } else if (Array.isArray((data as any).players)) {
            records = (data as any).players as RawRecord[];
          } else {
            this.snack.open('Estructura JSON no reconocida. Debe ser un array o contener una propiedad "data" o "players" con un array', 'OK', { duration: 5000 });
            input.value = '';
            return;
          }
        }
      } catch (parseError: any) {
        console.error('❌ Error parseando archivo:', parseError);
        this.snack.open(`Error al leer el archivo: ${parseError.message}`, 'OK', { duration: 4000 });
        input.value = '';
        return;
      }
      
      // VALIDACIÓN 4: Cantidad de registros
      const countValidation = this.validateRecordCount(records);
      if (!countValidation.valid) {
        this.snack.open(countValidation.errors[0], 'OK', { duration: 4000 });
        this.errors.set(countValidation.errors);
        input.value = '';
        return;
      }
      
      // Normalizar y validar cada registro
      const parsed = records.map((r, i) => this.normalizeRecord(r, i + 1));
      
      // VALIDACIÓN 5: Validar datos de cada jugador
      console.log('\n🔒 [SEGURIDAD] Validando datos de cada jugador...');
      const validationErrors: string[] = [];
      
      for (const record of parsed) {
        const validation = this.validatePlayerData(record);
        if (!validation.valid) {
          validationErrors.push(...validation.errors);
        }
      }
      
      if (validationErrors.length > 0) {
        console.error('❌ [SEGURIDAD] Se encontraron errores de validación:', validationErrors.length);
        this.errors.set(validationErrors);
        this.parsed.set(parsed);
        this.valid.set([]);
        this.snack.open(`Se encontraron ${validationErrors.length} errores de validación. Revisa los detalles abajo.`, 'OK', { duration: 5000 });
        return;
      }
      
      console.log('✅ [SEGURIDAD] Todas las validaciones pasaron exitosamente');
      this.parsed.set(parsed);
      this.validateAll();
    };
    
    reader.onerror = () => {
      this.snack.open('Error al leer el archivo', 'OK', { duration: 3000 });
      input.value = '';
    };
    
    reader.readAsText(file);
  }

  private parseCSV(text: string): RawRecord[] {
    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
    if (lines.length === 0) return [];
    const header = lines[0].split(',').map(h => h.trim());
    const out: RawRecord[] = [];
    for (let i = 1; i < lines.length; i++) {
      const row = this.splitCSVLine(lines[i]);
      const obj: RawRecord = {}; obj.__row = i + 1;
      header.forEach((h, idx) => obj[h] = row[idx]);
      out.push(obj);
    }
    return out;
  }

  private splitCSVLine(line: string): string[] {
    const res: string[] = [];
    let cur = ''; let inQuotes = false;
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (ch === '"') { inQuotes = !inQuotes; continue; }
      if (ch === ',' && !inQuotes) { res.push(cur.trim()); cur = ''; }
      else { cur += ch; }
    }
    res.push(cur.trim());
    return res;
  }

  // ===== ACTUALIZAR normalizeRecord PARA SANITIZAR =====
  private normalizeRecord(r: RawRecord, row: number): ParsedRecord {
    const pick = (keys: string[]): any => {
      for (const k of keys) {
        if (r[k] !== undefined && r[k] !== null && String(r[k]).trim() !== '') {
          return this.sanitizeString(String(r[k]), 200);
        }
      }
      return undefined;
    };
    
    const extId = pick(['ID','Id','id','ExternalID','ExternalId']);
    const name = pick(['Name','Nombre','FullName','Fullname','fullName']);
    const position = pick(['Position','Posicion','Posición','pos','POS']);
    const teamInput = pick(['NFLTeamID','TeamID','Team','Equipo','EquipoNFL','NFLTeam','nflTeam']);
    const image = pick(['Image','Imagen','PhotoUrl','ImageUrl','image']);
    
    return { row, source: r, extId, name, position, teamInput, image };
  }

  async validateAll(): Promise<void> {
    this.errors.set([]);
    this.valid.set([]);
    this.existingConflicts.set([]);

    const teams = this.teams();
    const byName = new Map(teams.map(t => [t.TeamName?.toLowerCase?.() ?? String(t.NFLTeamID), t]));
    const byId = new Map(teams.map(t => [t.NFLTeamID, t]));

    const localDupSet = new Set<string>();
    const localDupSeen = new Set<string>();

    const valids: ValidRecord[] = [];
    const errors: string[] = [];

    for (const rec of this.parsed()) {
      const errsForRow: string[] = [];
      const name = (rec.name || '').toString().trim();
      const pos = (rec.position || '').toString().trim().toUpperCase();
      let teamId: number | undefined;
      if (typeof rec.teamInput === 'number') {
        teamId = rec.teamInput;
      } else if (typeof rec.teamInput === 'string') {
        const s = rec.teamInput.trim();
        const asNum = Number(s);
        if (!isNaN(asNum)) teamId = asNum; else {
          const t = byName.get(s.toLowerCase());
          if (t) teamId = t.NFLTeamID;
        }
      }

      if (!name) errsForRow.push(`[Fila ${rec.row}] Falta nombre`);
      if (!pos || !this.allowedPositions.has(pos)) errsForRow.push(`[Fila ${rec.row}] Posición inválida: ${pos || '(vacía)'}`);
      if (!teamId) errsForRow.push(`[Fila ${rec.row}] Equipo NFL inválido o no encontrado`);

      let firstName = ''; let lastName = '';
      if (name) {
        const parts = name.split(' ').filter(x => x);
        if (parts.length >= 2) { lastName = parts.pop()!; firstName = parts.join(' '); }
        else { errsForRow.push(`[Fila ${rec.row}] Nombre debe incluir nombre y apellido`); }
      }

      const dupKey = `${name.toLowerCase()}|${teamId ?? 'X'}`;
      if (localDupSet.has(dupKey)) {
        if (!localDupSeen.has(dupKey)) {
          const teamName = this.teams().find(t => t.NFLTeamID === teamId)?.TeamName || `ID ${teamId}`;
          errors.push(`[Duplicado en archivo] ${name} - ${teamName}`);
          localDupSeen.add(dupKey);
        }
      } else {
        localDupSet.add(dupKey);
      }

      if (errsForRow.length === 0) {
        valids.push({
          row: rec.row,
          source: rec.source,
          extId: rec.extId,
          name,
          position: pos,
          teamInput: rec.teamInput,
          image: rec.image,
          firstName,
          lastName,
          nflTeamID: teamId!
        });
      } else {
        errors.push(...errsForRow);
      }
    }

    this.valid.set(valids);
    this.errors.set(errors);

    await this.checkExistingConflicts();
  }

  private async checkExistingConflicts(): Promise<void> {
    this.checking.set(true);
    const conflicts: string[] = [];

    for (const v of this.valid()) {
      try {
        if (v.nflTeamID && v.nflTeamID > 0) {
          const resp: any = await new Promise((resolve, reject) => {
            this.players.list({ PageNumber: 1, PageSize: 20, SearchTerm: v.firstName + ' ' + v.lastName, FilterNFLTeamID: v.nflTeamID })
              .subscribe({ next: resolve, error: reject });
          });
          const data = resp?.data ?? resp?.Data;
          const players: any[] = data?.Players ?? [];
          const exists = players.some(p =>
            (p.FullName || (`${p.FirstName} ${p.LastName}`)).toLowerCase() === (v.firstName + ' ' + v.lastName).toLowerCase()
            && Number(p.NFLTeamID) === v.nflTeamID
          );
          if (exists) {
            const teamName = this.teams().find(t => t.NFLTeamID === v.nflTeamID)?.TeamName || `ID ${v.nflTeamID}`;
            conflicts.push(`[Ya existe] ${v.firstName} ${v.lastName} en ${teamName}`);
          }
        }
      } catch {
        // silencio por registro para no bloquear el checado global
      }
    }

    this.existingConflicts.set(conflicts);
    this.checking.set(false);
  }

  canUpload(): boolean {
    return this.parsed().length > 0 && this.errors().length === 0 && this.existingConflicts().length === 0 && !this.uploading();
  }

  /** =======================
   *  Helpers de imágenes
   *  ======================= */
  private async fetchImageAsFile(url: string, suggestedName: string): Promise<File> {
    // intenta CORS, luego fallback
    const res = await fetch(url, { mode: 'cors' }).catch(() => fetch(url));
    if (!res || !res.ok) throw new Error('No se pudo descargar la imagen: ' + url);
    const blob = await res.blob();
    const name = (suggestedName || 'image').replace(/[^\w.-]+/g, '_');
    const type = blob.type && blob.type.startsWith('image/') ? blob.type : 'image/jpeg';
    return new File([blob], name, { type });
  }

  private getImageMeta(file: File): Promise<{ width: number; height: number; bytes: number; }> {
    return new Promise((resolve, reject) => {
      const url = URL.createObjectURL(file);
      const img = new Image();
      img.onload = () => {
        const meta = { width: img.width, height: img.height, bytes: file.size };
        URL.revokeObjectURL(url);
        resolve(meta);
      };
      img.onerror = () => {
        URL.revokeObjectURL(url);
        reject(new Error('No se pudieron obtener dimensiones de la imagen'));
      };
      img.src = url;
    });
  }

  /** Genera un thumbnail local cuadrado (cover) y retorna como File PNG */
  private async buildLocalThumbnailFile(srcFile: File, size = 512): Promise<File> {  // ✅ CAMBIO: 96 → 512
    const imgUrl = URL.createObjectURL(srcFile);
    try {
      const img = await new Promise<HTMLImageElement>((resolve, reject) => {
        const i = new Image();
        i.onload = () => resolve(i);
        i.onerror = reject;
        i.src = imgUrl;
      });

      // recorte tipo "cover" para preservar encuadre
      const canvas = document.createElement('canvas');
      canvas.width = size; canvas.height = size;
      const ctx = canvas.getContext('2d');
      if (!ctx) throw new Error('No se pudo inicializar canvas');

      const srcW = img.width, srcH = img.height;
      const scale = Math.max(size / srcW, size / srcH);
      const drawW = srcW * scale, drawH = srcH * scale;
      const dx = (size - drawW) / 2;
      const dy = (size - drawH) / 2;

      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = 'high';
      ctx.drawImage(img, dx, dy, drawW, drawH);

      const blob: Blob = await new Promise(resolve => canvas.toBlob(b => resolve(b as Blob), 'image/png'));
      const thumbName = srcFile.name.replace(/\.(\w+)$/, '') + `__thumb_${size}.png`;
      return new File([blob], thumbName, { type: 'image/png' });
    } finally {
      URL.revokeObjectURL(imgUrl);
    }
  }

  // 4. ACTUALIZAR nfl-player-batch-upload.page.ts (MÉTODO uploadAll COMPLETO)
  async uploadAll(): Promise<void> {
    if (!this.canUpload()) return;
    
    console.log('🚀 [BATCH UPLOAD] ========== INICIANDO PROCESO ==========');
    console.log('📊 [BATCH UPLOAD] Total jugadores a procesar:', this.valid().length);
    
    this.uploading.set(true);
    this.createdItems.set([]);

    try {
      // ========== PASO 1: Descargar imágenes y generar thumbnails ==========
      console.log('\n📥 [PASO 1] Descargando imágenes y generando thumbnails...');
      
      const filesToUpload: File[] = [];
      const imageMapping = new Map<number, { 
        originalIndex: number; 
        thumbIndex: number;
        originalMeta: { width: number; height: number; bytes: number };
        thumbMeta: { width: number; height: number; bytes: number };
      }>();
      
      for (let i = 0; i < this.valid().length; i++) {
        const v = this.valid()[i];
        console.log(`\n🖼️  [PASO 1.${i + 1}] Procesando jugador: ${v.firstName} ${v.lastName}`);
        
        if (v.image && typeof v.image === 'string' && v.image.trim().length > 0) {
          try {
            const baseName = `${v.firstName}_${v.lastName}_${v.nflTeamID}`.toLowerCase().replace(/[^\w.-]+/g, '_');
            
            console.log(`  📡 Descargando imagen original de: ${v.image}`);
            const originalFile = await this.fetchImageAsFile(v.image, `${baseName}.jpg`);
            console.log(`  ✅ Imagen descargada: ${originalFile.name} (${originalFile.size} bytes)`);
            
            // ✅ Obtener metadata de la imagen original
            const originalMeta = await this.getImageMeta(originalFile);
            console.log(`  📐 Dimensiones originales: ${originalMeta.width}x${originalMeta.height}`);
            
            console.log(`  🔄 Generando thumbnail 512x512...`);  // ✅ CAMBIO: 96 → 512
            const thumbFile = await this.buildLocalThumbnailFile(originalFile, 512);  // ✅ CAMBIO: 96 → 512
            console.log(`  ✅ Thumbnail generado: ${thumbFile.name} (${thumbFile.size} bytes)`);
            
            // ✅ Obtener metadata del thumbnail
            const thumbMeta = await this.getImageMeta(thumbFile);
            console.log(`  📐 Dimensiones thumbnail: ${thumbMeta.width}x${thumbMeta.height}`);
            
            const originalIndex = filesToUpload.length;
            filesToUpload.push(originalFile);
            
            const thumbIndex = filesToUpload.length;
            filesToUpload.push(thumbFile);
            
            imageMapping.set(i, { 
              originalIndex, 
              thumbIndex,
              originalMeta,
              thumbMeta
            });
            console.log(`  📌 Mapeado - Original: index ${originalIndex}, Thumb: index ${thumbIndex}`);
          } catch (err: any) {
            console.error(`  ❌ Error procesando imagen para ${v.firstName} ${v.lastName}:`, err.message);
          }
        } else {
          console.log(`  ℹ️  Sin imagen para procesar`);
        }
      }
      
      console.log(`\n📦 [PASO 1 COMPLETADO] Total archivos preparados: ${filesToUpload.length}`);
      console.log(`📋 Mapping creado para ${imageMapping.size} jugadores con imágenes`);

      // ========== PASO 2: Subida masiva de imágenes ==========
      console.log('\n📤 [PASO 2] Subiendo TODAS las imágenes en lote...');
      
      let uploadedImages: Array<{ ImageUrl: string; FileName: string }> = [];
      
      if (filesToUpload.length > 0) {
        const imageUploadResult = await firstValueFrom(this.imageSvc.uploadImages(filesToUpload));
        console.log('✅ [PASO 2] Respuesta de subida masiva:', imageUploadResult);
        
        if (!imageUploadResult.Success || imageUploadResult.Data.ErrorCount > 0) {
          throw new Error(`Error en subida de imágenes: ${imageUploadResult.Message}. Errores: ${imageUploadResult.Data.Errors.join(', ')}`);
        }
        
        uploadedImages = imageUploadResult.Data.UploadedImages;
        console.log(`✅ [PASO 2 COMPLETADO] ${uploadedImages.length} imágenes subidas exitosamente`);
      } else {
        console.log('ℹ️  [PASO 2 OMITIDO] No hay imágenes para subir');
      }

      // ========== PASO 3: Preparar DTOs de jugadores ==========
      console.log('\n🎯 [PASO 3] Preparando DTOs de jugadores...');
      
      const playerDTOs: CreateNFLPlayerDTO[] = this.valid().map((v, i) => {
        console.log(`\n👤 [PASO 3.${i + 1}] Jugador: ${v.firstName} ${v.lastName}`);
        
        const dto: CreateNFLPlayerDTO = {
          FirstName: v.firstName,
          LastName: v.lastName,
          Position: v.position,
          NFLTeamID: v.nflTeamID
        };

        const mapping = imageMapping.get(i);
        if (mapping && uploadedImages.length > 0) {
          const originalImg = uploadedImages[mapping.originalIndex];
          const thumbImg = uploadedImages[mapping.thumbIndex];
          
          if (originalImg && thumbImg) {
            console.log(`  🖼️  Original URL: ${originalImg.ImageUrl}`);
            console.log(`  🖼️  Thumbnail URL: ${thumbImg.ImageUrl}`);
            
            dto.PhotoUrl = originalImg.ImageUrl;
            dto.ThumbnailUrl = thumbImg.ImageUrl;
            dto.PhotoThumbnailUrl = thumbImg.ImageUrl;
            
            // ✅ Usar las dimensiones reales calculadas
            dto.PhotoWidth = mapping.originalMeta.width;
            dto.PhotoHeight = mapping.originalMeta.height;
            dto.PhotoBytes = mapping.originalMeta.bytes;
            
            dto.ThumbnailWidth = mapping.thumbMeta.width;   // ✅ Ahora será 512
            dto.ThumbnailHeight = mapping.thumbMeta.height; // ✅ Ahora será 512
            dto.ThumbnailBytes = mapping.thumbMeta.bytes;
            
            console.log(`  📐 Original: ${dto.PhotoWidth}x${dto.PhotoHeight} (${dto.PhotoBytes} bytes)`);
            console.log(`  📐 Thumbnail: ${dto.ThumbnailWidth}x${dto.ThumbnailHeight} (${dto.ThumbnailBytes} bytes)`);
          }
        } else {
          console.log(`  ℹ️  Sin imágenes`);
        }
        
        console.log(`  ✅ DTO preparado:`, JSON.stringify(dto, null, 2));
        return dto;
      });
      
      console.log(`\n✅ [PASO 3 COMPLETADO] ${playerDTOs.length} DTOs preparados`);

      // ========== PASO 4: Subida masiva de jugadores ==========
      console.log('\n📤 [PASO 4] Creando TODOS los jugadores en lote...');
      
      const batchResult = await firstValueFrom(this.players.createBatch(playerDTOs));
      console.log('✅ [PASO 4] Respuesta de creación masiva:', batchResult);
      
      if (!batchResult.Success) {
        throw new Error(`Error en creación de jugadores: ${batchResult.Message}`);
      }
      
      console.log(`✅ [PASO 4 COMPLETADO] ${batchResult.Data.SuccessCount} jugadores creados, ${batchResult.Data.ErrorCount} errores`);

      // ========== PASO 5: Generar y subir reporte JSON ==========
      console.log('\n📄 [PASO 5] Generando reporte JSON...');
      
      const status = batchResult.Data.ErrorCount === 0 ? 'EXITO' : 'FALLO';
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const reportFileName = `${status}_${timestamp}_${this.fileName()}.json`;
      
      const reportPayload = {
        status,
        timestamp: new Date().toISOString(),
        sourceFile: this.fileName(),
        totalProcessed: batchResult.Data.TotalProcessed,
        successCount: batchResult.Data.SuccessCount,
        errorCount: batchResult.Data.ErrorCount,
        createdPlayers: batchResult.Data.CreatedPlayers,
        errors: batchResult.Data.Errors
      };
      
      console.log('📋 [PASO 5] Payload del reporte:', reportPayload);
      
      const jsonBlob = new Blob([JSON.stringify(reportPayload, null, 2)], { type: 'application/json' });
      console.log(`📦 [PASO 5] Blob creado: ${jsonBlob.size} bytes`);
      
      console.log('📤 [PASO 5] Subiendo JSON a storage...');
      const jsonUploadResult = await firstValueFrom(this.imageSvc.uploadJson(jsonBlob, reportFileName));
      console.log('✅ [PASO 5] JSON subido:', jsonUploadResult);
      
      if (!jsonUploadResult.Success) {
        throw new Error(`Error subiendo JSON: ${jsonUploadResult.Message}`);
      }
      
      const reportUrl = jsonUploadResult.Data.JsonUrl;
      console.log(`✅ [PASO 5 COMPLETADO] Reporte URL: ${reportUrl}`);

      // ========== PASO 6: Crear registro de batch report ==========
      console.log('\n💾 [PASO 6] Creando registro de batch report...');
      
      const batchReportRequest: CreateBatchReportRequest = {
        ReportUrl: reportUrl,
        TotalProcessed: batchResult.Data.TotalProcessed,
        SuccessCount: batchResult.Data.SuccessCount,
        ErrorCount: batchResult.Data.ErrorCount,
        Message: batchResult.Message
      };
      
      console.log('📋 [PASO 6] Request:', batchReportRequest);
      
      const batchReportResult = await firstValueFrom(this.players.createBatchReport(batchReportRequest));
      console.log('✅ [PASO 6] Respuesta:', batchReportResult);
      
      if (!batchReportResult.Success) {
        console.warn('⚠️  [PASO 6] Error creando registro (no crítico):', batchReportResult.Message);
      } else {
        console.log(`✅ [PASO 6 COMPLETADO] BatchReportID: ${batchReportResult.Data.BatchReportID}`);
      }

      // ========== RESULTADO FINAL ==========
      console.log('\n🎉 ========== PROCESO COMPLETADO ==========');
      console.log(`✅ Jugadores creados: ${batchResult.Data.SuccessCount}`);
      console.log(`❌ Errores: ${batchResult.Data.ErrorCount}`);
      console.log(`📄 Reporte URL: ${reportUrl}`);
      
      this.report.set({
        created: batchResult.Data.SuccessCount,
        failed: batchResult.Data.ErrorCount,
        errors: batchResult.Data.Errors
      });
      
      this.snack.open(
        `Proceso completado: ${batchResult.Data.SuccessCount} jugadores creados, ${batchResult.Data.ErrorCount} errores`,
        'OK',
        { duration: 5000 }
      );

    } catch (error: any) {
      console.error('\n❌ ========== ERROR EN EL PROCESO ==========');
      console.error('❌ Error:', error);
      console.error('❌ Mensaje:', error.message);
      console.error('❌ Stack:', error.stack);
      
      this.snack.open(
        `Error en el proceso: ${error.message}`,
        'OK',
        { duration: 6000 }
      );
      
      this.report.set({
        created: 0,
        failed: this.valid().length,
        errors: [error.message]
      });
    } finally {
      this.uploading.set(false);
      console.log('\n🏁 ========== FIN DEL PROCESO ==========\n');
    }
  }

  downloadProcessedJson(): void {
    const name = this.fileName();
    if (!name) return;
    const base = name.replace(/\.(csv|json)$/i, '');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    const fileName = `${base}__${ts}.json`;
    const payload = {
      file: name,
      created: this.report()?.created ?? 0,
      failed: this.report()?.failed ?? 0,
      errors: this.report()?.errors ?? [],
      items: this.createdItems()
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = fileName; a.click();
    URL.revokeObjectURL(url);
  }

  // ===== CONFIGURACIÓN DE VALIDACIÓN =====
private readonly validationConfig: FileValidationConfig = {
  maxRecords: 100,  // Máximo 100 jugadores por archivo
  maxFileSize: 5 * 1024 * 1024,  // 5MB
  requiredFields: ['name', 'position', 'nflTeamID'],
  allowedImageDomains: [
    'imgur.com',
    'cloudinary.com',
    'amazonaws.com',
    's3.amazonaws.com',
    'googleusercontent.com',
    'cdn.com',
    'localhost',
    'placehold.co',
    '127.0.0.1'
  ]
};

  // ===== VALIDACIONES DE SEGURIDAD =====
  private validateFileSize(file: File): ValidationResult {
    console.log('🔒 [SEGURIDAD] Validando tamaño del archivo...');
    console.log(`📏 Tamaño: ${file.size} bytes (Máximo: ${this.validationConfig.maxFileSize} bytes)`);
    
    if (file.size > this.validationConfig.maxFileSize) {
      return {
        valid: false,
        errors: [`El archivo es demasiado grande (${(file.size / 1024 / 1024).toFixed(2)}MB). Máximo permitido: ${(this.validationConfig.maxFileSize / 1024 / 1024).toFixed(2)}MB`]
      };
    }
    
    console.log('✅ [SEGURIDAD] Tamaño del archivo válido');
    return { valid: true, errors: [] };
  }

  private validateFileContent(text: string): ValidationResult {
    console.log('🔒 [SEGURIDAD] Validando contenido del archivo...');
    const errors: string[] = [];
    
    // Validar caracteres sospechosos
    const suspiciousPatterns = [
      /<script/gi,
      /javascript:/gi,
      /on\w+\s*=/gi,  // onclick=, onload=, etc.
      /<iframe/gi,
      /eval\(/gi,
      /document\./gi,
      /window\./gi
    ];
    
    for (const pattern of suspiciousPatterns) {
      if (pattern.test(text)) {
        errors.push(`El archivo contiene contenido sospechoso: ${pattern.toString()}`);
      }
    }
    
    if (errors.length > 0) {
      console.error('❌ [SEGURIDAD] Contenido sospechoso detectado:', errors);
      return { valid: false, errors };
    }
    
    console.log('✅ [SEGURIDAD] Contenido del archivo válido');
    return { valid: true, errors: [] };
  }

  private validateRecordCount(records: RawRecord[]): ValidationResult {
    console.log('🔒 [SEGURIDAD] Validando cantidad de registros...');
    console.log(`📊 Registros encontrados: ${records.length} (Máximo: ${this.validationConfig.maxRecords})`);
    
    if (records.length === 0) {
      return { valid: false, errors: ['El archivo no contiene registros válidos'] };
    }
    
    if (records.length > this.validationConfig.maxRecords) {
      return {
        valid: false,
        errors: [`El archivo contiene ${records.length} registros. Máximo permitido: ${this.validationConfig.maxRecords}`]
      };
    }
    
    console.log('✅ [SEGURIDAD] Cantidad de registros válida');
    return { valid: true, errors: [] };
  }

  private validateImageUrl(url: string): ValidationResult {
    console.log('🔒 [SEGURIDAD] Validando URL de imagen:', url);
    const errors: string[] = [];
    
    // Validar que sea una URL válida
    try {
      const urlObj = new URL(url);
      
      // Validar protocolo
      if (!['http:', 'https:'].includes(urlObj.protocol)) {
        errors.push(`Protocolo no permitido: ${urlObj.protocol}. Solo se permiten http: y https:`);
      }
      
      // Validar dominio
      const hostname = urlObj.hostname.toLowerCase();
      const isDomainAllowed = this.validationConfig.allowedImageDomains.some(domain => 
        hostname === domain || hostname.endsWith('.' + domain)
      );
      
      if (!isDomainAllowed) {
        errors.push(`Dominio no permitido: ${hostname}. Dominios permitidos: ${this.validationConfig.allowedImageDomains.join(', ')}`);
      }
      
      // Validar extensión de imagen
      const validExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp', ''];
      const pathname = urlObj.pathname.toLowerCase();
      const hasValidExtension = validExtensions.some(ext => pathname.endsWith(ext));
      
      if (!hasValidExtension && !pathname.includes('image')) {
        errors.push(`La URL no parece ser una imagen válida. Extensiones permitidas: ${validExtensions.join(', ')}`);
      }
      
    } catch (e) {
      errors.push(`URL inválida: ${url}`);
    }
    
    if (errors.length > 0) {
      console.error('❌ [SEGURIDAD] URL inválida:', errors);
      return { valid: false, errors };
    }
    
    console.log('✅ [SEGURIDAD] URL válida');
    return { valid: true, errors: [] };
  }

  private sanitizeString(value: string, maxLength: number = 100): string {
    if (!value) return '';
    
    // Remover caracteres de control y espacios múltiples
    let sanitized = value
      .replace(/[\x00-\x1F\x7F]/g, '') // Caracteres de control
      .replace(/\s+/g, ' ') // Espacios múltiples
      .trim();
    
    // Limitar longitud
    if (sanitized.length > maxLength) {
      sanitized = sanitized.substring(0, maxLength);
    }
    
    return sanitized;
  }

  private validatePlayerData(record: ParsedRecord): ValidationResult {
    console.log(`🔒 [SEGURIDAD] Validando datos del jugador (Fila ${record.row})...`);
    const errors: string[] = [];
    
    // Validar nombre
    if (!record.name || record.name.toString().trim().length === 0) {
      errors.push(`[Fila ${record.row}] El nombre es requerido`);
    } else {
      const name = record.name.toString().trim();
      
      // Validar longitud del nombre
      if (name.length < 2) {
        errors.push(`[Fila ${record.row}] El nombre debe tener al menos 2 caracteres`);
      }
      if (name.length > 100) {
        errors.push(`[Fila ${record.row}] El nombre no puede tener más de 100 caracteres`);
      }
      
      // Validar caracteres permitidos en el nombre (letras, espacios, guiones, apóstrofes)
      if (!/^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$/.test(name)) {
        errors.push(`[Fila ${record.row}] El nombre contiene caracteres no permitidos. Solo se permiten letras, espacios, guiones y apóstrofes`);
      }
      
      // Validar que tenga nombre y apellido
      const parts = name.split(' ').filter(x => x.length > 0);
      if (parts.length < 2) {
        errors.push(`[Fila ${record.row}] El nombre debe incluir nombre y apellido`);
      }
    }
    
    // Validar posición
    if (!record.position || record.position.toString().trim().length === 0) {
      errors.push(`[Fila ${record.row}] La posición es requerida`);
    } else {
      const pos = record.position.toString().trim().toUpperCase();
      if (!this.allowedPositions.has(pos)) {
        errors.push(`[Fila ${record.row}] Posición inválida: "${pos}". Posiciones permitidas: ${Array.from(this.allowedPositions).join(', ')}`);
      }
    }
    
    // Validar NFLTeamID
    if (!record.teamInput) {
      errors.push(`[Fila ${record.row}] El equipo NFL es requerido`);
    } else {
      const teamInput = record.teamInput.toString().trim();
      
      // Si es un número, validar que sea positivo
      if (!isNaN(Number(teamInput))) {
        const teamId = Number(teamInput);
        if (teamId <= 0) {
          errors.push(`[Fila ${record.row}] El ID del equipo NFL debe ser un número positivo`);
        }
        if (teamId > 1000) {
          errors.push(`[Fila ${record.row}] El ID del equipo NFL parece inválido (${teamId})`);
        }
      }
    }
    
    // Validar URL de imagen (si existe)
    if (record.image && record.image.toString().trim().length > 0) {
      const imageUrl = record.image.toString().trim();
      const urlValidation = this.validateImageUrl(imageUrl);
      if (!urlValidation.valid) {
        errors.push(...urlValidation.errors.map(e => `[Fila ${record.row}] ${e}`));
      }
    }
    
    if (errors.length > 0) {
      console.error(`❌ [SEGURIDAD] Datos inválidos en fila ${record.row}:`, errors);
      return { valid: false, errors };
    }
    
    console.log(`✅ [SEGURIDAD] Datos válidos en fila ${record.row}`);
    return { valid: true, errors: [] };
  }
}
