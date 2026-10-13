import { Routes } from '@angular/router';

import { authGuard, permissionGuard } from '../../core/auth/auth.guard';

export const PERMUTAS_ROUTES: Routes = [
  {
    // Área do Servidor: acesso mínimo de qualquer servidor autenticado, sem permissão específica.
    path: 'minhas-permutas',
    canActivate: [authGuard],
    data: { navId: 'minhas-permutas' },
    loadComponent: () =>
      import('./pages/minhas-permutas-list/minhas-permutas-list').then((m) => m.MinhasPermutasList),
  },
  {
    // Gestão do Setor: a chefia registra as permutas. Permuta altera escala, então usa as
    // permissões de escala; a chefia da lotação é validada na API.
    path: 'permutas',
    canActivate: [permissionGuard('escalas.listar')],
    data: { navId: 'permutas' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/permutas-list/permutas-list').then((m) => m.PermutasList),
      },
      {
        path: 'nova',
        canActivate: [permissionGuard('escalas.editar')],
        loadComponent: () =>
          import('./pages/permuta-form/permuta-form').then((m) => m.PermutaForm),
      },
    ],
  },
];
