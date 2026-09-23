import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';

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
    // Gestão do Setor: quem chefia o setor decide as permutas dos servidores dele.
    path: 'permutas',
    canActivate: [authGuard],
    data: { navId: 'permutas' },
    loadComponent: () =>
      import('./pages/permutas-list/permutas-list').then((m) => m.PermutasList),
  },
];
