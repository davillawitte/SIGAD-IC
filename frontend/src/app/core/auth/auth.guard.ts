import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AuthService } from './auth.service';
import { SetupApiService } from './setup-api.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

/**
 * Não autenticado: libera /login, exceto quando ainda falta concluir o setup (redireciona
 * para /setup). Autenticado: mesma prioridade de sempre (troca de senha obrigatória > home).
 */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    if (auth.deveAlterarSenha()) {
      return router.createUrlTree(['/trocar-senha']);
    }

    return router.createUrlTree(['/']);
  }

  const setupApi = inject(SetupApiService);
  return setupApi.status().pipe(
    map((status) => (status.needsSetup ? router.createUrlTree(['/setup']) : true)),
    catchError(() => of(true)),
  );
};

/** /setup só é acessível enquanto o provisionamento estiver pendente. */
export const setupGuard: CanActivateFn = () => {
  const setupApi = inject(SetupApiService);
  const router = inject(Router);

  return setupApi.status().pipe(
    map((status) => (status.needsSetup ? true : router.createUrlTree(['/login']))),
    catchError(() => of(router.createUrlTree(['/login']))),
  );
};

export const mustChangePasswordGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (!auth.deveAlterarSenha()) {
    return router.createUrlTree(['/']);
  }

  return true;
};

export const passwordOkGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (auth.deveAlterarSenha()) {
    return router.createUrlTree(['/trocar-senha']);
  }

  return true;
};

export const superAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated() && auth.isSuperAdmin()) {
    return true;
  }

  return router.createUrlTree(['/']);
};

export const permissionGuard = (code: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isAuthenticated() && auth.hasPermission(code)) {
      return true;
    }

    return router.createUrlTree(['/']);
  };
};

export const anyPermissionGuard = (...codes: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isAuthenticated() && codes.some((code) => auth.hasPermission(code))) {
      return true;
    }

    return router.createUrlTree(['/']);
  };
};
