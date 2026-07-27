import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciColumn,
  PciFeedbackModalService,
  PciFilterField,
  PciFilterValues,
  PciListPageComponent,
  PciRowAction,
  PciSortChange,
  PciToastService,
  filterRowsByPanelValues,
  filterTableRowsByQuickSearch,
  sortTableRows,
} from '@davillawitte/pci-design-system';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import {
  DEFAULT_PAGE_SIZE,
  PAGE_SIZE_OPTIONS,
  PageSizeOption,
  ServidorExclusaoImpacto,
} from '../../models/admin.models';

type ServidorRow = {
  id: string;
  nome: string;
  matricula: string;
  cargo: string;
  setor: string;
  setorId: string;
  email: string;
  status: string;
};

@Component({
  selector: 'app-servidor-list',
  imports: [CommonModule, MatDialogModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './servidor-list.html',
  styleUrl: './servidor-list.scss',
})
export class ServidorList implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly toast = inject(PciToastService);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal<PageSizeOption>(DEFAULT_PAGE_SIZE);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly allRows = signal<ServidorRow[]>([]);
  readonly cargoOptions = signal<{ label: string; value: string }[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly sort = signal<PciSortChange<ServidorRow> | null>(null);
  readonly canCreate = this.auth.hasPermission('servidores.criar');
  readonly canEdit = this.auth.hasPermission('servidores.editar');
  readonly canDelete = this.auth.hasPermission('servidores.excluir');

  readonly filterFields = computed<PciFilterField[]>(() => [
    {
      key: 'nome',
      label: 'Nome',
      type: 'text',
      placeholder: 'Buscar por nome',
      columnKey: 'nome',
    },
    {
      key: 'matricula',
      label: 'Matrícula',
      type: 'text',
      placeholder: 'Buscar por matrícula',
      columnKey: 'matricula',
    },
    {
      key: 'cargo',
      label: 'Cargo',
      type: 'select',
      columnKey: 'cargo',
      options: this.cargoOptions(),
    },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      columnKey: 'status',
      options: [
        { label: 'Ativo', value: 'Ativo' },
        { label: 'Afastado', value: 'Afastado' },
        { label: 'Cedido', value: 'Cedido' },
      ],
    },
  ]);

  readonly columns: PciColumn<ServidorRow>[] = [
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'matricula', label: 'Matrícula', sortable: true },
    { key: 'cargo', label: 'Cargo' },
    { key: 'setor', label: 'Setor' },
    { key: 'email', label: 'E-mail' },
    { key: 'status', label: 'Status' },
  ];

  readonly rowActions = computed<PciRowAction<ServidorRow>[]>(() => {
    const actions: PciRowAction<ServidorRow>[] = [];
    if (this.canEdit) {
      actions.push({
        id: 'edit',
        label: 'Editar',
        icon: 'edit',
        placement: 'inline',
        disabled: (row) => !this.auth.canAccess('servidores.editar', row.setorId),
      });
    }
    if (this.canDelete) {
      actions.push({
        id: 'delete',
        label: 'Excluir',
        icon: 'trash',
        placement: 'inline',
        variant: 'danger',
        disabled: (row) => !this.auth.canAccess('servidores.excluir', row.setorId),
      });
    }
    return actions;
  });

  readonly filteredRows = computed(() => {
    const byPanel = filterRowsByPanelValues(
      this.allRows(),
      this.filterValues(),
      this.filterFields(),
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
    this.api.listCargos().subscribe({
      next: (cargos) =>
        this.cargoOptions.set(cargos.map((c) => ({ label: c.nome, value: c.nome }))),
    });
    this.reload();
  }

  goCreate(): void {
    void this.router.navigateByUrl('/servidores/novo');
  }

  onRowAction(event: { action: string; row: ServidorRow }): void {
    if (event.action === 'edit') {
      if (!this.auth.canAccess('servidores.editar', event.row.setorId)) {
        return;
      }
      void this.router.navigateByUrl(`/servidores/editar/${event.row.id}`);
      return;
    }
    if (event.action === 'delete') {
      if (!this.auth.canAccess('servidores.excluir', event.row.setorId)) {
        return;
      }
      this.excluir(event.row);
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

  onSortChange(sort: PciSortChange<ServidorRow> | null): void {
    this.sort.set(sort);
  }

  onPageSizeChange(size: number): void {
    const parsed = size as PageSizeOption;
    this.pageSize.set(PAGE_SIZE_OPTIONS.includes(parsed) ? parsed : DEFAULT_PAGE_SIZE);
    this.page.set(1);
  }

  private excluir(row: ServidorRow): void {
    if (!this.canDelete) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.api.getServidorExclusaoImpacto(row.id).subscribe({
      next: (impacto) => {
        this.loading.set(false);
        if (!impacto.podeExcluir) {
          openConfirmDialog(this.dialog, {
            title: 'Não é possível excluir',
            message: this.buildBloqueioMessage(row, impacto),
            confirmLabel: 'Entendi',
            cancelLabel: 'Fechar',
          }).subscribe();
          return;
        }

        const nucleoHint =
          impacto.nucleosComoChefe > 0
            ? `\n\nObs.: este servidor é chefe de ${impacto.nucleosComoChefe} núcleo(s); o vínculo será removido automaticamente.`
            : '';

        openConfirmDialog(this.dialog, {
          title: 'Excluir servidor',
          message: `Excluir o servidor "${row.nome}" (${row.matricula})? Esta ação não pode ser desfeita.${nucleoHint}`,
          confirmLabel: 'Excluir',
          danger: true,
        })
          .pipe(filter(Boolean))
          .subscribe(() => {
            this.loading.set(true);
            this.api.deleteServidor(row.id).subscribe({
              next: () => {
                this.loading.set(false);
                this.feedback.showSuccess('Servidor excluído com sucesso.');
                this.reload();
              },
              error: (err: { error?: { message?: string } }) => {
                const msg = err.error?.message ?? 'Não foi possível excluir o servidor.';
                this.error.set(msg);
                this.toast.showError(msg);
                this.loading.set(false);
              },
            });
          });
      },
      error: (err: { error?: { message?: string } }) => {
        const msg = err.error?.message ?? 'Não foi possível verificar os vínculos do servidor.';
        this.error.set(msg);
        this.toast.showError(msg);
        this.loading.set(false);
      },
    });
  }

  private buildBloqueioMessage(row: ServidorRow, impacto: ServidorExclusaoImpacto): string {
    const linhas: string[] = [
      `O servidor "${row.nome}" possui vínculos que impedem a exclusão. Remova-os manualmente antes de tentar novamente.`,
      '',
    ];
    if (impacto.escalas > 0) {
      linhas.push(`• Escalas: ${impacto.escalas}`);
    }
    if (impacto.afastamentos > 0) {
      linhas.push(`• Afastamentos: ${impacto.afastamentos}`);
    }
    if (impacto.chefias > 0) {
      linhas.push(`• Chefias de setor: ${impacto.chefias}`);
    }
    if (impacto.usuarios > 0) {
      linhas.push(`• Usuário do sistema: ${impacto.usuarios}`);
    }
    if (impacto.nucleosComoChefe > 0) {
      linhas.push(
        `• Núcleos como chefe: ${impacto.nucleosComoChefe} (informativo — não bloqueia)`,
      );
    }
    return linhas.join('\n');
  }

  private reload(): void {
    this.loading.set(true);
    this.api.listServidores(false).subscribe({
      next: (items) => {
        this.allRows.set(
          items.map((s) => ({
            id: s.id,
            nome: s.nome,
            matricula: s.matricula,
            cargo: s.cargo,
            setor: s.setorNome,
            setorId: s.setorId,
            email: s.email ?? '',
            status: s.status,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar os servidores.');
        this.loading.set(false);
      },
    });
  }
}
