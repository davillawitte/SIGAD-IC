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
  filterRowsByPanelValues,
  filterTableRowsByQuickSearch,
  sortTableRows,
} from '@davillawitte/pci-design-system';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { maskMatricula } from '../../../../shared/input-masks';
import { PERMUTAS_ROUTE_PAGES } from '../../permutas.routes.meta';
import { PermutasApiService } from '../../services/permutas-api.service';

type PermutaRow = {
  id: string;
  solicitante: string;
  permutante: string;
  ida: string;
  volta: string;
  observacao: string;
  registro: string;
};

/** Permutas da Gestão do Setor — lista as que envolvem servidores da chefia do usuário. */
@Component({
  selector: 'app-permutas-list',
  imports: [MatDialogModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './permutas-list.html',
  styleUrl: './permutas-list.scss',
})
export class PermutasList implements OnInit {
  private readonly api = inject(PermutasApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly routePages = PERMUTAS_ROUTE_PAGES;
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly allRows = signal<PermutaRow[]>([]);
  readonly filterValues = signal<PciFilterValues>({});
  readonly filtersExpanded = signal(true);
  readonly searchTerm = signal('');
  readonly sort = signal<PciSortChange<PermutaRow> | null>(null);
  readonly canCreate = computed(() => this.auth.hasPermission('escalas.editar'));

  readonly filterFields: PciFilterField[] = [
    { key: 'solicitante', label: 'Solicitante', type: 'text', columnKey: 'solicitante' },
    { key: 'permutante', label: 'Permutante', type: 'text', columnKey: 'permutante' },
  ];

  readonly columns: PciColumn<PermutaRow>[] = [
    { key: 'solicitante', label: 'Solicitante', sortable: true },
    { key: 'permutante', label: 'Permutante', sortable: true },
    { key: 'ida', label: 'Ida (solicitante → permutante)' },
    { key: 'volta', label: 'Volta (permutante → solicitante)' },
    { key: 'observacao', label: 'Observação' },
    { key: 'registro', label: 'Registrada por' },
  ];

  readonly rowActions = computed<PciRowAction<PermutaRow>[]>(() =>
    this.auth.hasPermission('escalas.editar')
      ? [{ id: 'delete', label: 'Excluir', icon: 'trash', placement: 'inline' }]
      : [],
  );

  readonly filteredRows = computed(() => {
    const byPanel = filterRowsByPanelValues(this.allRows(), this.filterValues(), this.filterFields);
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
    void this.router.navigateByUrl('/permutas/nova');
  }

  onRowAction(event: { action: string; row: PermutaRow }): void {
    if (event.action !== 'delete') {
      return;
    }
    openConfirmDialog(this.dialog, {
      title: 'Excluir permuta',
      message:
        `Excluir a permuta entre ${event.row.solicitante} e ${event.row.permutante}? ` +
        'As células das escalas nas datas da ida e da volta voltam ao que eram antes.',
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(filter(Boolean))
      .subscribe(() => {
        this.api.delete(event.row.id).subscribe({
          next: () => {
            this.feedback.showSuccess('Permuta excluída com sucesso.');
            this.reload();
          },
          error: (err: { error?: { message?: string } }) =>
            this.error.set(err.error?.message ?? 'Não foi possível excluir a permuta.'),
        });
      });
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

  onSortChange(sort: PciSortChange<PermutaRow> | null): void {
    this.sort.set(sort);
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.list().subscribe({
      next: (items) => {
        this.allRows.set(
          items.map((p) => ({
            id: p.id,
            solicitante: `${p.solicitanteNome} — ${maskMatricula(p.solicitanteMatricula)}`,
            permutante: `${p.permutanteNome} — ${maskMatricula(p.permutanteMatricula)}`,
            ida: this.fmt(p.dataIda),
            volta: this.fmt(p.dataVolta),
            observacao: p.observacao || '—',
            registro: `${p.createdBy || '—'} em ${this.fmt(p.createdAt)}`,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar as permutas.');
        this.loading.set(false);
      },
    });
  }

  private fmt(iso: string): string {
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }
}
