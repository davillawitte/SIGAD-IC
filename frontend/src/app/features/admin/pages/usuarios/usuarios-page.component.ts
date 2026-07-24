import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
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
import { filter } from 'rxjs/operators';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS, PageSizeOption } from '../../models/admin.models';
import { formatCpfDisplay } from '../../../../shared/input-masks';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';

type UsuarioRow = {
  id: string;
  nomeServidor: string;
  matricula: string;
  cpf: string;
  status: string;
  perfis: string;
};

@Component({
  selector: 'app-usuarios-page',
  imports: [CommonModule, MatDialogModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './usuarios-page.component.html',
})
export class UsuariosPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal<PageSizeOption>(DEFAULT_PAGE_SIZE);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly allRows = signal<UsuarioRow[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly sort = signal<PciSortChange<UsuarioRow> | null>(null);

  readonly filterFields: PciFilterField[] = [
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
      key: 'cpf',
      label: 'CPF',
      type: 'text',
      placeholder: 'Buscar por CPF',
      columnKey: 'cpf',
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
    { key: 'nomeServidor', label: 'Servidor', sortable: true },
    { key: 'matricula', label: 'Matrícula' },
    { key: 'cpf', label: 'CPF' },
    { key: 'perfis', label: 'Perfis' },
    { key: 'status', label: 'Status' },
  ];

  readonly rowActions: PciRowAction<UsuarioRow>[] = [
    { id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' },
    { id: 'reset-senha', label: 'Resetar senha', icon: 'lock', placement: 'inline' },
  ];

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
    void this.router.navigateByUrl('/usuarios/novo');
  }

  onRowAction(event: { action: string; row: UsuarioRow }): void {
    if (event.action === 'edit') {
      void this.router.navigateByUrl(`/usuarios/editar/${event.row.id}`);
      return;
    }

    if (event.action === 'reset-senha') {
      this.resetSenha(event.row);
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

  onSortChange(sort: PciSortChange<UsuarioRow> | null): void {
    this.sort.set(sort);
  }

  onPageSizeChange(size: number): void {
    const parsed = size as PageSizeOption;
    this.pageSize.set(PAGE_SIZE_OPTIONS.includes(parsed) ? parsed : DEFAULT_PAGE_SIZE);
    this.page.set(1);
  }

  private resetSenha(row: UsuarioRow): void {
    openConfirmDialog(this.dialog, {
      title: 'Resetar senha',
      message: `Resetar a senha de ${row.nomeServidor}?`,
      confirmLabel: 'Resetar',
      danger: true,
    })
      .pipe(filter(Boolean))
      .subscribe(() => {
        this.loading.set(true);
        this.error.set(null);
        this.successMessage.set(null);
        this.api.resetUsuarioSenha(row.id).subscribe({
          next: (result) => {
            this.successMessage.set(
              `Senha resetada para ${result.nomeServidor}. Nova senha temporária: ${result.senhaTemporaria}`,
            );
            this.loading.set(false);
          },
          error: (err: { error?: { message?: string } }) => {
            this.error.set(err.error?.message ?? 'Não foi possível resetar a senha.');
            this.loading.set(false);
          },
        });
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.listUsuarios({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        this.allRows.set(
          result.items.map((u) => ({
            id: u.id,
            nomeServidor: u.nomeServidor,
            matricula: u.matricula,
            cpf: formatCpfDisplay(u.cpf || u.login),
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
