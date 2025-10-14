import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';

export interface TableColumn<T = any> {
  /** Propiedad de la fila a mostrar (p.ej. 'LeagueName') */
  key: keyof T | string;
  /** Encabezado visible */
  label: string;
  /** Formatos básicos soportados por la vista */
  format?: 'date' | 'yesno';
  /** Formato de fecha (Angular DatePipe), si aplica */
  dateFormat?: string;
  /** Formateador custom: si existe, tiene prioridad sobre 'format' */
  formatter?: (row: T) => any;
}

@Component({
  selector: 'app-table-simple',
  standalone: true,
  imports: [CommonModule, MatTableModule],
  templateUrl: './table-simple.html',
  styleUrl: './table-simple.css'
})
export class TableSimple {
  /** Filas a mostrar */
  @Input() data: any[] = [];

  /** Definición de columnas */
  @Input() columns: TableColumn[] = [];

  /** Mensaje cuando no hay filas */
  @Input() emptyMessage = 'No records found.';

  /**
   * trackBy opcional. Si no viene, usamos el índice.
   * Esto mantiene buen rendimiento en listas largas.
   */
  @Input() trackBy: (index: number, row: any) => any = (i) => i;

  /** Claves de columnas que usa MatTable en header/row defs */
  get displayedColumns(): string[] {
    // Usamos el helper para garantizar string y evitar duplicar lógica
    return this.columns.map((c) => this.columnKey(c));
  }

  /** Helper: valor crudo de una celda (si no hay formatter) */
  cellValue(row: any, col: TableColumn): any {
    return (row && col && col.key != null) ? row[col.key as any] : null;
  }

  /**
   * Helper: clave string de columna para [matColumnDef].
   * (Evita usar `String(...)` en el template, que rompe con TS estricto)
   */
  columnKey(col: TableColumn): string {
    return String(col?.key ?? '');
  }
}

/**
 * Re-export de tipo con 'export type' para cumplir con isolatedModules.
 * Si quieres importar el alias: `import type { TableSimpleColumn } from ...`
 */
export type { TableColumn as TableSimpleColumn };
