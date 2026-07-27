import { PciRoutePageMeta } from '@davillawitte/pci-design-system';

export const ESCALAS_ROUTE_PAGES: PciRoutePageMeta[] = [
  { path: '/escalas', label: 'Escalas' },
  { path: '/escalas/nova', label: 'Nova', parentPath: '/escalas' },
  { path: '/escalas/:id', label: 'Detalhe', parentPath: '/escalas' },
  { path: '/escalas/:id/editar', label: 'Editar', parentPath: '/escalas' },
  { path: '/escalas/:id/calendario', label: 'Calendário', parentPath: '/escalas' },
];
