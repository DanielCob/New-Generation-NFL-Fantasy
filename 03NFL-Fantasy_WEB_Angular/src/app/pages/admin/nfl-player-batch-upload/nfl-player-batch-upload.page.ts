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
import { CreateNFLPlayerDTO } from '../../../core/models/nfl-player-model';

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
  image?: string;
  thumbnail?: string;
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

  loading = signal(false);
  checking = signal(false);
  uploading = signal(false);

  teams = signal<NFLTeamBasic[]>([]);
  allowedPositions = new Set(['QB','RB','WR','TE','K','DEF']);

  // Players batch
  fileName = signal<string>('');
  parsed = signal<ParsedRecord[]>([]);
  valid = signal<ValidRecord[]>([]);
  errors = signal<string[]>([]);
  existingConflicts = signal<string[]>([]);

  report = signal<{ created: number; failed: number; errors: string[] } | null>(null);

  ngOnInit(): void {
    this.refreshTeams();
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

  onFileSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) return;
    this.fileName.set(file.name);
    this.report.set(null);

    const reader = new FileReader();
    reader.onload = () => {
      const text = String(reader.result || '');
      let records: RawRecord[] = [];
      if (file.name.toLowerCase().endsWith('.csv')) {
        records = this.parseCSV(text);
      } else if (file.name.toLowerCase().endsWith('.json')) {
        try {
          const data = JSON.parse(text);
          if (Array.isArray(data)) records = data as RawRecord[];
          else if (Array.isArray((data as any).data)) records = (data as any).data as RawRecord[];
          else records = [];
        } catch {
          records = [];
        }
      } else {
        this.snack.open('Formato no soportado. Usa .csv o .json', 'OK', { duration: 3000 });
        return;
      }

      // Normalize
      const parsed = records.map((r, i) => this.normalizeRecord(r, i+1));
      this.parsed.set(parsed);
      this.validateAll();
    };
    reader.readAsText(file);
  }

  private parseCSV(text: string): RawRecord[] {
    const lines = text.split(/\r?\n/).filter(l => l.trim().length > 0);
    if (lines.length === 0) return [];
    const header = lines[0].split(',').map(h => h.trim());
    const out: RawRecord[] = [];
    for (let i=1;i<lines.length;i++) {
      const row = this.splitCSVLine(lines[i]);
      const obj: RawRecord = {}; obj.__row = i+1;
      header.forEach((h, idx) => obj[h] = row[idx]);
      out.push(obj);
    }
    return out;
  }
  private splitCSVLine(line: string): string[] {
    const res: string[] = [];
    let cur = ''; let inQuotes = false;
    for (let i=0;i<line.length;i++) {
      const ch = line[i];
      if (ch === '"') { inQuotes = !inQuotes; continue; }
      if (ch === ',' && !inQuotes) { res.push(cur.trim()); cur = ''; }
      else { cur += ch; }
    }
    res.push(cur.trim());
    return res;
  }

  private normalizeRecord(r: RawRecord, row: number): ParsedRecord {
    const pick = (keys: string[]): any => {
      for (const k of keys) if (r[k] !== undefined && r[k] !== null && String(r[k]).trim() !== '') return r[k];
      return undefined;
    };
    const extId = pick(['ID','Id','id','ExternalID','ExternalId']);
    const name = pick(['Name','Nombre','FullName','Fullname','fullName']);
    const position = pick(['Position','Posicion','Posición','pos','POS']);
    const teamInput = pick(['NFLTeamID','TeamID','Team','Equipo','EquipoNFL','NFLTeam','nflTeam']);
    const image = pick(['Image','Imagen','PhotoUrl','ImageUrl','image']);
    return { row, source: r, extId, name, position, teamInput, image };
  }

  private async buildThumbnail(url?: string): Promise<string | undefined> {
    if (!url) return undefined;
    try {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      const p = new Promise<HTMLImageElement>((resolve, reject) => {
        img.onload = () => resolve(img);
        img.onerror = reject;
      });
      img.src = url;
      const loaded = await p;
      const size = 96;
      const canvas = document.createElement('canvas');
      canvas.width = size; canvas.height = size;
      const ctx = canvas.getContext('2d');
      if (!ctx) return url;
      ctx.drawImage(loaded, 0, 0, size, size);
      return canvas.toDataURL('image/png');
    } catch {
      // fallback: reuse original url
      return url;
    }
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

      // split into first/last
      let firstName = ''; let lastName = '';
      if (name) {
        const parts = name.split(' ').filter(x => x);
        if (parts.length >= 2) { lastName = parts.pop()!; firstName = parts.join(' '); }
        else { errsForRow.push(`[Fila ${rec.row}] Nombre debe incluir nombre y apellido`); }
      }

      const dupKey = `${name.toLowerCase()}|${teamId ?? 'X'}`;
      if (localDupSet.has(dupKey)) {
        if (!localDupSeen.has(dupKey)) {
          errors.push(`[Duplicado en archivo] ${name} - Equipo ${teamId}`);
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
          nflTeamID: teamId!,
          thumbnail: undefined
        });
      } else {
        errors.push(...errsForRow);
      }
    }

    // generate thumbnails in parallel (best-effort)
    await Promise.all(valids.map(async v => { v.thumbnail = await this.buildThumbnail(v.image); }));

    this.valid.set(valids);
    this.errors.set(errors);

    // Check existing duplicates via API
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
          const exists = players.some(p => (p.FullName || (`${p.FirstName} ${p.LastName}`)).toLowerCase() === (v.firstName + ' ' + v.lastName).toLowerCase() && Number(p.NFLTeamID) === v.nflTeamID);
          if (exists) conflicts.push(`[Ya existe] ${v.firstName} ${v.lastName} en equipo ${v.nflTeamID}`);
        }
      } catch {
        // ignore individual check errors to avoid blocking
      }
    }

    this.existingConflicts.set(conflicts);
    this.checking.set(false);
  }

  canUpload(): boolean {
    return this.parsed().length > 0 && this.errors().length === 0 && this.existingConflicts().length === 0 && !this.uploading();
  }

  async uploadAll(): Promise<void> {
    if (!this.canUpload()) return;
    this.uploading.set(true);
    let playerCreated = 0;
    const playerFailed: string[] = [];

    // Create players
    for (const v of this.valid()) {
      const dto: CreateNFLPlayerDTO = {
        FirstName: v.firstName,
        LastName: v.lastName,
        Position: v.position,
        NFLTeamID: v.nflTeamID,
        ...(v.image ? { PhotoUrl: v.image } : {}),
        ...(v.thumbnail ? { ThumbnailUrl: v.thumbnail } : {})
      };
      try {
        const res: any = await new Promise((resolve, reject) => {
          this.players.create(dto).subscribe({ next: resolve, error: reject });
        });
        const ok = (res?.success ?? res?.Success) !== false;
        if (ok) {
          playerCreated++;
        } else {
          const errMsg = res?.message ?? res?.Message ?? 'Error desconocido al crear el jugador';
          playerFailed.push(`[Fila ${v.row}] ${v.firstName} ${v.lastName}: ${errMsg}`);
          break;
        }
      } catch (err: any) {
        const errMsg = err?.error?.message ?? err?.error?.Message ?? 'Error de conexión con el servidor';
        playerFailed.push(`[Fila ${v.row}] ${v.firstName} ${v.lastName}: ${errMsg}`);
        break;
      }
    }

    if (playerCreated !== this.valid().length) {
      this.snack.open('Se detectaron errores. No se creó el lote completo.', 'OK', { duration: 4000 });
    } else {
      this.snack.open(`Se crearon ${playerCreated} jugadores exitosamente`, 'OK', { duration: 3000 });
    }

    this.report.set({ created: playerCreated, failed: this.valid().length - playerCreated, errors: playerFailed });
    this.uploading.set(false);
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
      items: this.valid().map(v => ({
        ExternalID: v.extId ?? null,
        FullName: v.firstName + ' ' + v.lastName,
        Position: v.position,
        NFLTeamID: v.nflTeamID,
        PhotoUrl: v.image ?? null,
        ThumbnailUrl: v.thumbnail ?? null
      }))
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = fileName; a.click();
    URL.revokeObjectURL(url);
  }
}
