import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciBreadcrumbService,
  PciButtonComponent,
  PciCalendarComponent,
  PciCalendarEvent,
  PciColumn,
  PciDataTableComponent,
  PciFeedbackModalService,
  PciLayoutBreadcrumbService,
  PciPageHeaderComponent,
  PciRowAction,
  PciStackComponent,
  PciTabComponent,
  PciTableCellDirective,
  PciTabsComponent,
} from '@davillawitte/pci-design-system';
import { filter, switchMap } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { CalendarioApiService, MarcacaoItem } from '../../services/calendario-api.service';
import { CALENDARIO_ROUTE_PAGES } from '../../calendario-institucional.routes.meta';
import {
  marcacaoToCalendarEvent,
  origemLabel,
  statusCalendarioLabel,
  statusCalendarioVariant,
  tipoMarcacaoLabel,
  tipoMarcacaoVariant,
} from '../../models/calendario.models';
import type { CalendarioAnoDetail as CalendarioAnoDetailDto } from '../../services/calendario-api.service';

type ViewTab = 'listagem' | 'calendario';

@Component({
  selector: 'app-calendario-ano-detail',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciCalendarComponent,
    PciDataTableComponent,
    PciPageHeaderComponent,
    PciStackComponent,
    PciTabComponent,
    PciTableCellDirective,
    PciTabsComponent,
  ],
  templateUrl: './calendario-ano-detail.html',
  styleUrl: './calendario-ano-detail.scss',
})
export class CalendarioAnoDetail implements OnInit, OnDestroy {
  private readonly api = inject(CalendarioApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly breadcrumb = inject(PciBreadcrumbService);
  private readonly layoutBreadcrumb = inject(PciLayoutBreadcrumbService);

  readonly loading = signal(true);
  readonly working = signal(false);
  readonly error = signal<string | null>(null);
  readonly calendario = signal<CalendarioAnoDetailDto | null>(null);

  readonly canPublicar = computed(() => this.auth.hasPermission('calendario.publicar'));
  readonly canGerenciar = computed(() => this.auth.hasPermission('calendario.gerenciar_marcacoes'));
  readonly canEditarAno = computed(() => this.auth.hasPermission('calendario.gerar'));
  readonly canExcluirAno = computed(() => this.auth.hasPermission('calendario.excluir'));

  readonly statusLabel = statusCalendarioLabel;
  readonly statusVariant = statusCalendarioVariant;
  readonly tipoLabel = tipoMarcacaoLabel;
  readonly tipoVariant = tipoMarcacaoVariant;
  readonly origemLabel = origemLabel;

  private readonly viewTabOrder: ViewTab[] = ['listagem', 'calendario'];
  readonly viewTab = signal<ViewTab>('listagem');
  readonly viewTabIndex = computed(() => Math.max(0, this.viewTabOrder.indexOf(this.viewTab())));

  readonly calendarViewDate = signal(new Date());
  readonly calendarEvents = computed<PciCalendarEvent[]>(() =>
    (this.calendario()?.marcacoes ?? []).map(marcacaoToCalendarEvent),
  );

  readonly columns: PciColumn<MarcacaoItem>[] = [
    { key: 'data', label: 'Data', cell: (row) => this.fmtData(row.data, row.dataFim) },
    { key: 'tipo', label: 'Tipo' },
    { key: 'origem', label: 'Origem' },
    { key: 'nome', label: 'Nome' },
    { key: 'local', label: 'Local', cell: (row) => row.local ?? '' },
  ];

  readonly rowActions: PciRowAction<MarcacaoItem>[] = [
    { id: 'edit', label: 'Editar', icon: 'edit', placement: 'inline' },
    { id: 'delete', label: 'Excluir', icon: 'trash', placement: 'inline', variant: 'danger' },
  ];

  ngOnInit(): void {
    this.layoutBreadcrumb.setItems(
      this.breadcrumb.buildFromRoutes(CALENDARIO_ROUTE_PAGES, '/calendario-institucional/:ano'),
    );
    this.reload();
  }

  ngOnDestroy(): void {
    this.layoutBreadcrumb.clear();
  }

  onViewTabIndexChange(index: number): void {
    const tab = this.viewTabOrder[index];
    if (tab) this.viewTab.set(tab);
  }

  novaMarcacao(): void {
    void this.router.navigateByUrl(`/calendario-institucional/${this.anoParam()}/marcacoes/novo`);
  }

  editarAno(): void {
    void this.router.navigateByUrl(`/calendario-institucional/${this.anoParam()}/editar`);
  }

  editar(marcacao: MarcacaoItem): void {
    void this.router.navigateByUrl(
      `/calendario-institucional/${this.anoParam()}/marcacoes/${marcacao.id}/editar`,
    );
  }

  onRowAction(event: { action: string; row: MarcacaoItem }): void {
    if (event.action === 'edit') this.editar(event.row);
    if (event.action === 'delete') this.excluir(event.row);
  }

  onCalendarEventClick(event: PciCalendarEvent): void {
    const marcacao = (this.calendario()?.marcacoes ?? []).find(
      (m) => m.data.slice(0, 10) === event.date && m.nome === event.label,
    );
    if (marcacao) this.editar(marcacao);
  }

  publicar(): void {
    const c = this.calendario();
    if (!c) return;
    this.working.set(true);
    this.error.set(null);
    this.api.publicar(c.id).subscribe({
      next: (atualizado) => {
        this.calendario.set(atualizado);
        this.working.set(false);
        this.feedback.showSuccess('Calendário publicado com sucesso.');
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível publicar o calendário.');
        this.working.set(false);
      },
    });
  }

  excluir(marcacao: MarcacaoItem): void {
    openConfirmDialog(this.dialog, {
      title: 'Excluir marcação',
      message: `Excluir "${marcacao.nome}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.working.set(true);
          this.error.set(null);
          return this.api.excluirMarcacao(marcacao.id);
        }),
      )
      .subscribe({
        next: () => {
          this.working.set(false);
          this.feedback.showSuccess('Marcação excluída com sucesso.');
          this.reload();
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível excluir a marcação.');
          this.working.set(false);
        },
      });
  }

  excluirAno(): void {
    const c = this.calendario();
    if (!c) return;
    openConfirmDialog(this.dialog, {
      title: 'Excluir calendário',
      message: `Excluir o calendário de ${c.ano}? Todas as marcações serão removidas. Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.working.set(true);
          this.error.set(null);
          return this.api.excluirAno(c.id);
        }),
      )
      .subscribe({
        next: () => {
          this.feedback.showSuccess('Calendário excluído com sucesso.');
          void this.router.navigateByUrl('/calendario-institucional');
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível excluir o calendário.');
          this.working.set(false);
        },
      });
  }

  fmtData(iso: string, iso2?: string | null): string {
    const a = this.fmtOne(iso);
    if (!iso2 || iso2 === iso) return a;
    return `${a} a ${this.fmtOne(iso2)}`;
  }

  private fmtOne(iso: string): string {
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  private anoParam(): string {
    return this.route.snapshot.paramMap.get('ano') ?? '';
  }

  private reload(): void {
    const ano = Number(this.anoParam());
    if (!Number.isFinite(ano)) {
      this.error.set('Ano inválido.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.calendarViewDate.set(new Date(ano, 0, 1));
    this.api.obterAno(ano).subscribe({
      next: (calendario) => {
        this.calendario.set(calendario);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar o calendário deste ano.');
        this.loading.set(false);
      },
    });
  }
}
