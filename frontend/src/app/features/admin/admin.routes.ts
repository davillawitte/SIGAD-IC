import { Routes } from '@angular/router';

import { anyPermissionGuard } from '../../core/auth/auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'usuarios',
    canActivate: [anyPermissionGuard('usuarios.listar')],
    data: { navId: 'usuarios' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/usuario-list/usuario-list').then((m) => m.UsuarioList),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('usuarios.criar')],
        loadComponent: () =>
          import('./pages/usuario-form/usuario-form').then(
            (m) => m.UsuarioForm,
          ),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('usuarios.editar')],
        loadComponent: () =>
          import('./pages/usuario-form/usuario-form').then(
            (m) => m.UsuarioForm,
          ),
      },
    ],
  },
  {
    path: 'perfis',
    canActivate: [anyPermissionGuard('perfis.listar')],
    data: { navId: 'perfis' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/perfil-list/perfil-list').then((m) => m.PerfilList),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('perfis.criar')],
        loadComponent: () =>
          import('./pages/perfil-form/perfil-form').then((m) => m.PerfilForm),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('perfis.editar')],
        loadComponent: () =>
          import('./pages/perfil-form/perfil-form').then((m) => m.PerfilForm),
      },
    ],
  },
  {
    path: 'servidores',
    canActivate: [anyPermissionGuard('servidores.listar')],
    data: { navId: 'servidores' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/servidor-list/servidor-list').then(
            (m) => m.ServidorList,
          ),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('servidores.criar')],
        loadComponent: () =>
          import('./pages/servidor-form/servidor-form').then(
            (m) => m.ServidorForm,
          ),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('servidores.editar')],
        loadComponent: () =>
          import('./pages/servidor-form/servidor-form').then(
            (m) => m.ServidorForm,
          ),
      },
    ],
  },
  {
    path: 'estrutura-organizacional',
    canActivate: [anyPermissionGuard('nucleos.listar', 'setores.listar')],
    data: { navId: 'estrutura-organizacional' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/estrutura-list/estrutura-list').then(
            (m) => m.EstruturaList,
          ),
      },
      {
        path: 'nucleos/novo',
        canActivate: [anyPermissionGuard('nucleos.criar')],
        loadComponent: () =>
          import('./pages/nucleo-form/nucleo-form').then(
            (m) => m.NucleoForm,
          ),
      },
      {
        path: 'nucleos/editar/:id',
        canActivate: [anyPermissionGuard('nucleos.editar')],
        loadComponent: () =>
          import('./pages/nucleo-form/nucleo-form').then(
            (m) => m.NucleoForm,
          ),
      },
      {
        path: 'setores/novo',
        canActivate: [anyPermissionGuard('setores.criar')],
        loadComponent: () =>
          import('./pages/setor-form/setor-form').then(
            (m) => m.SetorForm,
          ),
      },
      {
        path: 'setores/editar/:id',
        canActivate: [anyPermissionGuard('setores.editar')],
        loadComponent: () =>
          import('./pages/setor-form/setor-form').then(
            (m) => m.SetorForm,
          ),
      },
    ],
  },
];
