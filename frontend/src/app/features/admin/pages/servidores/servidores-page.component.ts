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
} from '../../models/admin.models';

type ServidorRow = {
  id: string;
  nome: string;
  matricula: string;
  cargo: string;
  setor: string;
  email: string;
  status: string;
};

@Component({
  selector: 'app-servidores-page',
  imports: [CommonModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './servidores-page.component.html',
})
export class ServidoresPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);

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

  readonly rowActions: PciRowAction<ServidorRow>[] = [
    { id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' },
  ];

  readonly filteredRows = computed(() => {
    const byPanel = filterRowsByPanelValues(
      this.allRows(),
      this.filterValues(),
      this.filterFields(),
    );
    return filterTableRowsByQuickSearch(byPanel, this.columns, this.searchTerm());
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
      void this.router.navigateByUrl(`/servidores/editar/${event.row.id}`);
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
    this.api.listServidores(false).subscribe({
      next: (items) => {
        this.allRows.set(
          items.map((s) => ({
            id: s.id,
            nome: s.nome,
            matricula: s.matricula,
            cargo: s.cargo,
            setor: s.setorNome,
            email: s.email,
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
