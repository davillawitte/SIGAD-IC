import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciColumn,
  PciFeedbackModalService,
  PciListPageComponent,
  PciRowAction,
} from '@davillawitte/pci-design-system';
import { filter, switchMap } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { CALENDARIO_ROUTE_PAGES } from '../../calendario-institucional.routes.meta';
import { CalendarioApiService } from '../../services/calendario-api.service';
import type { StatusCalendarioAno } from '../../services/calendario-api.service';
import { statusCalendarioLabel } from '../../models/calendario.models';

type CalendarioAnoRow = {
  id: string;
  ano: number;
  status: string;
  statusRaw: StatusCalendarioAno;
  totalFeriados: number;
  totalPontosFacultativos: number;
  totalEventos: number;
};

@Component({
  selector: 'app-calendario-ano-list',
  imports: [CommonModule, MatDialogModule, PciAlertComponent, PciListPageComponent],
  templateUrl: './calendario-ano-list.html',
})
export class CalendarioAnoList implements OnInit {
  private readonly api = inject(CalendarioApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly routePages = CALENDARIO_ROUTE_PAGES;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<CalendarioAnoRow[]>([]);
  readonly canGerar = computed(() => this.auth.hasPermission('calendario.gerar'));
  readonly canEditar = computed(() => this.auth.hasPermission('calendario.gerar'));
  readonly canExcluir = computed(() => this.auth.hasPermission('calendario.excluir'));

  readonly columns: PciColumn<CalendarioAnoRow>[] = [
    { key: 'ano', label: 'Ano', sortable: true },
    { key: 'status', label: 'Status' },
    { key: 'totalFeriados', label: 'Feriados', width: '8rem' },
    { key: 'totalPontosFacultativos', label: 'Pontos facultativos', width: '10rem' },
    { key: 'totalEventos', label: 'Eventos institucionais', width: '10rem' },
  ];

  readonly rowActions = computed<PciRowAction<CalendarioAnoRow>[]>(() => {
    const actions: PciRowAction<CalendarioAnoRow>[] = [
      { id: 'view', label: 'Ver', icon: 'eye', placement: 'inline' },
    ];
    if (this.canEditar()) {
      actions.push({ id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' });
    }
    if (this.canExcluir()) {
      actions.push({
        id: 'delete',
        label: 'Excluir',
        icon: 'trash',
        placement: 'inline',
        variant: 'danger',
        hidden: (row) => row.statusRaw !== 'Rascunho',
      });
    }
    return actions;
  });

  ngOnInit(): void {
    this.reload();
  }

  onCreate(): void {
    void this.router.navigateByUrl('/calendario-institucional/novo');
  }

  onRowAction(event: { action: string; row: CalendarioAnoRow }): void {
    if (event.action === 'view') {
      void this.router.navigateByUrl(`/calendario-institucional/${event.row.ano}`);
    }
    if (event.action === 'edit') {
      void this.router.navigateByUrl(`/calendario-institucional/${event.row.ano}/editar`);
    }
    if (event.action === 'delete') {
      this.excluirAno(event.row);
    }
  }

  private excluirAno(row: CalendarioAnoRow): void {
    openConfirmDialog(this.dialog, {
      title: 'Excluir calendário',
      message: `Excluir o calendário de ${row.ano}? Todas as marcações serão removidas. Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.loading.set(true);
          this.error.set(null);
          return this.api.excluirAno(row.id);
        }),
      )
      .subscribe({
        next: () => {
          this.feedback.showSuccess('Calendário excluído com sucesso.');
          this.reload();
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível excluir o calendário.');
          this.loading.set(false);
        },
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listarAnos().subscribe({
      next: (anos) => {
        this.rows.set(
          anos.map((a) => ({
            id: a.id,
            ano: a.ano,
            status: statusCalendarioLabel(a.status),
            statusRaw: a.status,
            totalFeriados: a.totalFeriados,
            totalPontosFacultativos: a.totalPontosFacultativos,
            totalEventos: a.totalEventos,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar o calendário institucional.');
        this.loading.set(false);
      },
    });
  }
}
