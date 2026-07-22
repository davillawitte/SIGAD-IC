import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciBreadcrumbService,
  PciButtonComponent,
  PciDropdownMenuComponent,
  PciDropdownPanelDirective,
  PciDropdownTriggerDirective,
  PciIconButtonComponent,
  PciIconComponent,
  PciLayoutBreadcrumbService,
  PciPageHeaderComponent,
  PciStackComponent,
  PciTooltipChildDirective,
  PciTooltipComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { EstruturaOrganizacional, NucleoComSetores, SetorListItem } from '../../models/admin.models';

@Component({
  selector: 'app-estrutura-organizacional-page',
  imports: [
    CommonModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciDropdownMenuComponent,
    PciDropdownPanelDirective,
    PciDropdownTriggerDirective,
    PciIconButtonComponent,
    PciIconComponent,
    PciPageHeaderComponent,
    PciStackComponent,
    PciTooltipComponent,
    PciTooltipChildDirective,
  ],
  templateUrl: './estrutura-organizacional-page.component.html',
  styleUrl: './estrutura-organizacional-page.component.scss',
})
export class EstruturaOrganizacionalPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly breadcrumb = inject(PciBreadcrumbService);
  private readonly layoutBreadcrumb = inject(PciLayoutBreadcrumbService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly estrutura = signal<EstruturaOrganizacional | null>(null);
  readonly expandedIds = signal<Set<string>>(new Set());
  readonly novoMenuOpen = signal(false);
  readonly canDeleteNucleo = this.auth.hasPermission('nucleos.excluir');
  readonly canDeleteSetor = this.auth.hasPermission('setores.excluir');

  readonly nucleos = computed(() => this.estrutura()?.nucleos ?? []);
  readonly direcao = computed(() => this.estrutura()?.direcaoIc ?? null);
  readonly totalNucleos = computed(() => this.nucleos().length);

  ngOnInit(): void {
    this.layoutBreadcrumb.setItems(
      this.breadcrumb.buildFromRoutes(ADMIN_ROUTE_PAGES, '/estrutura-organizacional'),
    );
    this.reload();
  }

  ngOnDestroy(): void {
    this.layoutBreadcrumb.clear();
  }

  isExpanded(nucleoId: string): boolean {
    return this.expandedIds().has(nucleoId);
  }

  toggle(nucleoId: string): void {
    const next = new Set(this.expandedIds());
    if (next.has(nucleoId)) {
      next.delete(nucleoId);
    } else {
      next.add(nucleoId);
    }
    this.expandedIds.set(next);
  }

  expandAll(): void {
    this.expandedIds.set(new Set(this.nucleos().map((n) => n.id)));
  }

  collapseAll(): void {
    this.expandedIds.set(new Set());
  }

  setorCountLabel(nucleo: NucleoComSetores): string {
    const n = nucleo.setores.length;
    return n === 1 ? '1 setor' : `${n} setores`;
  }

  chefiaMeta(setor: SetorListItem): string {
    const parts: string[] = [];
    const find = (tipo: string) =>
      (setor.chefias ?? []).find((c) => c.tipoChefia === tipo)?.servidorNome;

    if (setor.isDirecaoIc) {
      const diretor = find('Diretor');
      const sub = find('Subcoordenador');
      if (diretor) parts.push(`Diretor(a): ${diretor}`);
      if (sub) parts.push(`Subcoordenador(a): ${sub}`);
    } else {
      const imediata = find('ChefiaImediata');
      const substituta = find('ChefiaSubstituta');
      if (imediata) parts.push(`Chefia imediata: ${imediata}`);
      if (substituta) parts.push(`Chefia substituta: ${substituta}`);
    }

    return parts.length ? ` · ${parts.join(' · ')}` : '';
  }

  novoNucleo(): void {
    this.novoMenuOpen.set(false);
    void this.router.navigateByUrl('/estrutura-organizacional/nucleos/novo');
  }

  novoSetor(): void {
    this.novoMenuOpen.set(false);
    void this.router.navigateByUrl('/estrutura-organizacional/setores/novo');
  }

  novoSetorNoNucleo(nucleoId: string): void {
    void this.router.navigate(['/estrutura-organizacional/setores/novo'], {
      queryParams: { nucleoId },
    });
  }

  editarNucleo(id: string): void {
    void this.router.navigateByUrl(`/estrutura-organizacional/nucleos/editar/${id}`);
  }

  editarSetor(id: string): void {
    void this.router.navigateByUrl(`/estrutura-organizacional/setores/editar/${id}`);
  }

  excluirNucleo(nucleo: NucleoComSetores): void {
    if (!this.canDeleteNucleo) {
      return;
    }

    const ok = window.confirm(
      `Excluir o núcleo "${nucleo.nome}"? Esta ação não pode ser desfeita.`,
    );
    if (!ok) {
      return;
    }

    this.error.set(null);
    this.api.deleteNucleo(nucleo.id).subscribe({
      next: () => this.reload(),
      error: (err: { error?: { message?: string } }) =>
        this.error.set(err.error?.message ?? 'Operação inválida ao excluir o núcleo.'),
    });
  }

  excluirSetor(setor: SetorListItem): void {
    if (!this.canDeleteSetor || setor.isDirecaoIc) {
      return;
    }

    const ok = window.confirm(
      `Excluir o setor "${setor.nome}"? Esta ação não pode ser desfeita.`,
    );
    if (!ok) {
      return;
    }

    this.error.set(null);
    this.api.deleteSetor(setor.id).subscribe({
      next: () => this.reload(),
      error: (err: { error?: { message?: string } }) =>
        this.error.set(err.error?.message ?? 'Operação inválida ao excluir o setor.'),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.getEstruturaOrganizacional().subscribe({
      next: (data) => {
        this.estrutura.set(data);
        this.expandedIds.set(new Set((data.nucleos ?? []).map((n) => n.id)));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar a estrutura organizacional.');
        this.loading.set(false);
      },
    });
  }
}
