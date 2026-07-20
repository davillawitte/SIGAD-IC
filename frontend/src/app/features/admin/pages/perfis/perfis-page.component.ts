import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciColumn,
  PciDataTableComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

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
  imports: [CommonModule, FormsModule, PciAlertComponent, PciDataTableComponent, PciStackComponent],
  template: `
    <section class="page">
      <pci-stack gap="6" [fullWidth]="true">
        @if (error()) {
          <pci-alert variant="error" title="Erro" [message]="error()!" />
        }

        <div class="pager-bar">
          <label class="field field--inline">
            <span>Registros por página</span>
            <select
              [ngModel]="pageSize()"
              (ngModelChange)="onPageSizeChange($event)"
              name="pageSize"
            >
              @for (size of pageSizeOptions; track size) {
                <option [ngValue]="size">{{ size }}</option>
              }
            </select>
          </label>
        </div>

        <pci-data-table
          title="Perfis"
          addLabel="Novo perfil"
          [columns]="columns"
          [rows]="rows()"
          [total]="totalItems()"
          [page]="page()"
          [pageSize]="pageSize()"
          [loading]="loading()"
          [showToolbar]="true"
          (addClicked)="goCreate()"
          (rowAction)="goEdit($event.row)"
          (pageChange)="onPageChange($event)"
        />
      </pci-stack>
    </section>
  `,
  styles: `
    .field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      font-size: 0.875rem;
    }

    .field span {
      color: var(--pci-color-text-secondary, #6b7280);
      font-weight: 500;
    }

    .field--inline {
      flex-direction: row;
      align-items: center;
    }

    .pager-bar {
      display: flex;
      justify-content: flex-end;
    }

    select {
      height: 2.5rem;
      border: 1px solid var(--pci-color-border, #e5e7eb);
      border-radius: var(--pci-radius-md, 0.5rem);
      padding: 0 0.75rem;
      background: #fff;
    }
  `,
})
export class PerfisPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);

  readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  readonly page = signal(1);
  readonly pageSize = signal<PageSizeOption>(DEFAULT_PAGE_SIZE);
  readonly totalItems = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<PerfilRow[]>([]);

  readonly columns: PciColumn<PerfilRow>[] = [
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'codigo', label: 'Código', sortable: true },
    { key: 'descricao', label: 'Descrição' },
    { key: 'permissoes', label: 'Qtd. permissões' },
    { key: 'sistema', label: 'Sistema' },
    { key: 'status', label: 'Status' },
  ];

  ngOnInit(): void {
    this.reload();
  }

  goCreate(): void {
    void this.router.navigateByUrl('/perfis/novo');
  }

  goEdit(row: PerfilRow): void {
    void this.router.navigateByUrl(`/perfis/editar/${row.id}`);
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.reload();
  }

  onPageSizeChange(size: string | number): void {
    const parsed = Number(size) as PageSizeOption;
    this.pageSize.set(PAGE_SIZE_OPTIONS.includes(parsed) ? parsed : DEFAULT_PAGE_SIZE);
    this.page.set(1);
    this.reload();
  }

  private reload(): void {
    this.loading.set(true);
    this.api.listPerfis({ page: this.page(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.totalItems.set(result.totalItems);
        this.page.set(result.page);
        this.rows.set(
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
