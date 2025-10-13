import { Pipe, PipeTransform } from '@angular/core';

/**
 * Retorna la clase CSS para el badge de lesión
 * Uso: <span [class]="player.injuryStatus | injuryStatusClass">{{ player.injuryStatus }}</span>
 */
@Pipe({
  name: 'injuryStatusClass',
  standalone: true
})
export class InjuryStatusClassPipe implements PipeTransform {
  transform(status?: string): string {
    if (!status || status === 'Healthy') return 'status-healthy';
    if (status === 'Questionable') return 'status-questionable';
    if (status === 'Doubtful') return 'status-doubtful';
    if (status === 'Out' || status === 'IR') return 'status-out';
    return 'status-unknown';
  }
}
