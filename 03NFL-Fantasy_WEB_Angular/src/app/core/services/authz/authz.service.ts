// core/services/authz/authz.service.ts
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../auth-service';
import { UserService } from '../user-service';
import { RoleService } from './role.service';            // <-- TU servicio
import { BehaviorSubject, defer, of } from 'rxjs';
import { catchError, map, shareReplay, switchMap } from 'rxjs/operators';

function pickData(x: any) { return x?.data ?? x?.Data ?? x ?? {}; }

@Injectable({ providedIn: 'root' })
export class AuthzService {
  private auth  = inject(AuthService);
  private user  = inject(UserService);
  private roles = inject(RoleService);                   // <-- usar tus helpers

  private reload$ = new BehaviorSubject<void>(undefined);

  private header$ = this.reload$.pipe(
    switchMap(() => defer(() => this.user.getHeader()).pipe(catchError(() => of(null)))),
    shareReplay(1)
  );

  /** ¿Es admin? usa RoleService.isAdminRole(...) y environment.enableAdmin */
  readonly isAdmin$ = this.header$.pipe(
    map(h => {
      if (environment.enableAdmin === true) return true;
      const p = pickData(h);
      const role = this.roles.fromHeader(p);
      return this.roles.isAdminRole(role, p);
    })
  );

  refresh() { this.reload$.next(); }
}
