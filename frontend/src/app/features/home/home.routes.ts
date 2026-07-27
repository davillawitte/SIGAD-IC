import { Routes } from '@angular/router';

export const HOME_ROUTES: Routes = [
  {
    path: '',
    data: { navId: 'home' },
    loadComponent: () =>
      import('./pages/home/home').then((m) => m.Home),
  },
];
