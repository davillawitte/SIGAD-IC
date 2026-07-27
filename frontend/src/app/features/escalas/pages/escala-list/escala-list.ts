import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciBreadcrumbService,
  PciButtonComponent,
  PciDropdownMenuComponent,
  PciDropdownPanelDirective,
  PciDropdownTriggerDirective,
  PciFeedbackModalService,
  PciFilterField,
  PciFilterPanelComponent,
  PciFilterValues,
  PciIconButtonComponent,
  PciIconComponent,
  PciLayoutBreadcrumbService,
  PciPageHeaderComponent,
  PciStackComponent,
  PciToastService,
  PciTooltipChildDirective,
  PciTooltipComponent,
} from '@davillawitte/pci-design-system';
import type { PciBadgeVariant, PciIconName } from '@davillawitte/pci-design-system';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { ESCALAS_ROUTE_PAGES } from '../../escalas-route-pages';
import { EscalasApiService } from '../../services/escalas-api.service';
import type { EscalaListItem, SolicitacaoDevolucaoEscala, StatusEscala } from '../../models/escalas.models';
import { statusEscalaLabel } from '../../models/escalas.models';

type EscalaEscopo = 'setor' | 'institucional';

type EscalaRow = {
  id: string;
  setorId: string;
  periodoReferencia: string;
  setor: string;
  status: StatusEscala;
  publicadaEm: string;
  criadoEm: string;
  criadoPor: string;
};

type InlineAction = {
  id: string;
  label: string;
  icon: PciIconName;
};

@Component({
  selector: 'app-escala-list',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciDropdownMenuComponent,
    PciDropdownPanelDirective,
    PciDropdownTriggerDirective,
    PciFilterPanelComponent,
    PciIconButtonComponent,
    PciIconComponent,
    PciPageHeaderComponent,
    PciStackComponent,
    PciTooltipComponent,
    PciTooltipChildDirective,
  ],
  templateUrl: './escala-list.html',
  styleUrl: './escala-list.scss',
})
export class EscalaList implements OnInit, OnDestroy {
  private readonly api = inject(EscalasApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(PciToastService);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly dialog = inject(MatDialog);
  private readonly breadcrumb = inject(PciBreadcrumbService);
  private readonly layoutBreadcrumb = inject(PciLayoutBreadcrumbService);
  readonly statusLabel = statusEscalaLabel;

  readonly escopo = signal<EscalaEscopo>('setor');
  readonly isInstitucional = computed(() => this.escopo() === 'institucional');
  readonly pageTitle = computed(() =>
    this.isInstitucional() ? 'Escalas institucionais' : 'Escalas',
  );
  readonly pageDescription = computed(() =>
    this.isInstitucional()
      ? 'Consulta das escalas de todos os setores (exceto a Direção do IC). Somente visualização e devolução.'
      : 'Gerencie as escalas mensais do seu setor.',
  );
  readonly listBasePath = computed(() =>
    this.isInstitucional() ? '/escalas-institucionais' : '/escalas',
  );

  readonly routePages = ESCALAS_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<EscalaRow[]>([]);
  readonly totalItems = signal(0);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly canCreate = computed(
    () => !this.isInstitucional() && this.auth.hasPermission('escalas.criar'),
  );
  readonly canDevolver = computed(
    () => this.isInstitucional() && this.auth.hasPermission('escalas.devolver'),
  );
  readonly devolucoes = signal<SolicitacaoDevolucaoEscala[]>([]);
  readonly devolucaoWorking = signal(false);
  readonly openMenuRowId = signal<string | null>(null);

  readonly filterFields: PciFilterField[] = [
    {
      key: 'mes',
      label: 'Mês',
      type: 'select',
      options: [
        { label: 'Janeiro', value: '1' },
        { label: 'Fevereiro', value: '2' },
        { label: 'Março', value: '3' },
        { label: 'Abril', value: '4' },
        { label: 'Maio', value: '5' },
        { label: 'Junho', value: '6' },
        { label: 'Julho', value: '7' },
        { label: 'Agosto', value: '8' },
        { label: 'Setembro', value: '9' },
        { label: 'Outubro', value: '10' },
        { label: 'Novembro', value: '11' },
        { label: 'Dezembro', value: '12' },
      ],
    },
    { key: 'ano', label: 'Ano', type: 'text', placeholder: 'Ex.: 2026' },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      options: [
        { label: 'Rascunho', value: 'Rascunho' },
        { label: 'Finalizada', value: 'Finalizada' },
        { label: 'Publicada', value: 'Publicada' },
        { label: 'Devolução solicitada', value: 'DevolucaoSolicitada' },
      ],
    },
  ];

  readonly totalPages = computed(() => {
    const size = this.pageSize();
    const total = this.totalItems();
    return total <= 0 ? 0 : Math.ceil(total / size);
  });

