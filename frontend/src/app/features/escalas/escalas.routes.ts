import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/auth.guard';
import { escalaWizardCanDeactivate } from './escalas-wizard.guard';

export const ESCALAS_ROUTES: Routes = [
  {
    path: 'escalas',
    canActivate: [permissionGuard('escalas.listar')],
    data: { navId: 'escalas' },
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/escalas-page.component').then((m) => m.EscalasPageComponent),
      },
      {
        path: 'nova',
        canActivate: [permissionGuard('escalas.criar')],
        canDeactivate: [escalaWizardCanDeactivate],
        loadComponent: () =>
          import('./pages/escala-wizard-page.component').then((m) => m.EscalaWizardPageComponent),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/escala-detail-page.component').then((m) => m.EscalaDetailPageComponent),
      },
      {
        path: ':id/editar',
        canActivate: [permissionGuard('escalas.editar')],
        canDeactivate: [escalaWizardCanDeactivate],
        loadComponent: () =>
          import('./pages/escala-wizard-page.component').then((m) => m.EscalaWizardPageComponent),
      },
      {
        path: ':id/calendario',
        loadComponent: () =>
          import('./pages/escala-calendario-page.component').then(
            (m) => m.EscalaCalendarioPageComponent,
          ),
      },
      {
        path: ':id/copiar',
        canActivate: [permissionGuard('escalas.criar')],
        loadComponent: () =>
          import('./pages/escala-copiar-page.component').then((m) => m.EscalaCopiarPageComponent),
      },
    ],
  },
];
