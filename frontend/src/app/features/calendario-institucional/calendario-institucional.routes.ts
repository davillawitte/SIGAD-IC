import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/auth.guard';

export const CALENDARIO_INSTITUCIONAL_ROUTES: Routes = [
  {
    path: 'calendario-institucional',
    canActivate: [permissionGuard('calendario.listar')],
    data: { navId: 'calendario-institucional' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/calendario-ano-list/calendario-ano-list').then(
            (m) => m.CalendarioAnoList,
          ),
      },
      {
        path: 'novo',
        canActivate: [permissionGuard('calendario.gerar')],
        loadComponent: () =>
          import('./pages/calendario-novo/calendario-novo').then((m) => m.CalendarioNovo),
      },
      {
        path: ':ano',
        loadComponent: () =>
          import('./pages/calendario-ano-detail/calendario-ano-detail').then(
            (m) => m.CalendarioAnoDetail,
          ),
      },
      {
        path: ':ano/editar',
        canActivate: [permissionGuard('calendario.gerar')],
        loadComponent: () =>
          import('./pages/calendario-ano-edit/calendario-ano-edit').then(
            (m) => m.CalendarioAnoEdit,
          ),
      },
      {
        path: ':ano/marcacoes/novo',
        canActivate: [permissionGuard('calendario.gerenciar_marcacoes')],
        loadComponent: () =>
          import('./pages/marcacao-form/marcacao-form').then((m) => m.MarcacaoForm),
      },
      {
        path: ':ano/marcacoes/:id/editar',
        canActivate: [permissionGuard('calendario.gerenciar_marcacoes')],
        loadComponent: () =>
          import('./pages/marcacao-form/marcacao-form').then((m) => m.MarcacaoForm),
      },
    ],
  },
];
