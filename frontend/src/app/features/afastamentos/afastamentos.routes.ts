import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/auth.guard';

export const AFASTAMENTOS_ROUTES: Routes = [
  {
    path: 'afastamentos',
    canActivate: [permissionGuard('afastamentos.listar')],
    data: { navId: 'afastamentos' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/afastamento-list/afastamento-list').then((m) => m.AfastamentoList),
      },
      {
        path: 'novo',
        canActivate: [permissionGuard('afastamentos.criar')],
        loadComponent: () =>
          import('./pages/afastamento-form/afastamento-form').then((m) => m.AfastamentoForm),
      },
      {
        path: 'editar/:id',
        canActivate: [permissionGuard('afastamentos.editar')],
        loadComponent: () =>
          import('./pages/afastamento-form/afastamento-form').then((m) => m.AfastamentoForm),
      },
    ],
  },
];
