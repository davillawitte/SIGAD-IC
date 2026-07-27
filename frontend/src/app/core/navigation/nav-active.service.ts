import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

/**
 * Mantém o item ativo do menu lateral.
 * Resolve primeiro pela URL (caminho mais específico vence), para que
 * `/escalas-institucionais` não seja confundido com `/escalas`.
 */
@Injectable({ providedIn: 'root' })
export class NavActiveService {
  private readonly router = inject(Router);
  private readonly navIdSignal = signal('home');

  readonly navId = this.navIdSignal.asReadonly();

  /** Ordem irrelevante: o match usa o path mais longo. */
  private readonly navPaths: ReadonlyArray<{ id: string; path: string }> = [
    { id: 'escalas-institucionais', path: '/escalas-institucionais' },
    { id: 'afastamentos-institucionais', path: '/afastamentos-institucionais' },
    { id: 'estrutura-organizacional', path: '/estrutura-organizacional' },
    { id: 'solicitacoes-trocas', path: '/solicitacoes-trocas' },
    { id: 'servidores', path: '/servidores' },
    { id: 'usuarios', path: '/usuarios' },
    { id: 'perfis', path: '/perfis' },
    { id: 'escalas', path: '/escalas' },
    { id: 'afastamentos', path: '/afastamentos' },
    { id: 'home', path: '/' },
  ];

  constructor() {
    this.refresh();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.refresh());
  }

  private refresh(): void {
    const byUrl = this.resolveFromUrl(this.router.url);
    if (byUrl) {
      this.navIdSignal.set(byUrl);
      return;
    }

    const trail = this.collect(this.router.routerState.snapshot.root);
    let navId = 'home';
    for (const entry of trail) {
      if (entry.navId) {
        navId = entry.navId;
      }
    }
    this.navIdSignal.set(navId);
  }

  private resolveFromUrl(rawUrl: string): string | null {
    const path = (rawUrl.split('?')[0].split('#')[0] || '/').replace(/\/+$/, '') || '/';
    const sorted = [...this.navPaths].sort((a, b) => b.path.length - a.path.length);

    for (const entry of sorted) {
      if (entry.path === '/') {
        if (path === '/') {
          return entry.id;
        }
        continue;
      }
      if (path === entry.path || path.startsWith(`${entry.path}/`)) {
        return entry.id;
      }
    }

    return null;
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
