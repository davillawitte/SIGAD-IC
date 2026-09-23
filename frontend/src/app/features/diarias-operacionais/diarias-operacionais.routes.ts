import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';

export const DIARIAS_OPERACIONAIS_ROUTES: Routes = [
  {
    // Área do Servidor: acesso mínimo de qualquer servidor autenticado, sem permissão específica.
    path: 'minhas-diarias',
    canActivate: [authGuard],
    data: { navId: 'minhas-diarias' },
    loadComponent: () =>
      import('./pages/minhas-diarias-list/minhas-diarias-list').then((m) => m.MinhasDiariasList),
  },
  {
    // Gestão do Setor.
    path: 'diarias-operacionais',
    canActivate: [authGuard],
    data: { navId: 'diarias-operacionais' },
    loadComponent: () =>
      import('./pages/diarias-list/diarias-list').then((m) => m.DiariasList),
  },
  {
    // Gestão Institucional.
    path: 'diarias-operacionais-institucionais',
    canActivate: [authGuard],
    data: { navId: 'diarias-operacionais-institucionais' },
    loadComponent: () =>
      import('./pages/diarias-institucionais-list/diarias-institucionais-list').then(
        (m) => m.DiariasInstitucionaisList,
      ),
  },
];
