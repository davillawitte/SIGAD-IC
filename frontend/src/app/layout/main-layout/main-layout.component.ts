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

/** Setor "Direção IC" é catálogo estrutural especial — quem chefia lá não é "chefe de setor"
 * no sentido operacional do rótulo abaixo; mostra o cargo institucional (Diretor(a)/
 * Subcoordenador(a), conforme `TipoChefia` em `ChefiaResumo`) em vez disso. Ver
 * `SetorSiglas.IsDirecaoIc` no backend e `isDirecaoIcSigla` em escala-detail.ts, mesmo padrão. */
function isDirecaoIcSigla(sigla: string | null | undefined): boolean {
  const n = (sigla ?? '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '');
  return n === 'direcao ic';
}

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
    'escalas-institucionais': '/escalas-institucionais',
    afastamentos: '/afastamentos',
    'afastamentos-institucionais': '/afastamentos-institucionais',
    'calendario-institucional': '/calendario-institucional',
  };

  readonly activeItemId = computed(() => this.navActive.navId());

  readonly navGroups = computed<PciNavGroup[]>(() => {
    const groups: PciNavGroup[] = [
      {
        title: 'Principal',
        items: [{ id: 'home', label: 'Início', icon: 'home' }],
      },
    ];

    const gestaoInstitucional = [
      ...(this.auth.hasGestaoInstitucional()
        ? [
            {
              id: 'escalas-institucionais',
              label: 'Escalas',
              icon: 'clock' as const,
            },
            {
              id: 'afastamentos-institucionais',
              label: 'Afastamentos',
              icon: 'user-x' as const,
            },
          ]
        : []),
      ...(this.auth.hasPermission('calendario.listar')
        ? [
            {
              id: 'calendario-institucional',
              label: 'Calendário',
              icon: 'calendar' as const,
            },
          ]
        : []),
      ...(this.auth.hasPermission('servidores.listar')
        ? [{ id: 'servidores', label: 'Servidores', icon: 'users' as const }]
        : []),
      ...(this.auth.hasAnyPermission(['nucleos.listar', 'setores.listar'])
        ? [
            {
              id: 'estrutura-organizacional',
              label: 'Estrutura Organizacional',
              icon: 'building' as const,
            },
          ]
        : []),
    ];
    if (gestaoInstitucional.length) {
      groups.push({ title: 'Gestão Institucional', items: gestaoInstitucional });
    }

    // Gestão do Setor: aparece quando o usuário é chefia de algum setor, ou chefe de
    // núcleo (acessa a escala resumida pelo mesmo item "Escalas"), e tem a área
    // operacional (escalas/afastamentos).
    const user = this.auth.currentUser();
    const isChefe =
      (user?.setoresGerenciadosIds?.length ?? 0) > 0 || (user?.nucleosGerenciadosIds?.length ?? 0) > 0;
    const gestaoSetor =
      isChefe
        ? [
            ...(this.auth.hasPermission('escalas.listar')
              ? [{ id: 'escalas', label: 'Escalas', icon: 'clock' as const }]
              : []),
            ...(this.auth.hasPermission('afastamentos.listar')
              ? [{ id: 'afastamentos', label: 'Afastamentos', icon: 'user-x' as const }]
              : []),
          ]
        : [];
    if (gestaoSetor.length) {
      groups.push({ title: 'Gestão do Setor', items: gestaoSetor });
    }

    if (this.auth.isSuperAdmin()) {
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

    const geridos = [...(user.setoresGeridos ?? []), ...(user.nucleosGeridos ?? [])];
    // Quem chefia o setor Direção IC é, institucionalmente, Diretor(a) (TipoChefia.Diretor) ou
    // Subcoordenador(a) (TipoChefia.Subcoordenador) — não "chefe de setor" no sentido
    // operacional do rótulo abaixo.
    const direcaoIc = geridos.find((x) => isDirecaoIcSigla(x.sigla));
    if (direcaoIc) {
      return direcaoIc.tipoChefia === 'Subcoordenador' ? 'Subcoordenador(a)' : 'Diretor(a)';
    }

    const siglas = geridos.map((x) => x.sigla);
    if (siglas.length === 1) {
      return `Chefe do ${siglas[0]}`;
    }
    if (siglas.length > 1) {
      return `Chefe de ${siglas.join(', ')}`;
    }

    return user.setorLotacaoNome?.trim() || user.nucleoLotacaoNome?.trim() || user.meta || 'SIGAD-IC';
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
