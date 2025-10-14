import { Pipe, PipeTransform } from '@angular/core';

/**
 * Retorna el icono Material para cada posición NFL
 * Uso: {{ 'QB' | positionIcon }}
 */
@Pipe({
  name: 'positionIcon',
  standalone: true
})
export class PositionIconPipe implements PipeTransform {
  transform(position: string): string {
    const icons: { [key: string]: string } = {
      'QB': 'sports_football',
      'RB': 'directions_run',
      'WR': 'airline_stops',
      'TE': 'sports_kabaddi',
      'K': 'sports_soccer',
      'DEF': 'shield'
    };

    return icons[position] || 'person';
  }
}
