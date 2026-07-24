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
          import('./pages/usuarios/usuarios-page.component').then((m) => m.UsuariosPageComponent),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('usuarios.criar')],
        loadComponent: () =>
          import('./pages/usuarios/usuario-form-page.component').then(
            (m) => m.UsuarioFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('usuarios.editar')],
        loadComponent: () =>
          import('./pages/usuarios/usuario-form-page.component').then(
            (m) => m.UsuarioFormPageComponent,
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
          import('./pages/perfis/perfis-page.component').then((m) => m.PerfisPageComponent),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('perfis.criar')],
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('perfis.editar')],
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
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
          import('./pages/servidores/servidores-page.component').then(
            (m) => m.ServidoresPageComponent,
          ),
      },
      {
        path: 'novo',
        canActivate: [anyPermissionGuard('servidores.criar')],
        loadComponent: () =>
          import('./pages/servidores/servidor-form-page.component').then(
            (m) => m.ServidorFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        canActivate: [anyPermissionGuard('servidores.editar')],
        loadComponent: () =>
          import('./pages/servidores/servidor-form-page.component').then(
            (m) => m.ServidorFormPageComponent,
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
          import('./pages/estrutura/estrutura-organizacional-page.component').then(
            (m) => m.EstruturaOrganizacionalPageComponent,
          ),
      },
      {
        path: 'nucleos/novo',
        canActivate: [anyPermissionGuard('nucleos.criar')],
        loadComponent: () =>
          import('./pages/estrutura/nucleo-form-page.component').then(
            (m) => m.NucleoFormPageComponent,
          ),
      },
      {
        path: 'nucleos/editar/:id',
        canActivate: [anyPermissionGuard('nucleos.editar')],
        loadComponent: () =>
          import('./pages/estrutura/nucleo-form-page.component').then(
            (m) => m.NucleoFormPageComponent,
          ),
      },
      {
        path: 'setores/novo',
        canActivate: [anyPermissionGuard('setores.criar')],
        loadComponent: () =>
          import('./pages/estrutura/setor-form-page.component').then(
            (m) => m.SetorFormPageComponent,
          ),
      },
      {
        path: 'setores/editar/:id',
        canActivate: [anyPermissionGuard('setores.editar')],
        loadComponent: () =>
          import('./pages/estrutura/setor-form-page.component').then(
            (m) => m.SetorFormPageComponent,
          ),
      },
    ],
  },
];