  readonly rangeLabel = computed(() => {
    const total = this.totalItems();
    if (total <= 0) return '0 resultados';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(this.page() * this.pageSize(), total);
    return `${start}–${end} de ${total}`;
  });

  ngOnInit(): void {
    const modo = (this.route.snapshot.data['escopo'] as EscalaEscopo | undefined) ?? 'setor';
    this.escopo.set(modo === 'institucional' ? 'institucional' : 'setor');
    this.layoutBreadcrumb.setItems(
      this.breadcrumb.buildFromRoutes(this.routePages, this.listBasePath()),
    );
    this.reload();
    if (this.canDevolver()) {
      this.reloadDevolucoes();
    }
  }

  ngOnDestroy(): void {
    this.layoutBreadcrumb.clear();
  }

  statusVariant(status: StatusEscala): PciBadgeVariant {
    if (status === 'Publicada') return 'success';
    if (status === 'Finalizada') return 'secondary';
    return 'warning';
  }

  inlineActions(row: EscalaRow): InlineAction[] {
    const actions: InlineAction[] = [
      { id: 'view', label: 'Visualizar', icon: 'eye' },
    ];
    if (this.isInstitucional()) {
      return actions;
    }
    if (
      this.auth.canAccess('escalas.editar', row.setorId) &&
      (row.status === 'Rascunho' || row.status === 'Finalizada')
    ) {
      actions.push({ id: 'edit', label: 'Editar', icon: 'edit' });
    }
    if (this.auth.canAccess('escalas.publicar', row.setorId) && row.status === 'Finalizada') {
      actions.push({ id: 'publish', label: 'Publicar', icon: 'check' });
    }
    return actions;
  }

  menuActions(row: EscalaRow): InlineAction[] {
    const actions: InlineAction[] = [];
    if (this.auth.hasPermission('escalas.exportar')) {
      actions.push(
        { id: 'pdf-h', label: 'PDF horizontal', icon: 'download' },
        { id: 'pdf-v', label: 'PDF vertical', icon: 'download' },
      );
    }
    if (
      !this.isInstitucional() &&
      this.auth.canAccess('escalas.excluir', row.setorId) &&
      (row.status === 'Rascunho' || row.status === 'Finalizada')
    ) {
      actions.push({ id: 'delete', label: 'Excluir', icon: 'trash' });
    }
    return actions;
  }

