import { Routes } from '@angular/router';

import {
  authGuard,
  guestGuard,
  mustChangePasswordGuard,
  passwordOkGuard,
  setupGuard,
} from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'setup',
    canActivate: [setupGuard],
    loadComponent: () =>
      import('./features/auth/pages/setup-wizard/setup-wizard').then((m) => m.SetupWizard),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/login-form/login-form').then((m) => m.LoginForm),
  },
  {
    path: 'trocar-senha',
    canActivate: [mustChangePasswordGuard],
    loadComponent: () =>
      import('./features/auth/pages/trocar-senha-form/trocar-senha-form').then(
        (m) => m.TrocarSenhaForm,
      ),
  },
  {
    path: '',
    canActivate: [authGuard, passwordOkGuard],
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: '',
        loadChildren: () => import('./features/home/home.routes').then((m) => m.HOME_ROUTES),
      },
      {
        path: '',
        loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
      },
      {
        path: '',
        loadChildren: () =>
          import('./features/escalas/escalas.routes').then((m) => m.ESCALAS_ROUTES),
      },
      {
        path: '',
        loadChildren: () =>
          import('./features/afastamentos/afastamentos.routes').then((m) => m.AFASTAMENTOS_ROUTES),
      },
      {
        path: '',
        loadChildren: () =>
          import('./features/gestao-setor/gestao-setor.routes').then((m) => m.GESTAO_SETOR_ROUTES),
      },
      {
        path: '',
        loadChildren: () =>
          import('./features/calendario-institucional/calendario-institucional.routes').then(
            (m) => m.CALENDARIO_INSTITUCIONAL_ROUTES,
          ),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/pages/not-found/not-found').then(
        (m) => m.NotFound,
      ),
  },
];
