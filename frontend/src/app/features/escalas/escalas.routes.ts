import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/auth.guard';
import { escalaFormCanDeactivate } from './escala-form.guard';

export const ESCALAS_ROUTES: Routes = [
  {
    path: 'escalas',
    canActivate: [permissionGuard('escalas.listar')],
    data: { navId: 'escalas', escopo: 'setor' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/escala-list/escala-list').then((m) => m.EscalaList),
        data: { escopo: 'setor' },
      },
      {
        path: 'nova',
        canActivate: [permissionGuard('escalas.criar')],
        canDeactivate: [escalaFormCanDeactivate],
        loadComponent: () =>
          import('./pages/escala-form/escala-form').then((m) => m.EscalaForm),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/escala-detail/escala-detail').then((m) => m.EscalaDetail),
        data: { escopo: 'setor' },
      },
      {
        path: ':id/editar',
        canActivate: [permissionGuard('escalas.editar')],
        canDeactivate: [escalaFormCanDeactivate],
        loadComponent: () =>
          import('./pages/escala-form/escala-form').then((m) => m.EscalaForm),
      },
      {
        path: ':id/calendario',
        loadComponent: () =>
          import('./pages/escala-calendario/escala-calendario').then(
            (m) => m.EscalaCalendario,
          ),
        data: { escopo: 'setor' },
      },
    ],
  },
  {
    path: 'escalas-institucionais',
    canActivate: [permissionGuard('escalas.listar')],
    data: { navId: 'escalas-institucionais', escopo: 'institucional' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/escala-list/escala-list').then((m) => m.EscalaList),
        data: { escopo: 'institucional' },
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/escala-detail/escala-detail').then((m) => m.EscalaDetail),
        data: { escopo: 'institucional' },
      },
      {
        path: ':id/calendario',
        loadComponent: () =>
          import('./pages/escala-calendario/escala-calendario').then(
            (m) => m.EscalaCalendario,
          ),
        data: { escopo: 'institucional' },
      },
    ],
  },
];
