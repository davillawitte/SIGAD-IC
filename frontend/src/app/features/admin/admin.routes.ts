import { Routes } from '@angular/router';

import { superAdminGuard } from '../../core/auth/auth.guard';

const adminSection = 'Administração do Sistema';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'usuarios',
    canActivate: [superAdminGuard],
    data: {
      title: 'Usuários',
      breadcrumb: 'Usuários',
      section: adminSection,
      navId: 'usuarios',
    },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/usuarios/usuarios-page.component').then((m) => m.UsuariosPageComponent),
      },
      {
        path: 'novo',
        data: { title: 'Novo usuário', breadcrumb: 'Novo' },
        loadComponent: () =>
          import('./pages/usuarios/usuario-form-page.component').then(
            (m) => m.UsuarioFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        data: { title: 'Editar usuário', breadcrumb: 'Editar' },
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
    data: {
      title: 'Perfis',
      breadcrumb: 'Perfis',
      section: adminSection,
      navId: 'perfis',
    },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/perfis/perfis-page.component').then((m) => m.PerfisPageComponent),
      },
      {
        path: 'novo',
        data: { title: 'Novo perfil', breadcrumb: 'Novo' },
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
      },
      {
        path: 'editar/:id',
        data: { title: 'Editar perfil', breadcrumb: 'Editar' },
        loadComponent: () =>
          import('./pages/perfis/perfil-form-page.component').then((m) => m.PerfilFormPageComponent),
      },
    ],
  },
  {
    path: 'permissoes',
    canActivate: [superAdminGuard],
    data: {
      title: 'Permissões',
      breadcrumb: 'Permissões',
      section: adminSection,
      navId: 'permissoes',
    },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/permissoes/permissoes-page.component').then(
            (m) => m.PermissoesPageComponent,
          ),
      },
      {
        path: 'novo',
        data: { title: 'Nova permissão', breadcrumb: 'Nova' },
        loadComponent: () =>
          import('./pages/permissoes/permissao-form-page.component').then(
            (m) => m.PermissaoFormPageComponent,
          ),
      },
      {
        path: 'editar/:id',
        data: { title: 'Editar permissão', breadcrumb: 'Editar' },
        loadComponent: () =>
          import('./pages/permissoes/permissao-form-page.component').then(
            (m) => m.PermissaoFormPageComponent,
          ),
      },
    ],
  },
];
