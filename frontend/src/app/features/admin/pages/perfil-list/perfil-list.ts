import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciColumn,
  PciFilterField,
  PciFilterValues,
  PciListPageComponent,
  PciRowAction,
  PciSortChange,
  filterRowsByPanelValues,
  filterTableRowsByQuickSearch,
  sortTableRows,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import {
  DEFAULT_PAGE_SIZE,
  PAGE_SIZE_OPTIONS,
  PageSizeOption,
  PerfilListItem,
} from '../../models/admin.models';

type PerfilRow = {
  id: string;
  nome: string;
  descricao: string;
  permissoes: string;
  status: string;
};

@Component({
  selector: 'app-perfil-list',
  imports: [CommonModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './perfil-list.html',
  styleUrl: './perfil-list.scss',
})
export class PerfilList implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal<PageSizeOption>(DEFAULT_PAGE_SIZE);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly allRows = signal<PerfilRow[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly sort = signal<PciSortChange<PerfilRow> | null>(null);
  readonly canCreate = this.auth.hasPermission('perfis.criar');
  readonly canEdit = this.auth.hasPermission('perfis.editar');

  readonly filterFields: PciFilterField[] = [
    {
      key: 'nome',
      label: 'Nome',
      type: 'text',
      placeholder: 'Buscar por nome',
      columnKey: 'nome',
    },
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      columnKey: 'status',
      options: [
        { label: 'Ativo', value: 'Ativo' },
        { label: 'Inativo', value: 'Inativo' },
      ],
    },
  ];

  readonly columns: PciColumn<PerfilRow>[] = [
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'descricao', label: 'Descrição', width: '18rem' },
    { key: 'status', label: 'Status' },
  ];

  readonly rowActions = computed<PciRowAction<PerfilRow>[]>(() => {
    const actions: PciRowAction<PerfilRow>[] = [];
    if (this.canEdit) {
      actions.push({ id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' });
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
    this.reload();
  }

  goCreate(): void {
    void this.router.navigateByUrl('/perfis/novo');
  }

  onRowAction(event: { action: string; row: PerfilRow }): void {
    if (event.action === 'edit') {
      void this.router.navigateByUrl(`/perfis/editar/${event.row.id}`);
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

  onSortChange(sort: PciSortChange<PerfilRow> | null): void {
    this.sort.set(sort);
  }

  onPageSizeChange(size: number): void {
    const parsed = size as PageSizeOption;
    this.pageSize.set(PAGE_SIZE_OPTIONS.includes(parsed) ? parsed : DEFAULT_PAGE_SIZE);
    this.page.set(1);
  }

  private reload(): void {
    this.loading.set(true);
    this.api.listPerfis({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        this.allRows.set(
          result.items.map((p: PerfilListItem) => ({
            id: p.id,
            nome: this.displayPerfilNome(p.nome),
            descricao: p.descricao || '—',
            permissoes: String(p.quantidadePermissoes),
            status: p.ativo ? 'Ativo' : 'Inativo',
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar os perfis.');
        this.loading.set(false);
      },
    });
  }

  /** Só rótulo de exibição nesta listagem — o `Nome` real do perfil no banco continua "Chefe de
   * Setor" (perfil também cobre chefia de núcleo, mas renomear o dado em si fica pra depois). */
  private displayPerfilNome(nome: string): string {
    return nome === 'Chefe de Setor' ? 'Chefe de Setor/Núcleo' : nome;
  }
}
