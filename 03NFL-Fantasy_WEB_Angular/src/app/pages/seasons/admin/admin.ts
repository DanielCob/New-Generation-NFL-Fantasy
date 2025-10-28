import { Component, OnInit, OnDestroy, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatSliderModule } from '@angular/material/slider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subscription } from 'rxjs';
import { SeasonService } from '../../../core/services/season-service';
import { Season } from '../../../core/models/season-model';

@Component({
  selector: 'app-seasons-admin',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSnackBarModule,
    MatChipsModule,
    MatSliderModule,
    MatSlideToggleModule,
    MatTabsModule,
    MatTableModule,
    MatDialogModule,
    MatTooltipModule
  ],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class SeasonsAdminComponent implements OnInit, OnDestroy {
  private seasons = inject(SeasonService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  loading = signal(true);
  error = signal<string | null>(null);
  current = signal<Season | null>(null);
  canDeactivate = computed(() => {
    const s = this.current();
    if (!s) return false;
    if (s.IsCurrent) return false;
    const sd = this.atStartOfDay(new Date(s.StartDate));
    const ed = this.atStartOfDay(new Date(s.EndDate));
    return sd.getTime() <= this.today.getTime() && ed.getTime() <= this.today.getTime();
  });
  canDelete = computed(() => {
    const s = this.current();
    if (!s) return false;
    return !s.IsCurrent; // backend will validate active leagues linkage
  });

  // Create form
  createForm = this.fb.group({
    Name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
    WeekCount: [18, [Validators.required, Validators.min(1), Validators.max(30)]],
    StartDate: [null as Date | null, [Validators.required]],
    EndDate: [null as Date | null, [Validators.required]],
    MarkAsCurrent: [false],
    AutoWeekCount: [true]
  }, { validators: [this.validateDateOrder('StartDate', 'EndDate'), this.validateNotInPast('StartDate', 'EndDate')] });

  // Edit form (for current season)
  editForm = this.fb.group({
    Name: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
    WeekCount: [18, [Validators.required, Validators.min(1), Validators.max(30)]],
    StartDate: [null as Date | null, [Validators.required]],
    EndDate: [null as Date | null, [Validators.required]],
    SetAsCurrent: [false],
    ConfirmMakeCurrent: [false],
    AutoWeekCount: [true]
  }, { validators: [this.validateDateOrder('StartDate', 'EndDate'), this.validateNotInPast('StartDate', 'EndDate')] });

  // Weeks (UI-only validation)
  createWeeks = signal<{ index: number; start: Date; end: Date; }[]>([]);
  editWeeks = signal<{ index: number; start: Date; end: Date; }[]>([]);
  // datepicker helpers
  today = this.atStartOfDay(new Date());
  startFilter = (d: Date | null) => {
    if (!d) return false;
    return this.atStartOfDay(d).getTime() >= this.today.getTime();
  };
  endFilterEdit = (d: Date | null) => {
    if (!d) return false;
    const st = this.editForm.controls.StartDate.value ?? this.today;
    return this.atStartOfDay(d).getTime() > this.atStartOfDay(st).getTime();
  };
  endFilterCreate = (d: Date | null) => {
    if (!d) return false;
    const st = this.createForm.controls.StartDate.value ?? this.today;
    return this.atStartOfDay(d).getTime() > this.atStartOfDay(st).getTime();
  };

  ngOnInit(): void {
    this.refresh();
    // Reactive syncing for create form
    this.subs.add(this.createForm.valueChanges.subscribe(() => {
      const s = this.createForm.controls.StartDate.value;
      const e = this.createForm.controls.EndDate.value;
      const c = this.createForm.controls.WeekCount.value ?? 0;
      const auto = !!this.createForm.controls.AutoWeekCount.value;
      if (s && e) {
        if (auto) {
          const weeks = this.calcWeeksFromDates(s, e);
          if (weeks !== c) this.createForm.controls.WeekCount.setValue(weeks, { emitEvent: false });
        } else if (c > 0) {
          const newEnd = this.calcEndFromWeeks(s, c);
          if (!e || this.atStartOfDay(e).getTime() !== newEnd.getTime()) {
            this.createForm.controls.EndDate.setValue(newEnd, { emitEvent: false });
          }
        }
      }
      // update weeks preview
      if (s && (this.createForm.controls.EndDate.value) && (this.createForm.controls.WeekCount.value)) {
        this.createWeeks.set(this.generateWeeks(s, this.createForm.controls.EndDate.value!, this.createForm.controls.WeekCount.value!));
      } else {
        this.createWeeks.set([]);
      }
    }));

    // Reactive syncing for edit form
    this.subs.add(this.editForm.valueChanges.subscribe(() => {
      const s = this.editForm.controls.StartDate.value;
      const e = this.editForm.controls.EndDate.value;
      const c = this.editForm.controls.WeekCount.value ?? 0;
      const auto = !!this.editForm.controls.AutoWeekCount.value;
      if (s && e) {
        if (auto) {
          const weeks = this.calcWeeksFromDates(s, e);
          if (weeks !== c) this.editForm.controls.WeekCount.setValue(weeks, { emitEvent: false });
        } else if (c > 0) {
          const newEnd = this.calcEndFromWeeks(s, c);
          if (!e || this.atStartOfDay(e).getTime() !== newEnd.getTime()) {
            this.editForm.controls.EndDate.setValue(newEnd, { emitEvent: false });
          }
        }
      }
      // update weeks preview
      if (s && (this.editForm.controls.EndDate.value) && (this.editForm.controls.WeekCount.value)) {
        this.editWeeks.set(this.generateWeeks(s, this.editForm.controls.EndDate.value!, this.editForm.controls.WeekCount.value!));
      } else {
        this.editWeeks.set([]);
      }
    }));
  }

  private subs = new Subscription();
  ngOnDestroy(): void { this.subs.unsubscribe(); }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.seasons.getCurrent().subscribe({
      next: (s) => {
        this.current.set(s);
        this.loading.set(false);
        // populate edit form
        const sd = new Date(s.StartDate);
        const ed = new Date(s.EndDate);
        this.editForm.patchValue({
          Name: s.Label ?? '',
          WeekCount: (s as any).WeekCount ?? 18,
          StartDate: sd,
          EndDate: ed,
          SetAsCurrent: !!s.IsCurrent,
          ConfirmMakeCurrent: false
        });
        this.editWeeks.set(this.generateWeeks(sd, ed, (this.editForm.controls.WeekCount.value ?? 18)));
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudo obtener la temporada actual');
      }
    });
  }

  // ---------- Validation helpers ----------
  private atStartOfDay(d: Date): Date { const dd = new Date(d); dd.setHours(0,0,0,0); return dd; }
  private calcWeeksFromDates(start: Date, end: Date): number {
    const ms = this.atStartOfDay(end).getTime() - this.atStartOfDay(start).getTime();
    const days = Math.floor(ms / (24*60*60*1000)) + 1; // inclusive
    return Math.max(1, Math.min(30, Math.ceil(days / 7)));
  }
  private calcEndFromWeeks(start: Date, weeks: number): Date {
    const s = this.atStartOfDay(start).getTime();
    const end = new Date(s + (weeks * 7 * 24 * 60 * 60 * 1000) - 1);
    return this.atStartOfDay(end);
  }

  private validateDateOrder(startKey: string, endKey: string) {
    return (ctrl: AbstractControl) => {
      const s = ctrl.get(startKey)?.value as Date | null;
      const e = ctrl.get(endKey)?.value as Date | null;
      if (!s || !e) return null;
      return e.getTime() > s.getTime() ? null : { dateOrder: 'EndDate must be after StartDate' };
    };
  }

  private validateNotInPast(startKey: string, endKey: string) {
    return (ctrl: AbstractControl) => {
      const s = ctrl.get(startKey)?.value as Date | null;
      const e = ctrl.get(endKey)?.value as Date | null;
      if (!s || !e) return null;
      const today = this.atStartOfDay(new Date()).getTime();
      if (this.atStartOfDay(s).getTime() < today) return { notInPast: 'StartDate cannot be in the past' };
      if (this.atStartOfDay(e).getTime() < today) return { notInPast: 'EndDate cannot be in the past' };
      return null;
    };
  }

  private weeksValid(weeks: {start: Date; end: Date;}[], seasonStart: Date, seasonEnd: Date): string | null {
    const s0 = seasonStart.getTime();
    const e0 = seasonEnd.getTime();
    for (let i=0;i<weeks.length;i++){
      const w = weeks[i];
      const ws = w.start.getTime();
      const we = w.end.getTime();
      if (we <= ws) return `Week ${i+1}: end must be after start`;
      if (ws < s0 || we > e0) return `Week ${i+1}: must be within season range`;
      for (let j=i+1;j<weeks.length;j++){
        const w2 = weeks[j];
        const ws2 = w2.start.getTime();
        const we2 = w2.end.getTime();
        const overlap = Math.max(ws, ws2) < Math.min(we, we2);
        if (overlap) return `Week ${i+1} overlaps week ${j+1}`;
      }
    }
    return null;
  }

  private generateWeeks(start: Date, end: Date, count: number): { index: number; start: Date; end: Date; }[] {
    const s = this.atStartOfDay(start);
    const e = this.atStartOfDay(end);
    const totalMs = e.getTime() - s.getTime();
    if (count <= 0 || totalMs <= 0) return [];
    const weekMs = Math.floor(totalMs / count);
    const weeks: { index: number; start: Date; end: Date; }[] = [];
    let cur = new Date(s);
    for (let i=0;i<count;i++){
      const next = i === count-1 ? new Date(e) : new Date(cur.getTime() + weekMs);
      const wStart = new Date(cur);
      const wEnd = new Date(next);
      if (i < count-1) wEnd.setMilliseconds(wEnd.getMilliseconds() - 1);
      weeks.push({ index: i+1, start: wStart, end: wEnd });
      cur = new Date(next);
    }
    return weeks;
  }

  // ---------- Actions ----------
  createSeason(): void {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    const { Name, WeekCount, StartDate, EndDate, MarkAsCurrent } = this.createForm.getRawValue();
    const weeks = this.createWeeks();
    const weeksError = this.weeksValid(weeks, StartDate!, EndDate!);
    if (weeksError) { this.snack.open(weeksError, 'OK', { duration: 4000 }); return; }

    let mark = !!MarkAsCurrent;
    if (mark) {
      const go = window.confirm('Solo una temporada puede estar marcada como actual. ¿Deseas continuar?');
      if (!go) return;
    }
    this.seasons.createSeason({
      Name: Name!,
      WeekCount: Number(WeekCount!),
      StartDate: new Date(StartDate!).toISOString(),
      EndDate: new Date(EndDate!).toISOString(),
      MarkAsCurrent: mark
    }).subscribe({
      next: (r: any) => {
        const ok = (r?.success ?? r?.Success) !== false;
        if (ok) {
          this.snack.open('Temporada creada', 'OK', { duration: 3000 });
          this.refresh();
        } else {
          const msg = r?.message ?? r?.Message ?? 'No se pudo crear la temporada';
          this.snack.open(msg, 'OK', { duration: 4000 });
        }
      },
      error: (err) => {
        const msg = err?.error?.message ?? err?.error?.Message ?? 'Error de red o validación (nombre duplicado/traslape)';
        this.snack.open(msg, 'OK', { duration: 5000 });
      }
    });
  }

  saveEdits(): void {
    const cur = this.current();
    if (!cur) { this.snack.open('No hay temporada para editar', 'OK', { duration: 3000 }); return; }
    if (this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }
    const { Name, WeekCount, StartDate, EndDate, SetAsCurrent, ConfirmMakeCurrent } = this.editForm.getRawValue();
    const weeks = this.editWeeks();
    const weeksError = this.weeksValid(weeks, StartDate!, EndDate!);
    if (weeksError) { this.snack.open(weeksError, 'OK', { duration: 4000 }); return; }

    let setCurrent = !!SetAsCurrent;
    let confirmCurrent = !!ConfirmMakeCurrent;
    if (setCurrent && !confirmCurrent) {
      const go = window.confirm('Cambiar la temporada actual desmarcará la anterior. ¿Confirmas la operación?');
      if (!go) return; else confirmCurrent = true;
    }

    this.seasons.updateSeason(cur.SeasonID, {
      Name: Name!,
      WeekCount: Number(WeekCount!),
      StartDate: new Date(StartDate!).toISOString(),
      EndDate: new Date(EndDate!).toISOString(),
      SetAsCurrent: setCurrent,
      ConfirmMakeCurrent: confirmCurrent
    }).subscribe({
      next: (r: any) => {
        const ok = (r?.success ?? r?.Success) !== false;
        if (ok) {
          this.snack.open('Temporada actualizada', 'OK', { duration: 3000 });
          this.refresh();
        } else {
          const msg = r?.message ?? r?.Message ?? 'No se pudo actualizar la temporada';
          this.snack.open(msg, 'OK', { duration: 4000 });
        }
      },
      error: (err) => {
        const msg = err?.error?.message ?? err?.error?.Message ?? 'Error de red o validación (traslape de fechas)';
        this.snack.open(msg, 'OK', { duration: 5000 });
      }
    });
  }

  // -------- Deactivate & Delete --------
  deactivate(): void {
    const s = this.current();
    if (!s) return;
    if (!this.canDeactivate()) {
      this.snack.open('No se puede desactivar: debe NO ser actual y no tener fechas futuras.', 'OK', { duration: 4000 });
      return;
    }
    const ok = window.confirm('¿Confirmas desactivar esta temporada? Pasará a históricos y dejará de ser visible para usuarios finales.');
    if (!ok) return;
    this.seasons.deactivateSeason(s.SeasonID).subscribe({
      next: (r: any) => {
        const success = (r?.success ?? r?.Success) !== false;
        if (success) {
          this.snack.open('Temporada desactivada', 'OK', { duration: 3000 });
          this.refresh();
        } else {
          const msg = r?.message ?? r?.Message ?? 'No se pudo desactivar la temporada';
          this.snack.open(msg, 'OK', { duration: 5000 });
        }
      },
      error: (err) => {
        const msg = err?.error?.message ?? err?.error?.Message ?? 'Operación rechazada (puede ser actual o con fechas futuras)';
        this.snack.open(msg, 'OK', { duration: 5000 });
      }
    });
  }

  delete(): void {
    const s = this.current();
    if (!s) return;
    if (s.IsCurrent) {
      this.snack.open('No se puede eliminar una temporada marcada como actual.', 'OK', { duration: 4000 });
      return;
    }
    const ok = window.confirm('Esta acción eliminará la temporada. Debe no tener ligas activas asociadas. ¿Confirmas?');
    if (!ok) return;
    this.seasons.deleteSeason(s.SeasonID).subscribe({
      next: (r: any) => {
        const success = (r?.success ?? r?.Success) !== false;
        if (success) {
          this.snack.open('Temporada eliminada', 'OK', { duration: 3000 });
          this.current.set(null);
          this.editForm.reset();
        } else {
          const msg = r?.message ?? r?.Message ?? 'No se pudo eliminar la temporada';
          this.snack.open(msg, 'OK', { duration: 5000 });
        }
      },
      error: (err) => {
        const msg = err?.error?.message ?? err?.error?.Message ?? 'Bloqueada: hay ligas activas o es temporada actual';
        this.snack.open(msg, 'OK', { duration: 5000 });
      }
    });
  }

  // -------- Historical table --------
  allSeasons = signal<Season[]>([]);
  historyLoading = signal(false);
  historyError = signal<string | null>(null);
  displayedColumns: string[] = ['label','year','start','end','actions'];

  history = computed(() => {
    const todayMs = this.today.getTime();
    return (this.allSeasons() || []).filter(s => !s.IsCurrent && new Date(s.EndDate).getTime() <= todayMs);
  });

  historySeasonId = signal<number | null>(null);
  loadHistoricalById(): void {
    const id = this.historySeasonId();
    if (!id || id <= 0) { this.snack.open('Ingresa un ID de temporada válido', 'OK', { duration: 2500 }); return; }
    this.historyLoading.set(true);
    this.historyError.set(null);
    this.seasons.getSeason(id).subscribe({
      next: (r: any) => {
        const s: Season | null = (r?.data ?? r?.Data ?? null) as Season | null;
        this.historyLoading.set(false);
        if (!s) { this.snack.open('No se encontró la temporada', 'OK', { duration: 2500 }); return; }
        const isHistorical = !s.IsCurrent && new Date(s.EndDate).getTime() <= this.today.getTime();
        if (!isHistorical) { this.snack.open('La temporada no es histórica (aún vigente o actual).', 'OK', { duration: 3000 }); return; }
        const existing = this.allSeasons().some(x => x.SeasonID === s.SeasonID);
        if (!existing) this.allSeasons.set([s, ...this.allSeasons()]);
      },
      error: () => {
        this.historyLoading.set(false);
        this.snack.open('Error al buscar la temporada', 'OK', { duration: 3000 });
      }
    });
  }
  clearHistoryList(): void { this.allSeasons.set([]); }

  openWeeksDialog(s: Season): void {
    this.seasons.getWeeks(s.SeasonID).subscribe({
      next: (r: any) => {
        const weeks = Array.isArray(r) ? r : (r?.data ?? r?.Data ?? []);
        import('./season-weeks-dialog').then(m => {
          this.dialog.open(m.SeasonWeeksDialog, { data: { seasonLabel: s.Label, weeks } });
        });
      },
      error: () => this.snack.open('No se pudieron cargar las semanas', 'OK', { duration: 3000 })
    });
  }

  deleteHistorical(s: Season): void {
    if (s.IsCurrent) {
      this.snack.open('No se puede eliminar una temporada marcada como actual.', 'OK', { duration: 3000 });
      return;
    }
    const ok = window.confirm(`¿Eliminar la temporada "${s.Label}"? Debe no tener ligas activas asociadas.`);
    if (!ok) return;
    this.seasons.deleteSeason(s.SeasonID).subscribe({
      next: (r: any) => {
        const success = (r?.success ?? r?.Success) !== false;
        if (success) {
          this.snack.open('Temporada eliminada', 'OK', { duration: 2000 });
          this.allSeasons.set(this.allSeasons().filter(x => x.SeasonID !== s.SeasonID));
        } else {
          const msg = r?.message ?? r?.Message ?? 'No se pudo eliminar la temporada';
          this.snack.open(msg, 'OK', { duration: 4000 });
        }
      },
      error: () => this.snack.open('No se pudo eliminar', 'OK', { duration: 3000 })
    });
  }
}
