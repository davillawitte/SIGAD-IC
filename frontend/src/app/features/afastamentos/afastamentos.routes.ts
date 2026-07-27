import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/auth.guard';

export const AFASTAMENTOS_ROUTES: Routes = [
  {
    path: 'afastamentos',
    canActivate: [permissionGuard('afastamentos.listar')],
    data: { navId: 'afastamentos', escopo: 'setor' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/afastamento-list/afastamento-list').then((m) => m.AfastamentoList),
        data: { escopo: 'setor' },
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
  {
    path: 'afastamentos-institucionais',
    canActivate: [permissionGuard('afastamentos.listar')],
    data: { navId: 'afastamentos-institucionais', escopo: 'institucional' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/afastamento-list/afastamento-list').then((m) => m.AfastamentoList),
        data: { escopo: 'institucional' },
      },
    ],
  },
];
