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
import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS, PageSizeOption } from '../../models/admin.models';

type UsuarioRow = {
  id: string;
  login: string;
  nomeServidor: string;
  matricula: string;
  status: string;
  perfis: string;
};

@Component({
  selector: 'app-usuarios-page',
  imports: [CommonModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './usuarios-page.component.html',
})
export class UsuariosPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal<PageSizeOption>(DEFAULT_PAGE_SIZE);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly allRows = signal<UsuarioRow[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');

  readonly filterFields: PciFilterField[] = [
    {
      key: 'login',
      label: 'Login',
      type: 'text',
      placeholder: 'Buscar por login',
      columnKey: 'login',
    },
    {
      key: 'nomeServidor',
      label: 'Servidor',
      type: 'text',
      placeholder: 'Buscar por nome',
      columnKey: 'nomeServidor',
    },
    {
      key: 'matricula',
      label: 'Matrícula',
      type: 'text',
      placeholder: 'Buscar por matrícula',
      columnKey: 'matricula',
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

  readonly columns: PciColumn<UsuarioRow>[] = [
    { key: 'login', label: 'Login', sortable: true },
    { key: 'nomeServidor', label: 'Servidor', sortable: true },
    { key: 'matricula', label: 'Matrícula' },
    { key: 'perfis', label: 'Perfis' },
    { key: 'status', label: 'Status' },
  ];

  readonly rowActions: PciRowAction<UsuarioRow>[] = [
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
    void this.router.navigateByUrl('/usuarios/novo');
  }

  onRowAction(event: { action: string; row: UsuarioRow }): void {
    if (event.action === 'edit') {
      void this.router.navigateByUrl(`/usuarios/editar/${event.row.id}`);
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
    this.api.listUsuarios({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        this.allRows.set(
          result.items.map((u) => ({
            id: u.id,
            login: u.login,
            nomeServidor: u.nomeServidor,
            matricula: u.matricula,
            perfis: (u.perfis ?? []).join(', '),
            status: u.ativo ? 'Ativo' : 'Inativo',
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar os usuários.');
        this.loading.set(false);
      },
    });
  }
}
