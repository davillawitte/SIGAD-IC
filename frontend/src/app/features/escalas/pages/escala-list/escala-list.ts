import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBreadcrumbService,
  PciButtonComponent,
  PciColumn,
  PciFeedbackModalService,
  PciFilterField,
  PciFilterValues,
  PciLayoutBreadcrumbService,
  PciListPageComponent,
  PciRowAction,
  PciStackComponent,
  PciToastService,
} from '@davillawitte/pci-design-system';
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
  status: string;
  statusRaw: StatusEscala;
  publicadaEm: string;
  criadoEm: string;
  criadoPor: string;
};

@Component({
  selector: 'app-escala-list',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciListPageComponent,
    PciStackComponent,
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

  readonly escopo = signal<EscalaEscopo>('setor');
  readonly isInstitucional = computed(() => this.escopo() === 'institucional');
  readonly pageTitle = computed(() =>
    this.isInstitucional() ? 'Escalas Institucionais' : 'Escalas',
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
  readonly canExport = computed(
    () =>
      this.auth.hasPermission('escalas.exportar') ||
      (this.isInstitucional() && this.auth.hasPermission('escalas.listar')),
  );
  readonly devolucoes = signal<SolicitacaoDevolucaoEscala[]>([]);
  readonly devolucaoWorking = signal(false);

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

  readonly columns: PciColumn<EscalaRow>[] = [
    { key: 'periodoReferencia', label: 'Período de Referência', sortable: false },
    { key: 'setor', label: 'Setor', sortable: false },
    { key: 'status', label: 'Status', sortable: false },
    { key: 'publicadaEm', label: 'Publicada em', sortable: false },
    { key: 'criadoEm', label: 'Criada em', sortable: false },
    { key: 'criadoPor', label: 'Responsável', sortable: false },
  ];

  readonly rowActions = computed<PciRowAction<EscalaRow>[]>(() => {
    const actions: PciRowAction<EscalaRow>[] = [
      { id: 'view', label: 'Visualizar', icon: 'eye', placement: 'inline' },
    ];

    if (!this.isInstitucional()) {
      actions.push(
        {
          id: 'edit',
          label: 'Editar',
          icon: 'edit',
          placement: 'inline',
          hidden: (row) =>
            !this.auth.canAccess('escalas.editar', row.setorId) ||
            (row.statusRaw !== 'Rascunho' && row.statusRaw !== 'Finalizada'),
        },
        {
          id: 'publish',
          label: 'Publicar',
          icon: 'check',
          placement: 'inline',
          hidden: (row) =>
            !this.auth.canAccess('escalas.publicar', row.setorId) || row.statusRaw !== 'Finalizada',
        },
      );
    }

    if (this.canExport()) {
      actions.push(
        { id: 'pdf-h', label: 'PDF horizontal', icon: 'download', placement: 'menu' },
        { id: 'pdf-v', label: 'PDF vertical', icon: 'download', placement: 'menu' },
      );
    }

    if (!this.isInstitucional()) {
      actions.push({
        id: 'delete',
        label: 'Excluir',
        icon: 'trash',
        placement: 'menu',
        variant: 'danger',
        hidden: (row) =>
          !this.auth.canAccess('escalas.excluir', row.setorId) ||
          (row.statusRaw !== 'Rascunho' && row.statusRaw !== 'Finalizada'),
      });
    }

    return actions;
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

  onCreate(): void {
    void this.router.navigateByUrl('/escalas/nova');
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.reload();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
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

  onSearchChange(term: string): void {
    this.searchTerm.set(term);
    this.page.set(1);
    this.reload();
  }

  onRowAction(event: { action: string; row: EscalaRow }): void {
    const { action, row } = event;
    const id = row.id;
    switch (action) {
      case 'view':
        void this.router.navigateByUrl(`${this.listBasePath()}/${id}`);
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

  private excluir(row: EscalaRow): void {
    if (row.statusRaw !== 'Rascunho' && row.statusRaw !== 'Finalizada') {
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

  private publicar(row: EscalaRow): void {
    if (row.statusRaw !== 'Finalizada') {
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
      error: (err: { error?: { message?: string } }) => {
        const msg = err.error?.message ?? 'Não foi possível exportar o PDF.';
        this.error.set(msg);
        this.toast.showError(msg);
      },
    });
  }

  private reloadDevolucoes(): void {
    if (!this.canDevolver()) {
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
      status: statusEscalaLabel(item.status),
      statusRaw: item.status,
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
