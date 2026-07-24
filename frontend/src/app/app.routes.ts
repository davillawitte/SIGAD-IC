import { Routes } from '@angular/router';

import { authGuard, guestGuard, mustChangePasswordGuard, passwordOkGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'trocar-senha',
    canActivate: [mustChangePasswordGuard],
    loadComponent: () =>
      import('./features/auth/pages/trocar-senha/trocar-senha-page.component').then(
        (m) => m.TrocarSenhaPageComponent,
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
          import('./features/gestao-setor/gestao-setor.routes').then((m) => m.GESTAO_SETOR_ROUTES),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/pages/not-found/not-found.component').then(
        (m) => m.NotFoundComponent,
      ),
  },
];
