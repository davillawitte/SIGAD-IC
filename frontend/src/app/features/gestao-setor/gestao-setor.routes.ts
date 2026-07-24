import { Routes } from '@angular/router';

import { authGuard, permissionGuard } from '../../core/auth/auth.guard';

export const GESTAO_SETOR_ROUTES: Routes = [
  {
    path: 'afastamentos',
    canActivate: [permissionGuard('afastamentos.listar')],
    data: { navId: 'afastamentos' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/afastamentos/afastamentos-page.component').then(
            (m) => m.AfastamentosPageComponent,
          ),
      },
      {
        path: 'novo',
        canActivate: [permissionGuard('afastamentos.criar')],
        loadComponent: () =>
          import('./pages/afastamentos/afastamento-form-page.component').then(
            (m) => m.AfastamentoFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        canActivate: [permissionGuard('afastamentos.editar')],
        loadComponent: () =>
          import('./pages/afastamentos/afastamento-form-page.component').then(
            (m) => m.AfastamentoFormPageComponent,
          ),
      },
    ],
  },
  {
    path: 'solicitacoes-trocas',
    canActivate: [authGuard],
    data: { navId: 'solicitacoes-trocas' },
    loadComponent: () =>
      import('./pages/solicitacoes-trocas-page.component').then(
        (m) => m.SolicitacoesTrocasPageComponent,
      ),
  },
];
