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
  filterRowsByPanelValues,
  filterTableRowsByQuickSearch,
} from '@davillawitte/pci-design-system';

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
  codigo: string;
  descricao: string;
  permissoes: string;
  status: string;
  sistema: string;
};

@Component({
  selector: 'app-perfis-page',
  imports: [CommonModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './perfis-page.component.html',
})
export class PerfisPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
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

  readonly filterFields: PciFilterField[] = [
    {
      key: 'nome',
      label: 'Nome',
      type: 'text',
      placeholder: 'Buscar por nome',
      columnKey: 'nome',
    },
    {
      key: 'codigo',
      label: 'Código',
      type: 'text',
      placeholder: 'Buscar por código',
      columnKey: 'codigo',
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
    {
      key: 'sistema',
      label: 'Sistema',
      type: 'select',
      columnKey: 'sistema',
      options: [
        { label: 'Sim', value: 'Sim' },
        { label: 'Não', value: 'Não' },
      ],
    },
  ];

  readonly columns: PciColumn<PerfilRow>[] = [
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'codigo', label: 'Código', sortable: true },
    { key: 'descricao', label: 'Descrição' },
    { key: 'permissoes', label: 'Qtd. permissões' },
    { key: 'sistema', label: 'Sistema' },
    { key: 'status', label: 'Status' },
  ];

  readonly rowActions: PciRowAction<PerfilRow>[] = [
    { id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' },
  ];

  readonly filteredRows = computed(() => {
    const byPanel = filterRowsByPanelValues(
      this.allRows(),
      this.filterValues(),
      this.filterFields,
    );
    return filterTableRowsByQuickSearch(byPanel, this.columns, this.searchTerm());
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
            nome: p.nome,
            codigo: p.codigo,
            descricao: p.descricao || '—',
            permissoes: String(p.quantidadePermissoes),
            sistema: p.sistema ? 'Sim' : 'Não',
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
}
