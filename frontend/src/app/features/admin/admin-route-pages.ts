import { PciRoutePageMeta } from '@davillawitte/pci-design-system';

/** Metadados de rota para breadcrumb automático do design system. */
export const ADMIN_ROUTE_PAGES: PciRoutePageMeta[] = [
  { path: '/usuarios', label: 'Usuários' },
  { path: '/usuarios/novo', label: 'Novo', parentPath: '/usuarios' },
  { path: '/usuarios/editar/:id', label: 'Editar', parentPath: '/usuarios' },

  { path: '/perfis', label: 'Perfis' },
  { path: '/perfis/novo', label: 'Novo', parentPath: '/perfis' },
  { path: '/perfis/editar/:id', label: 'Editar', parentPath: '/perfis' },

  { path: '/servidores', label: 'Servidores' },
  { path: '/servidores/novo', label: 'Novo', parentPath: '/servidores' },
  { path: '/servidores/editar/:id', label: 'Editar', parentPath: '/servidores' },

  { path: '/estrutura-organizacional', label: 'Estrutura Organizacional' },
  {
    path: '/estrutura-organizacional/nucleos/novo',
    label: 'Novo núcleo',
    parentPath: '/estrutura-organizacional',
  },
  {
    path: '/estrutura-organizacional/nucleos/editar/:id',
    label: 'Editar núcleo',
    parentPath: '/estrutura-organizacional',
  },
  {
    path: '/estrutura-organizacional/setores/novo',
    label: 'Novo setor',
    parentPath: '/estrutura-organizacional',
  },
  {
    path: '/estrutura-organizacional/setores/editar/:id',
    label: 'Editar setor',
    parentPath: '/estrutura-organizacional',
  },
];
