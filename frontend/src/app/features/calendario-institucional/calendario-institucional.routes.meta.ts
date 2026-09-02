import { PciRoutePageMeta } from '@davillawitte/pci-design-system';

export const CALENDARIO_ROUTE_PAGES: PciRoutePageMeta[] = [
  { path: '/calendario-institucional', label: 'Calendário' },
  {
    path: '/calendario-institucional/novo',
    label: 'Novo',
    parentPath: '/calendario-institucional',
  },
  {
    path: '/calendario-institucional/:ano',
    label: 'Detalhe',
    parentPath: '/calendario-institucional',
  },
  {
    path: '/calendario-institucional/:ano/editar',
    label: 'Editar',
    parentPath: '/calendario-institucional/:ano',
  },
  {
    path: '/calendario-institucional/:ano/marcacoes/novo',
    label: 'Novo',
    parentPath: '/calendario-institucional/:ano',
  },
  {
    path: '/calendario-institucional/:ano/marcacoes/:id/editar',
    label: 'Editar',
    parentPath: '/calendario-institucional/:ano',
  },
];
