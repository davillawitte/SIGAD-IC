import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import {
  PciAppLayoutComponent,
  PciFeedbackModalComponent,
  PciNavGroup,
  PciToastContainerComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../core/auth/auth.service';
import { NavActiveService } from '../../core/navigation/nav-active.service';

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    PciAppLayoutComponent,
    PciToastContainerComponent,
    PciFeedbackModalComponent,
  ],
  host: {
    '(click)': 'onBreadcrumbLinkClick($event)',
  },
  templateUrl: './main-layout.component.html',
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly navActive = inject(NavActiveService);

  readonly collapsed = signal(false);
  readonly mobileOpen = signal(false);

  private readonly routeMap: Record<string, string> = {
    home: '/',
    usuarios: '/usuarios',
    perfis: '/perfis',
    servidores: '/servidores',
    'estrutura-organizacional': '/estrutura-organizacional',
    escalas: '/escalas',
    afastamentos: '/afastamentos',
    'solicitacoes-trocas': '/solicitacoes-trocas',
  };

  readonly activeItemId = computed(() => this.navActive.navId());

  readonly navGroups = computed<PciNavGroup[]>(() => {
    const groups: PciNavGroup[] = [
      {
        title: 'Principal',
        items: [{ id: 'home', label: 'Início', icon: 'home' }],
      },
    ];

    if (
      this.auth.hasAnyPermission([
        'nucleos.listar',
        'setores.listar',
        'servidores.listar',
        'cargos.listar',
      ])
    ) {
      groups.push({
        title: 'Gestão Institucional',
        items: [
          { id: 'servidores', label: 'Servidores', icon: 'users' },
          { id: 'estrutura-organizacional', label: 'Estrutura Organizacional', icon: 'building' },
        ],
      });
    }

    if (this.auth.hasAnyPermission(['escalas.listar', 'afastamentos.listar'])) {
      groups.push({
        title: 'Gestão do Setor',
        items: [
          ...(this.auth.hasPermission('escalas.listar')
            ? [{ id: 'escalas', label: 'Escalas', icon: 'schedule' as const }]
            : []),
          ...(this.auth.hasPermission('afastamentos.listar')
            ? [{ id: 'afastamentos', label: 'Afastamentos', icon: 'calendar' as const }]
            : []),
          { id: 'solicitacoes-trocas', label: 'Solicitações de trocas', icon: 'refresh' as const },
        ],
      });
    }

    if (this.auth.hasAnyPermission(['usuarios.listar', 'perfis.listar'])) {
      groups.push({
        title: 'Administração do Sistema',
        items: [
          { id: 'usuarios', label: 'Usuários', icon: 'users' },
          { id: 'perfis', label: 'Perfis', icon: 'shield' },
        ],
      });
    }

    return groups;
  });

  readonly userName = computed(
    () => this.auth.currentUser()?.displayName ?? 'Usuário',
  );

  readonly userMeta = computed(() => {
    const user = this.auth.currentUser();
    if (!user) {
      return 'SIGAD-IC';
    }

    return user.setorLotacaoNome?.trim() || user.meta || 'SIGAD-IC';
  });

  readonly userAvatarName = computed(
    () => this.auth.currentUser()?.displayName ?? 'Usuário',
  );

  onNavChange(itemId: string): void {
    const path = this.routeMap[itemId] ?? '/';
    void this.router.navigateByUrl(path);
  }

  /** Intercepta links do breadcrumb da topbar para navegação SPA. */
  onBreadcrumbLinkClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    const link = target?.closest('a.pci-breadcrumb__link');
    if (!(link instanceof HTMLAnchorElement)) {
      return;
    }

    const href = link.getAttribute('href');
    if (!href || href === '#') {
      return;
    }

    event.preventDefault();
    void this.router.navigateByUrl(href);
  }

  onLogout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
