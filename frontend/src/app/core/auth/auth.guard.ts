import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PciToastService } from '@davillawitte/pci-design-system';
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
 * para /setup). Autenticado: vai para a home — a não ser que a troca de senha obrigatória
 * esteja pendente, caso em que pedir o login é desistir do primeiro acesso: a sessão é
 * encerrada e o login aparece (mesma saída do botão "Cancelar" em `trocar-senha-form`).
 */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    if (auth.deveAlterarSenha()) {
      auth.logout();
      return true;
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

/** Barrado por falta de permissão: avisa e volta ao início. Antes voltava calado e a tela
 * parecia "não abrir" (ex.: item do menu visível sem a permissão da rota). */
function semPermissao(router: Router, auth: AuthService) {
  if (auth.isAuthenticated()) {
    inject(PciToastService).showError(
      'Você não tem permissão para acessar esta tela. Fale com o administrador do sistema.',
    );
  }
  return router.createUrlTree(['/']);
}

export const permissionGuard = (code: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isAuthenticated() && auth.hasPermission(code)) {
      return true;
    }

    return semPermissao(router, auth);
  };
};

/** Visão global do módulo (`<modulo>.listar` em TodosOsSetores) — ex.: Gestão Institucional. */
export const visaoGlobalGuard = (modulo: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isAuthenticated() && auth.hasVisaoGlobal(modulo)) {
      return true;
    }

    return semPermissao(router, auth);
  };
};

export const anyPermissionGuard = (...codes: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isAuthenticated() && codes.some((code) => auth.hasPermission(code))) {
      return true;
    }

    return semPermissao(router, auth);
  };
};
