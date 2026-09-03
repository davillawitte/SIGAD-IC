import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciColumn,
  PciFilterField,
  PciFilterValues,
  PciFeedbackModalService,
  PciListPageComponent,
  PciRowAction,
  PciSortChange,
  filterRowsByPanelValues,
  filterTableRowsByQuickSearch,
  sortTableRows,
} from '@davillawitte/pci-design-system';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { AFASTAMENTOS_ROUTE_PAGES } from '../../afastamentos.routes.meta';
import { AfastamentosApiService } from '../../services/afastamentos-api.service';

type AfastamentoEscopo = 'setor' | 'institucional';

type AfastamentoRow = {
  id: string;
  servidor: string;
  matricula: string;
  setorId?: string | null;
  nucleoId?: string | null;
  setor: string;
  periodo: string;
  tipo: string;
  sei: string;
  observacao: string;
};

@Component({
  selector: 'app-afastamento-list',
  imports: [CommonModule, MatDialogModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './afastamento-list.html',
  styleUrl: './afastamento-list.scss',
})
export class AfastamentoList implements OnInit {
  private readonly api = inject(AfastamentosApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly routePages = AFASTAMENTOS_ROUTE_PAGES;
  readonly escopo = signal<AfastamentoEscopo>('setor');
  readonly isInstitucional = computed(() => this.escopo() === 'institucional');
  readonly pageTitle = computed(() =>
    this.isInstitucional() ? 'Afastamentos Institucionais' : 'Afastamentos',
  );
  readonly pageDescription = computed(() =>
    this.isInstitucional()
      ? 'Consulta dos afastamentos de todos os setores (exceto a Direção do IC). Somente visualização.'
      : 'Cadastre férias, licenças e afastamentos dos servidores do seu setor.',
  );
  readonly listPath = computed(() =>
    this.isInstitucional() ? '/afastamentos-institucionais' : '/afastamentos',
  );

  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly allRows = signal<AfastamentoRow[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly sort = signal<PciSortChange<AfastamentoRow> | null>(null);
  readonly canCreate = computed(
    () => !this.isInstitucional() && this.auth.hasPermission('afastamentos.criar'),
  );

  readonly filterFields: PciFilterField[] = [
    { key: 'servidor', label: 'Servidor', type: 'text', columnKey: 'servidor' },
    { key: 'matricula', label: 'Matrícula', type: 'text', columnKey: 'matricula' },
    {
      key: 'tipo',
      label: 'Tipo',
      type: 'select',
      columnKey: 'tipo',
      options: [
        { label: 'Férias', value: 'FR — Férias' },
        { label: 'Licença Médica', value: 'LM — Licença Médica' },
        { label: 'Licença Prêmio', value: 'LP — Licença Prêmio' },
        { label: 'Licença Outros', value: 'LO — Licença Outros' },
      ],
    },
  ];

  readonly columns: PciColumn<AfastamentoRow>[] = [
    { key: 'servidor', label: 'Servidor', sortable: true },
    { key: 'matricula', label: 'Matrícula' },
    { key: 'setor', label: 'Setor' },
    { key: 'periodo', label: 'Período' },
    { key: 'tipo', label: 'Tipo' },
    { key: 'sei', label: 'SEI', width: '8rem' },
    { key: 'observacao', label: 'Observação', width: '18rem' },
  ];

  readonly rowActions = computed<PciRowAction<AfastamentoRow>[]>(() => {
    if (this.isInstitucional()) {
      return [];
    }
    const actions: PciRowAction<AfastamentoRow>[] = [];
    if (this.auth.hasPermission('afastamentos.editar')) {
      actions.push({
        id: 'edit',
        label: 'Editar',
        icon: 'edit',
        placement: 'inline',
        disabled: (row) =>
          !this.auth.canAccessLotacao('afastamentos.editar', 'afastamentos', row.setorId, row.nucleoId),
      });
    }
    if (this.auth.hasPermission('afastamentos.excluir')) {
      actions.push({
        id: 'delete',
        label: 'Excluir',
        icon: 'trash',
        placement: 'inline',
        disabled: (row) =>
          !this.auth.canAccessLotacao('afastamentos.excluir', 'afastamentos', row.setorId, row.nucleoId),
      });
    }
    return actions;
  });

  readonly filteredRows = computed(() => {
    const byPanel = filterRowsByPanelValues(
      this.allRows(),
      this.filterValues(),
      this.filterFields,
    );
    const searched = filterTableRowsByQuickSearch(byPanel, this.columns, this.searchTerm());
    const sort = this.sort();
    return sort ? sortTableRows(searched, sort, this.columns) : searched;
  });

  readonly pagedRows = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filteredRows().slice(start, start + this.pageSize());
  });

  ngOnInit(): void {
    const modo = (this.route.snapshot.data['escopo'] as AfastamentoEscopo | undefined) ?? 'setor';
    this.escopo.set(modo === 'institucional' ? 'institucional' : 'setor');
    this.reload();
  }

  goCreate(): void {
    void this.router.navigateByUrl('/afastamentos/novo');
  }

  onRowAction(event: { action: string; row: AfastamentoRow }): void {
    if (this.isInstitucional()) {
      return;
    }
    const { setorId, nucleoId } = event.row;
    if (
      (event.action === 'edit' &&
        !this.auth.canAccessLotacao('afastamentos.editar', 'afastamentos', setorId, nucleoId)) ||
      (event.action === 'delete' &&
        !this.auth.canAccessLotacao('afastamentos.excluir', 'afastamentos', setorId, nucleoId))
    ) {
      this.error.set('Sem permissão para alterar afastamentos de outra lotação.');
      return;
    }
    if (event.action === 'edit') {
      void this.router.navigateByUrl(`/afastamentos/editar/${event.row.id}`);
      return;
    }
    if (event.action === 'delete') {
      openConfirmDialog(this.dialog, {
        title: 'Excluir afastamento',
        message: `Excluir afastamento de ${event.row.servidor}?`,
        confirmLabel: 'Excluir',
        danger: true,
      })
        .pipe(filter(Boolean))
        .subscribe(() => {
          this.api.delete(event.row.id).subscribe({
            next: () => {
              this.feedback.showSuccess('Afastamento excluído com sucesso.');
              this.reload();
            },
            error: (err: { error?: { message?: string } }) =>
              this.error.set(err.error?.message ?? 'Não foi possível excluir.'),
          });
        });
    }
  }

  onFilterApply(values: PciFilterValues): void {
    this.filterValues.set(values);
    this.page.set(1);
  }

  onFilterClear(): void {
    this.filterValues.set({});
    this.page.set(1);
  }

  onSearchChange(term: string): void {
    this.searchTerm.set(term);
    this.page.set(1);
  }

  onSortChange(sort: PciSortChange<AfastamentoRow> | null): void {
    this.sort.set(sort);
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.list({ escopo: this.escopo() }).subscribe({
      next: (items) => {
        this.allRows.set(
          items.map((a) => ({
            id: a.id,
            servidor: a.servidorNome,
            matricula: a.matricula,
            setorId: a.setorId,
            nucleoId: a.nucleoId,
            setor: a.setorId
              ? `${a.setorSigla} — ${a.setorNome}`
              : a.nucleoId
                ? `${a.nucleoSigla} — ${a.nucleoNome} (núcleo)`
                : '—',
            periodo: `${this.fmt(a.dataInicio)} a ${this.fmt(a.dataFim)}`,
            tipo: `${a.tipoOcorrenciaCodigo} — ${a.tipoOcorrenciaNome}`,
            sei: a.sei || '—',
            observacao: a.observacao || '—',
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar os afastamentos.');
        this.loading.set(false);
      },
    });
  }

  private fmt(iso: string): string {
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }
}
