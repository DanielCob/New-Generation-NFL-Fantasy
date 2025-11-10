// core/services/authz/role.service.ts
import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RoleService {
  /** Extrae el “rol” desde headers/perfil, tolerando distintos casings y envoltorios. */
  fromHeader(payload: any): unknown {
    const p = payload?.data ?? payload?.Data ?? payload ?? {};
    return p.Role ?? p.SystemRoleCode ?? p.role ?? p.systemRoleCode ?? null;
  }

  /** True si el usuario es admin. Soporta string/number y un flag directo IsAdmin. */
  isAdminRole(role: unknown, payload?: any): boolean {
    if (environment.enableAdmin === true) return true;

    // Flag directo que a veces expone el backend
    const p = payload?.data ?? payload?.Data ?? payload ?? {};
    if (p.IsAdmin === true || p.isAdmin === true) return true;

    if (role == null) return false;

    if (typeof role === 'number') return role === 1;

    if (typeof role === 'string') {
      const r = role.toUpperCase();
      return r === 'ADMIN' || r === 'SYSTEM_ADMIN' || r === 'SYS_ADMIN';
    }

    return false;
  }
}
