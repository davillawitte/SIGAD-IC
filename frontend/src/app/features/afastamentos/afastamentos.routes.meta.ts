import { PciRoutePageMeta } from '@davillawitte/pci-design-system';

export const AFASTAMENTOS_ROUTE_PAGES: PciRoutePageMeta[] = [
  { path: '/afastamentos', label: 'Afastamentos' },
  { path: '/afastamentos/novo', label: 'Novo', parentPath: '/afastamentos' },
  { path: '/afastamentos/editar/:id', label: 'Editar', parentPath: '/afastamentos' },
];
