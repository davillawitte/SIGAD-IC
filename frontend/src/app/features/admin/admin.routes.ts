import { Routes } from '@angular/router';

import { superAdminGuard } from '../../core/auth/auth.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'usuarios',
    canActivate: [superAdminGuard],
    data: { navId: 'usuarios' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/usuarios/usuarios-page.component').then((m) => m.UsuariosPageComponent),
      },
      {
        path: 'novo',
        loadComponent: () =>
          import('./pages/usuarios/usuario-form-page.component').then(
            (m) => m.UsuarioFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        loadComponent: () =>
          import('./pages/usuarios/usuario-form-page.component').then(
            (m) => m.UsuarioFormPageComponent,
          ),
      },
    ],
  },
  {
    path: 'perfis',
    canActivate: [superAdminGuard],
    data: { navId: 'perfis' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/perfis/perfis-page.component').then((m) => m.PerfisPageComponent),
      },
      {
        path: 'novo',
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
      },
      {
        path: 'editar/:id',
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
      },
    ],
  },
  {
    path: 'servidores',
    canActivate: [superAdminGuard],
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
        loadComponent: () =>
          import('./pages/servidores/servidor-form-page.component').then(
            (m) => m.ServidorFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        loadComponent: () =>
          import('./pages/servidores/servidor-form-page.component').then(
            (m) => m.ServidorFormPageComponent,
          ),
      },
    ],
  },
  {
    path: 'estrutura-organizacional',
    canActivate: [superAdminGuard],
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
        loadComponent: () =>
          import('./pages/estrutura/nucleo-form-page.component').then(
            (m) => m.NucleoFormPageComponent,
          ),
      },
      {
        path: 'nucleos/editar/:id',
        loadComponent: () =>
          import('./pages/estrutura/nucleo-form-page.component').then(
            (m) => m.NucleoFormPageComponent,
          ),
      },
      {
        path: 'setores/novo',
        loadComponent: () =>
          import('./pages/estrutura/setor-form-page.component').then(
            (m) => m.SetorFormPageComponent,
          ),
      },
      {
        path: 'setores/editar/:id',
        loadComponent: () =>
          import('./pages/estrutura/setor-form-page.component').then(
            (m) => m.SetorFormPageComponent,
          ),
      },
    ],
  },
];
