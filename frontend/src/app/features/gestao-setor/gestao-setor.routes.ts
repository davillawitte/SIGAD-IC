import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';

export const GESTAO_SETOR_ROUTES: Routes = [
  {
    path: 'solicitacoes-trocas',
    canActivate: [authGuard],
    data: { navId: 'solicitacoes-trocas' },
    loadComponent: () =>
      import('./pages/solicitacao-troca-list/solicitacao-troca-list').then(
        (m) => m.SolicitacaoTrocaList,
      ),
  },
];
