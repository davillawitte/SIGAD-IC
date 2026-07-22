import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/** Mantém apenas o item ativo do menu lateral (breadcrumb vem do design system). */
@Injectable({ providedIn: 'root' })
export class NavActiveService {
  private readonly router = inject(Router);
  private readonly navIdSignal = signal('home');

  readonly navId = this.navIdSignal.asReadonly();

  constructor() {
    this.refresh();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.refresh());
  }

  private refresh(): void {
    const trail = this.collect(this.router.routerState.snapshot.root);
    let navId = 'home';
    for (const entry of trail) {
      if (entry.navId) {
        navId = entry.navId;
      }
    }
    this.navIdSignal.set(navId);
  }

  private collect(route: ActivatedRouteSnapshot): Array<{ navId?: string }> {
    const result: Array<{ navId?: string }> = [];

    const walk = (node: ActivatedRouteSnapshot) => {
      const data = node.data as { navId?: string };
      if (data?.navId) {
        result.push({ navId: data.navId });
      }
      for (const child of node.children) {
        walk(child);
      }
    };

    walk(route);
    return result;
  }
}
