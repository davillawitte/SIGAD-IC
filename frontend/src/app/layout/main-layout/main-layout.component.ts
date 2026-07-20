import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import {
  PciAppLayoutComponent,
  PciBreadcrumbComponent,
  PciBreadcrumbItem,
  PciNavGroup,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../core/auth/auth.service';
import { BreadcrumbService } from '../../core/navigation/breadcrumb.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, PciAppLayoutComponent, PciBreadcrumbComponent],
  template: `
    <pci-app-layout
      brandTitle="SIGAD-IC"
      brandSubtitle="Instituto de Criminalística — RN"
      brandLogoSrc="/assets/images/ic-icon.png"
      [navGroups]="navGroups()"
      [activeItemId]="activeItemId()"
      breadcrumbRoot=""
      breadcrumbCurrent=""
      [userName]="userName()"
      [userMeta]="userMeta()"
      [userAvatarName]="userAvatarName()"
      [collapsed]="collapsed()"
      [mobileOpen]="mobileOpen()"
      (collapsedChange)="collapsed.set($event)"
      (mobileOpenChange)="mobileOpen.set($event)"
      (activeItemChange)="onNavChange($event)"
      (logout)="onLogout()"
    >
      <div class="layout-body" (click)="onCrumbAreaClick($event)">
        <pci-breadcrumb [items]="breadcrumb.items()" (itemClick)="onBreadcrumbClick($event)" />
        <router-outlet />
      </div>
    </pci-app-layout>
  `,
  styles: `
    .layout-body {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    :host ::ng-deep .pci-app-layout__breadcrumb {
      display: none;
    }
  `,
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly breadcrumb = inject(BreadcrumbService);

  readonly collapsed = signal(false);
  readonly mobileOpen = signal(false);

  private readonly routeMap: Record<string, string> = {
    home: '/',
    usuarios: '/usuarios',
    perfis: '/perfis',
    permissoes: '/permissoes',
  };

  readonly activeItemId = computed(() => this.breadcrumb.navId());

  readonly navGroups = computed<PciNavGroup[]>(() => {
    const groups: PciNavGroup[] = [
      {
        title: 'Principal',
        items: [{ id: 'home', label: 'Início', icon: 'home' }],
      },
    ];

    if (this.auth.isSuperAdmin()) {
      groups.push({
        title: 'Administração do Sistema',
        items: [
          { id: 'usuarios', label: 'Usuários', icon: 'users' },
          { id: 'perfis', label: 'Perfis', icon: 'shield' },
          { id: 'permissoes', label: 'Permissões', icon: 'lock' },
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

    return user.perfis[0] ?? user.meta ?? 'SIGAD-IC';
  });

  readonly userAvatarName = computed(
    () => this.auth.currentUser()?.displayName ?? 'Usuário',
  );

  onNavChange(itemId: string): void {
    const path = this.routeMap[itemId] ?? '/';
    void this.router.navigateByUrl(path);
  }

  onBreadcrumbClick(item: PciBreadcrumbItem): void {
    if (item.href) {
      void this.router.navigateByUrl(item.href);
    }
  }

  onCrumbAreaClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (target?.closest('a.pci-breadcrumb__link')) {
      event.preventDefault();
    }
  }

  onLogout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
