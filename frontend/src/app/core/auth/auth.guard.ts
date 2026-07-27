import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return true;
  }

  if (auth.deveAlterarSenha()) {
    return router.createUrlTree(['/trocar-senha']);
  }

  return router.createUrlTree(['/']);
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
