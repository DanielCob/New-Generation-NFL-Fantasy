import { Pipe, PipeTransform } from '@angular/core';

/**
 * Retorna el icono para tipo de adquisición
 */
@Pipe({
  name: 'acquisitionIcon',
  standalone: true
})
export class AcquisitionIconPipe implements PipeTransform {
  transform(type: string): string {
    const icons: { [key: string]: string } = {
      'Draft': 'how_to_reg',
      'Trade': 'swap_horiz',
      'FreeAgent': 'person_add',
      'Waiver': 'priority_high'
    };

    return icons[type] || 'help_outline';
  }
}
