import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { PciBreadcrumbItem } from '@davillawitte/pci-design-system';
import { filter } from 'rxjs';

export interface AppRouteData {
  title?: string;
  breadcrumb?: string;
  /** Seção pai sem URL própria (ex.: Administração do Sistema). */
  section?: string;
  /** Item ativo no menu lateral. */
  navId?: string;
  /** Quando false, o crumb não recebe link. */
  breadcrumbLink?: boolean;
}

@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  private readonly router = inject(Router);

  private readonly itemsSignal = signal<PciBreadcrumbItem[]>([]);
  private readonly titleSignal = signal('Início');
  private readonly navIdSignal = signal('home');

  readonly items = this.itemsSignal.asReadonly();
  readonly title = this.titleSignal.asReadonly();
  readonly navId = this.navIdSignal.asReadonly();

  constructor() {
    this.refresh();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.refresh());
  }

  private refresh(): void {
    const root = this.router.routerState.snapshot.root;
    const trail = this.collect(root);

    const items: PciBreadcrumbItem[] = [{ label: 'Início', href: '/' }];
    let sectionAdded: string | null = null;
    let navId = 'home';
    let title = 'Início';

    for (const entry of trail) {
      if (entry.section && entry.section !== sectionAdded) {
        items.push({ label: entry.section });
        sectionAdded = entry.section;
      }

      if (entry.navId) {
        navId = entry.navId;
      }

      if (entry.breadcrumb) {
        title = entry.title ?? entry.breadcrumb;
        const isLast = entry === trail[trail.length - 1];
        const linkable = entry.breadcrumbLink !== false && !!entry.url && !isLast;
        items.push({
          label: entry.breadcrumb,
          href: linkable ? entry.url : undefined,
        });
      }
    }

    // Evita "Início > Início"
    const normalized =
      items.length > 1 && items[0].label === items[1].label ? items.slice(1) : items;

    this.itemsSignal.set(normalized);
    this.titleSignal.set(title);
    this.navIdSignal.set(navId);
  }

  private collect(route: ActivatedRouteSnapshot): Array<AppRouteData & { url: string }> {
    const result: Array<AppRouteData & { url: string }> = [];

    const walk = (node: ActivatedRouteSnapshot, parentUrl: string) => {
      const segments = node.url.map((s) => s.path).filter(Boolean);
      const url = segments.length
        ? `${parentUrl}/${segments.join('/')}`.replace(/\/+/g, '/')
        : parentUrl || '/';

      const data = node.data as AppRouteData;
      if (data?.breadcrumb || data?.title || data?.section || data?.navId) {
        result.push({
          ...data,
          breadcrumb: data.breadcrumb ?? data.title,
          url: url === '' ? '/' : url,
        });
      }

      for (const child of node.children) {
        walk(child, url === '/' ? '' : url);
      }
    };

    walk(route, '');
    return result;
  }
}