  onCreate(): void {
    void this.router.navigateByUrl('/escalas/nova');
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.totalPages() > 0 && page > this.totalPages())) return;
    this.page.set(page);
    this.reload();
  }

  onPageSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value <= 0) return;
    this.pageSize.set(value);
    this.page.set(1);
    this.reload();
  }

  onFilterApply(values: PciFilterValues): void {
    this.filterValues.set(values);
    this.page.set(1);
    this.reload();
  }

  onFilterClear(): void {
    this.filterValues.set({});
    this.page.set(1);
    this.reload();
  }

  onSearchInput(event: Event): void {
    const term = (event.target as HTMLInputElement).value ?? '';
    this.searchTerm.set(term);
    this.page.set(1);
    this.reload();
  }

  onRowAction(action: string, row: EscalaRow): void {
    this.openMenuRowId.set(null);
    const id = row.id;
    switch (action) {
      case 'view':
        void this.router.navigateByUrl(`/escalas/${id}`);
        break;
      case 'edit':
        void this.router.navigateByUrl(`/escalas/${id}/editar`);
        break;
      case 'publish':
        this.publicar(row);
        break;
      case 'pdf-h':
        this.downloadPdf(id, 'horizontal');
        break;
      case 'pdf-v':
        this.downloadPdf(id, 'vertical');
        break;
      case 'delete':
        this.excluir(row);
        break;
    }
  }

  private excluir(row: EscalaRow): void {
    if (row.status !== 'Rascunho' && row.status !== 'Finalizada') {
      const msg = 'Somente escalas em rascunho ou finalizadas podem ser excluídas.';
      this.error.set(msg);
      this.toast.showError(msg);
      return;
    }
    openConfirmDialog(this.dialog, {
      title: 'Excluir escala',
      message: `Excluir a escala ${row.periodoReferencia} (${row.setor})? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(filter(Boolean))
      .subscribe(() => {
        this.loading.set(true);
        this.error.set(null);
        this.api.delete(row.id).subscribe({
          next: () => {
            this.loading.set(false);
            this.feedback.showSuccess('Escala excluída com sucesso.');
            this.reload();
          },
          error: (err: { error?: { message?: string } }) => {
            const msg = err.error?.message ?? 'Não foi possível excluir a escala.';
            this.error.set(msg);
            this.toast.showError(msg);
            this.loading.set(false);
          },
        });
      });
  }

  aprovarDevolucao(item: SolicitacaoDevolucaoEscala): void {
    this.devolucaoWorking.set(true);
    this.api.aprovarDevolucao(item.id).subscribe({
      next: () => {
        this.devolucaoWorking.set(false);
        this.feedback.showSuccess('Devolução aprovada.');
        this.reloadDevolucoes();
        this.reload();
      },
      error: (err: { error?: { message?: string } }) => {
        const msg = err.error?.message ?? 'Não foi possível aprovar a devolução.';
        this.error.set(msg);
        this.toast.showError(msg);
        this.devolucaoWorking.set(false);
      },
    });
  }

  recusarDevolucao(item: SolicitacaoDevolucaoEscala): void {
    this.devolucaoWorking.set(true);
    this.api.recusarDevolucao(item.id).subscribe({
      next: () => {
        this.devolucaoWorking.set(false);
        this.feedback.showSuccess('Devolução recusada.');
        this.reloadDevolucoes();
        this.reload();
      },
      error: (err: { error?: { message?: string } }) => {
        const msg = err.error?.message ?? 'Não foi possível recusar a devolução.';
        this.error.set(msg);
        this.toast.showError(msg);
        this.devolucaoWorking.set(false);
      },
    });
  }

  private publicar(row: EscalaRow): void {
    if (row.status !== 'Finalizada') {
      const msg = 'Somente escalas finalizadas podem ser publicadas.';
      this.error.set(msg);
      this.toast.showError(msg);
      return;
    }

    openConfirmDialog(this.dialog, {
      title: 'Publicar escala',
      message: 'Tem certeza que deseja publicar esta escala?',
      confirmLabel: 'Publicar',
    })
      .pipe(filter(Boolean))
      .subscribe(() => {
        this.loading.set(true);
        this.error.set(null);
        this.api.publicar(row.id).subscribe({
          next: () => {
            this.loading.set(false);
            this.feedback.showSuccess('Escala publicada com sucesso.');
            this.reload();
          },
          error: (err: { error?: { message?: string } }) => {
            const msg = err.error?.message ?? 'Não foi possível publicar a escala.';
            this.error.set(msg);
            this.toast.showError(msg);
            this.loading.set(false);
          },
        });
      });
  }

  private downloadPdf(id: string, layout: 'horizontal' | 'vertical'): void {
    this.api.downloadPdf(id, layout).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `escala-${layout}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.feedback.showSuccess('PDF gerado com sucesso.');
      },
      error: () => {
        const msg = 'Não foi possível exportar o PDF.';
        this.error.set(msg);
        this.toast.showError(msg);
      },
    });
  }

  private reloadDevolucoes(): void {
    if (!this.canDevolver) {
      this.devolucoes.set([]);
      return;
    }
    this.api.listDevolucoesPendentes().subscribe({
      next: (items) => this.devolucoes.set(items),
      error: () => this.devolucoes.set([]),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    const filters = this.filterValues();
    const ano = Number(filters['ano'] || '');
    const mes = Number(filters['mes'] || '');

    this.api
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.searchTerm() || undefined,
        status: (filters['status'] as string) || undefined,
        ano: Number.isFinite(ano) && ano > 0 ? ano : undefined,
        mes: Number.isFinite(mes) && mes >= 1 && mes <= 12 ? mes : undefined,
        escopo: this.escopo(),
      })
      .subscribe({
        next: (result) => {
          this.rows.set((result.items ?? []).map((item) => this.toRow(item)));
          this.totalItems.set(result.totalItems);
          this.loading.set(false);
        },
        error: () => {
          const msg = 'Não foi possível carregar as escalas.';
          this.error.set(msg);
          this.toast.showError(msg);
          this.loading.set(false);
        },
      });
  }

  private toRow(item: EscalaListItem): EscalaRow {
    const mesNome = [
      '',
      'Janeiro',
      'Fevereiro',
      'Março',
      'Abril',
      'Maio',
      'Junho',
      'Julho',
      'Agosto',
      'Setembro',
      'Outubro',
      'Novembro',
      'Dezembro',
    ][item.mes] ?? String(item.mes);
    return {
      id: item.id,
      setorId: item.setorId,
      periodoReferencia: `${mesNome}/${item.ano}`,
      setor: `${item.setorSigla} — ${item.setorNome}`,
      status: item.status,
      publicadaEm:
        (item.status === 'Publicada' || item.status === 'DevolucaoSolicitada') && item.publicadaEm
          ? this.fmtDateTime(item.publicadaEm)
          : '—',
      criadoEm: this.fmtDateTime(item.createdAt),
      criadoPor: item.createdBy ?? '—',
    };
  }

  private fmtDateTime(iso: string): string {
    const d = new Date(iso);
    return Number.isNaN(d.getTime()) ? iso : d.toLocaleString('pt-BR');
  }
}
