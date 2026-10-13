import { PciRoutePageMeta } from '@davillawitte/pci-design-system';

export const AFASTAMENTOS_ROUTE_PAGES: PciRoutePageMeta[] = [
  { path: '/afastamentos', label: 'Afastamentos' },
  { path: '/afastamentos/novo', label: 'Novo', parentPath: '/afastamentos' },
  { path: '/afastamentos/editar/:id', label: 'Editar', parentPath: '/afastamentos' },
  { path: '/afastamentos-institucionais', label: 'Afastamentos Institucionais' },
  {
    path: '/afastamentos-institucionais/novo',
    label: 'Novo',
    parentPath: '/afastamentos-institucionais',
  },
  {
    path: '/afastamentos-institucionais/editar/:id',
    label: 'Editar',
    parentPath: '/afastamentos-institucionais',
  },
];
