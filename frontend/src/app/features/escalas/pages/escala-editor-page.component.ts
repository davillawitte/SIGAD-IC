import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCalendarComponent,
  PciCalendarEvent,
  PciDatepickerComponent,
  PciInputComponent,
  PciPageHeaderComponent,
  PciSelectionListComponent,
  PciSelectionListItem,
  PciStackComponent,
  PciTabComponent,
  PciTabsComponent,
} from '@davillawitte/pci-design-system';
import { filter } from 'rxjs/operators';

import { AdminApiService } from '../../admin/services/admin-api.service';
import { openConfirmDialog } from '../../../shared/dialogs/dialog.helpers';
import { EscalasApiService } from '../services/escalas-api.service';
import type {
  EscalaCobertura,
  EscalaConflitos,
  EscalaDetail,
  EscalaServidor,
  GerarEscalaItemPayload,
  PadraoEscala,
  RecorrenciaTipo,
  TipoFuncionamento,
  TipoJornada,
  TipoOcorrencia,
} from '../models/escalas.models';

type TabId = 'gerar' | 'calendario' | 'matriz' | 'cobertura' | 'conflitos' | 'massa';

interface GerarRow {
  servidorId: string;
  nome: string;
  padraoId: string;
  dataInicioCiclo: string;
  horaInicio: string;
  horaFim: string;
}

@Component({
  selector: 'app-escala-editor-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSelectModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCalendarComponent,
    PciDatepickerComponent,
    PciInputComponent,
    PciPageHeaderComponent,
    PciSelectionListComponent,
    PciStackComponent,
    PciTabComponent,
    PciTabsComponent,
  ],
  templateUrl: './escala-editor-page.component.html',
  styleUrl: './escala-editor-page.component.scss',
})
export class EscalaEditorPageComponent implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  private readonly tabOrder: TabId[] = ['gerar', 'calendario', 'matriz', 'cobertura', 'conflitos', 'massa'];

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly escala = signal<EscalaDetail | null>(null);
  readonly selectedServidorId = signal<string | null>(null);
  readonly tipos = signal<TipoOcorrencia[]>([]);
  readonly padroes = signal<PadraoEscala[]>([]);
  readonly availableServidores = signal<{ id: string; label: string }[]>([]);
  readonly allServidoresSetor = signal<{ id: string; label: string }[]>([]);
  readonly selectedDate = signal<string | null>(null);
  readonly viewDate = signal(new Date());
  readonly activeTab = signal<TabId>('gerar');
  readonly cobertura = signal<EscalaCobertura | null>(null);
  readonly conflitos = signal<EscalaConflitos | null>(null);
  readonly selectedServidorIdsForGerar = signal<string[]>([]);
  readonly gerarRows = signal<GerarRow[]>([]);

  readonly selectedServidorIdsForMassa = signal<string[]>([]);
  readonly massaError = signal<string | null>(null);
  readonly massaSuccess = signal<string | null>(null);

  readonly jornadaForm = this.fb.nonNullable.group({
    tipoJornada: ['Administrativo' as TipoJornada, Validators.required],
    dataInicio: ['', Validators.required],
    dataFim: ['', Validators.required],
    tipoOcorrenciaCodigo: ['M', Validators.required],
    recorrenciaTipo: ['DiasSemana' as RecorrenciaTipo, Validators.required],
    horaInicio: ['08:00'],
    horaFim: ['14:00'],
    horas: [6 as number | null],
    diasSemana: ['1,2,3,4,5'],
    intervaloDias: [1 as number | null],
    diasTrabalho: [1 as number | null],
    diasFolga: [3 as number | null],
    tipoOcorrenciaFolgaCodigo: ['D'],
  });

  readonly pontoForm = this.fb.nonNullable.group({
    tipoOcorrenciaCodigo: ['M', Validators.required],
    horaInicio: [''],
    horaFim: [''],
    horas: [null as number | null],
  });

  readonly gerarForm = this.fb.nonNullable.group({
    padraoId: ['', Validators.required],
    dataInicioCiclo: ['', Validators.required],
    horaInicio: [''],
    horaFim: [''],
    distribuir: [false],
  });

  readonly massaForm = this.fb.nonNullable.group({
    dataInicio: ['', Validators.required],
    dataFim: ['', Validators.required],
    tipoOcorrenciaCodigo: ['M', Validators.required],
    horaInicio: [''],
    horaFim: [''],
    horas: [null as number | null],
  });

  readonly selectedServidor = computed(() => {
    const id = this.selectedServidorId();
    return this.escala()?.servidores.find((s) => s.servidorId === id) ?? null;
  });

  readonly calendarEvents = computed<PciCalendarEvent[]>(() => {
    const servidor = this.selectedServidor();
    if (!servidor) return [];
    return (servidor.ocorrencias ?? []).map((o) => ({
      date: o.data.slice(0, 10),
      label: o.tipoOcorrenciaCodigo,
      description: o.horaInicio && o.horaFim ? `${o.horaInicio}–${o.horaFim}` : o.tipoOcorrenciaNome ?? '',
      variant: this.variantFor(o.tipoOcorrenciaCodigo),
      meta: o.origem,
    }));
  });

  readonly gerarServidorOptions = computed<PciSelectionListItem[]>(() => this.allServidoresSetor());

  readonly massaServidorOptions = computed<PciSelectionListItem[]>(() =>
    (this.escala()?.servidores ?? []).map((s) => ({ id: s.servidorId, label: `${s.servidorNome} — ${s.matricula}` })),
  );

  readonly matrizDays = computed(() => {
    const e = this.escala();
    if (!e) return [] as string[];
    const start = new Date(e.dataInicio + 'T00:00:00');
    const end = new Date(e.dataFim + 'T00:00:00');
    const list: string[] = [];
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      list.push(d.toISOString().slice(0, 10));
    }
    return list;
  });

  readonly selectedTabIndex = computed(() => Math.max(0, this.tabOrder.indexOf(this.activeTab())));

  isEditable(): boolean {
    const status = this.escala()?.status;
    return status === 'Rascunho' || status === 'Finalizada';
  }

  readonly tipoJornadaOptions: { label: string; value: TipoJornada }[] = [
    { label: 'Administrativo', value: 'Administrativo' },
    { label: 'Plantão', value: 'Plantao' },
    { label: 'Expediente', value: 'Expediente' },
    { label: 'Outro', value: 'Outro' },
  ];

  readonly recorrenciaOptions: { label: string; value: RecorrenciaTipo }[] = [
    { label: 'Somente data início', value: 'Nenhuma' },
    { label: 'Todos os dias', value: 'TodosOsDias' },
    { label: 'Dias da semana', value: 'DiasSemana' },
    { label: 'A cada X dias', value: 'ACadaXDias' },
    { label: 'Ciclo plantão (ex.: 1×3)', value: 'CicloPlantao' },
  ];

  private escalaId = '';

  ngOnInit(): void {
    this.escalaId = this.route.snapshot.paramMap.get('id') ?? '';
    this.api.listTiposOcorrencia().subscribe({ next: (items) => this.tipos.set(items) });
    this.gerarForm.valueChanges.subscribe(() => this.rebuildGerarRows(this.selectedServidorIdsForGerar()));
    this.reload();
  }

  setActiveTab(tab: TabId): void {
    this.activeTab.set(tab);
    if (tab === 'cobertura' && !this.cobertura()) {
      this.loadCobertura();
    }
    if (tab === 'conflitos' && !this.conflitos()) {
      this.loadConflitos();
    }
  }

  onTabIndexChange(index: number): void {
    const tab = this.tabOrder[index];
    if (tab) this.setActiveTab(tab);
  }

  onSelectServidor(servidorId: string): void {
    this.selectedServidorId.set(servidorId);
    this.selectedDate.set(null);
  }

  onSelectedDateChange(date: string | null): void {
    this.selectedDate.set(date);
    if (date) {
      const oc = this.selectedServidor()?.ocorrencias.find((o) => o.data.slice(0, 10) === date);
      this.pontoForm.patchValue({
        tipoOcorrenciaCodigo: oc?.tipoOcorrenciaCodigo ?? 'M',
        horaInicio: oc?.horaInicio?.slice(0, 5) ?? '',
        horaFim: oc?.horaFim?.slice(0, 5) ?? '',
        horas: oc?.horas ?? null,
      });
      this.jornadaForm.patchValue({ dataInicio: date, dataFim: date });
    }
  }

  addServidores(servidorIds: string[]): void {
    if (!servidorIds.length) return;
    this.saving.set(true);
    this.api.addServidores(this.escalaId, servidorIds).subscribe({
      next: (escala) => {
        this.escala.set(escala);
        this.refreshAvailable(escala);
        this.saving.set(false);
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  onAddServidorSelect(servidorId: string): void {
    if (!servidorId) return;
    this.addServidores([servidorId]);
  }

  removeServidor(servidorId: string): void {
    openConfirmDialog(this.dialog, {
      title: 'Remover servidor',
      message: 'Remover este servidor da escala?',
      confirmLabel: 'Remover',
      danger: true,
    })
      .pipe(filter(Boolean))
      .subscribe(() => {
        this.api.removeServidor(this.escalaId, servidorId).subscribe({
          next: () => this.reload(),
          error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
        });
      });
  }

  aplicarJornada(): void {
    const servidor = this.selectedServidor();
    if (!servidor) {
      this.error.set('Selecione um servidor.');
      return;
    }

    this.jornadaForm.markAllAsTouched();
    if (this.jornadaForm.invalid) {
      this.error.set('Preencha os dados da jornada.');
      return;
    }

    const v = this.jornadaForm.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .addJornada(this.escalaId, servidor.servidorId, {
        tipoJornada: v.tipoJornada,
        dataInicio: v.dataInicio,
        dataFim: v.dataFim,
        tipoOcorrenciaCodigo: v.tipoOcorrenciaCodigo,
        recorrenciaTipo: v.recorrenciaTipo,
        horaInicio: v.horaInicio || null,
        horaFim: v.horaFim || null,
        horas: v.horas,
        diasSemana: v.recorrenciaTipo === 'DiasSemana' ? v.diasSemana : null,
        intervaloDias: v.recorrenciaTipo === 'ACadaXDias' ? v.intervaloDias : null,
        diasTrabalho: v.recorrenciaTipo === 'CicloPlantao' ? v.diasTrabalho : null,
        diasFolga: v.recorrenciaTipo === 'CicloPlantao' ? v.diasFolga : null,
        tipoOcorrenciaFolgaCodigo:
          v.recorrenciaTipo === 'CicloPlantao' ? v.tipoOcorrenciaFolgaCodigo : null,
      })
      .subscribe({
        next: (escala) => {
          this.escala.set(escala);
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  salvarPonto(): void {
    const servidor = this.selectedServidor();
    const data = this.selectedDate();
    if (!servidor || !data) {
      this.error.set('Selecione um dia no calendário.');
      return;
    }

    const v = this.pontoForm.getRawValue();
    this.saving.set(true);
    this.api
      .upsertOcorrencia(this.escalaId, servidor.servidorId, {
        data,
        tipoOcorrenciaCodigo: v.tipoOcorrenciaCodigo,
        horaInicio: v.horaInicio || null,
        horaFim: v.horaFim || null,
        horas: v.horas,
      })
      .subscribe({
        next: (escala) => {
          this.escala.set(escala);
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  removerPonto(): void {
    const servidor = this.selectedServidor();
    const data = this.selectedDate();
    const oc = servidor?.ocorrencias.find((o) => o.data.slice(0, 10) === data);
    if (!oc) return;
    this.api.deleteOcorrencia(this.escalaId, oc.id).subscribe({
      next: () => this.reload(),
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  onGerarSelectionChange(ids: string[]): void {
    this.selectedServidorIdsForGerar.set(ids);
    this.rebuildGerarRows(ids);
  }

  onPadraoChange(padraoId: string): void {
    const padrao = this.padroes().find((p) => p.id === padraoId);
    if (!padrao) return;
    this.gerarForm.patchValue({
      horaInicio: padrao.horaInicioPadrao ?? this.gerarForm.controls.horaInicio.value,
      horaFim: padrao.horaFimPadrao ?? this.gerarForm.controls.horaFim.value,
    });
  }

  updateGerarRowDate(servidorId: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.gerarRows.set(
      this.gerarRows().map((row) => (row.servidorId === servidorId ? { ...row, dataInicioCiclo: value } : row)),
    );
  }

  removeGerarRow(servidorId: string): void {
    this.gerarRows.set(this.gerarRows().filter((row) => row.servidorId !== servidorId));
    this.selectedServidorIdsForGerar.set(this.selectedServidorIdsForGerar().filter((id) => id !== servidorId));
  }

  addDays(iso: string, days: number): string {
    if (!iso) return '';
    const d = new Date(iso + 'T00:00:00');
    d.setDate(d.getDate() + days);
    return d.toISOString().slice(0, 10);
  }

  gerarEscala(): void {
    const escala = this.escala();
    const rows = this.gerarRows();
    if (!escala || !rows.length) {
      this.error.set('Selecione ao menos um servidor.');
      return;
    }

    this.gerarForm.markAllAsTouched();
    if (this.gerarForm.invalid) {
      this.error.set('Selecione o padrão e a data base do ciclo.');
      return;
    }

    const master = this.gerarForm.getRawValue();
    const base = master.dataInicioCiclo || escala.dataInicio;
    const distribuir = master.distribuir;

    const itens: GerarEscalaItemPayload[] = rows.map((row, index) => ({
      servidorId: row.servidorId,
      padraoEscalaId: master.padraoId,
      dataInicioCiclo: distribuir ? this.addDays(base, index) : row.dataInicioCiclo || escala.dataInicio,
      horaInicio: master.horaInicio || null,
      horaFim: master.horaFim || null,
    }));

    this.saving.set(true);
    this.error.set(null);
    this.api
      .gerar(this.escalaId, {
        itens,
        distribuirAutomaticamente: distribuir,
        dataBaseDistribuicao: distribuir ? base : null,
      })
      .subscribe({
        next: (escala) => {
          this.escala.set(escala);
          this.refreshAvailable(escala);
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  codeFor(servidorId: string, day: string): string {
    const s = this.escala()?.servidores.find((x) => x.servidorId === servidorId);
    return s?.ocorrencias.find((o) => o.data.slice(0, 10) === day)?.tipoOcorrenciaCodigo ?? '';
  }

  isWeekend(day: string): boolean {
    const d = new Date(day + 'T00:00:00').getDay();
    return d === 0 || d === 6;
  }

  dayLabel(day: string): string {
    const d = new Date(day + 'T00:00:00');
    const letters = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
    return `${d.getDate()}\n${letters[d.getDay()]}`;
  }

  onMatrizCellClick(servidorId: string, day: string): void {
    if (!this.isEditable()) return;
    this.onSelectServidor(servidorId);
    this.onSelectedDateChange(day);
  }

  aplicarMassa(): void {
    this.massaForm.markAllAsTouched();
    if (this.massaForm.invalid || !this.selectedServidorIdsForMassa().length) {
      this.massaError.set('Selecione ao menos um servidor e preencha o período.');
      return;
    }
    this.massaSuccess.set(null);
    this.runMassa(false);
  }

  private runMassa(confirmarSobrescrita: boolean): void {
    const v = this.massaForm.getRawValue();
    this.saving.set(true);
    this.massaError.set(null);
    this.api
      .upsertOcorrenciasLote(this.escalaId, {
        servidorIds: this.selectedServidorIdsForMassa(),
        dataInicio: v.dataInicio,
        dataFim: v.dataFim,
        tipoOcorrenciaCodigo: v.tipoOcorrenciaCodigo,
        horaInicio: v.horaInicio || null,
        horaFim: v.horaFim || null,
        horas: v.horas,
        confirmarSobrescrita,
      })
      .subscribe({
        next: (escala) => {
          this.escala.set(escala);
          this.saving.set(false);
          this.massaSuccess.set('Ocorrências aplicadas com sucesso.');
        },
        error: (err: { error?: { message?: string } }) => {
          const message = err.error?.message ?? '';
          if (!confirmarSobrescrita && /sobrescrita/i.test(message)) {
            openConfirmDialog(this.dialog, {
              title: 'Confirmar sobrescrita',
              message: `${message} Deseja confirmar a sobrescrita?`,
              confirmLabel: 'Sobrescrever',
              danger: true,
            })
              .pipe(filter(Boolean))
              .subscribe(() => this.runMassa(true));
            this.saving.set(false);
            return;
          }
          this.saving.set(false);
          this.massaError.set(message || 'Não foi possível aplicar as ocorrências em massa.');
        },
      });
  }

  voltar(): void {
    void this.router.navigateByUrl(`/escalas/${this.escalaId}`);
  }

  private reload(): void {
    this.loading.set(true);
    this.api.get(this.escalaId).subscribe({
      next: (escala) => {
        if (escala.status !== 'Rascunho' && escala.status !== 'Finalizada') {
          this.error.set('Somente escalas em rascunho ou finalizadas podem ser editadas.');
        }
        this.escala.set(escala);
        this.viewDate.set(new Date(escala.dataInicio + 'T00:00:00'));
        this.jornadaForm.patchValue({
          dataInicio: escala.dataInicio,
          dataFim: escala.dataFim,
        });
        this.gerarForm.patchValue({ dataInicioCiclo: escala.dataInicio });
        this.massaForm.patchValue({ dataInicio: escala.dataInicio, dataFim: escala.dataFim });
        if (!this.selectedServidorId() && escala.servidores[0]) {
          this.selectedServidorId.set(escala.servidores[0].servidorId);
        }
        this.refreshAvailable(escala);
        this.loadPadroes(escala.tipoFuncionamento);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar a escala.');
        this.loading.set(false);
      },
    });
  }

  private loadPadroes(tipoFuncionamento: TipoFuncionamento): void {
    this.api.listPadroes(tipoFuncionamento).subscribe({
      next: (padroes) => {
        this.padroes.set(padroes);
        if (padroes.length && !this.gerarForm.controls.padraoId.value) {
          const first = padroes[0];
          this.gerarForm.patchValue({
            padraoId: first.id,
            horaInicio: first.horaInicioPadrao ?? '',
            horaFim: first.horaFimPadrao ?? '',
          });
        }
      },
    });
  }

  private loadCobertura(): void {
    this.api.getCobertura(this.escalaId).subscribe({
      next: (cobertura) => this.cobertura.set(cobertura),
      error: (err: { error?: { message?: string } }) =>
        this.error.set(err.error?.message ?? 'Não foi possível carregar a cobertura.'),
    });
  }

  private loadConflitos(): void {
    this.api.getConflitos(this.escalaId).subscribe({
      next: (conflitos) => this.conflitos.set(conflitos),
      error: (err: { error?: { message?: string } }) =>
        this.error.set(err.error?.message ?? 'Não foi possível carregar os conflitos.'),
    });
  }

  private rebuildGerarRows(ids: string[]): void {
    const escala = this.escala();
    const master = this.gerarForm.getRawValue();
    const existing = new Map(this.gerarRows().map((row) => [row.servidorId, row]));
    const nameFor = (id: string) => this.allServidoresSetor().find((o) => o.id === id)?.label ?? id;

    const rows: GerarRow[] = ids.map((id) => {
      const prev = existing.get(id);
      return {
        servidorId: id,
        nome: nameFor(id),
        padraoId: master.padraoId,
        dataInicioCiclo: prev?.dataInicioCiclo ?? master.dataInicioCiclo ?? escala?.dataInicio ?? '',
        horaInicio: master.horaInicio,
        horaFim: master.horaFim,
      };
    });
    this.gerarRows.set(rows);
  }

  private refreshAvailable(escala: EscalaDetail): void {
    this.adminApi.listServidores(false).subscribe({
      next: (servidores) => {
        const doSetor = servidores.filter((s) => s.setorId === escala.setorId);
        const inEscala = new Set(escala.servidores.map((s) => s.servidorId));
        this.availableServidores.set(
          doSetor
            .filter((s) => !inEscala.has(s.id))
            .map((s) => ({ id: s.id, label: `${s.nome} — ${s.matricula}` })),
        );
        this.allServidoresSetor.set(doSetor.map((s) => ({ id: s.id, label: `${s.nome} — ${s.matricula}` })));
      },
    });
  }

  private variantFor(codigo: string): PciCalendarEvent['variant'] {
    if (['D', 'F'].includes(codigo)) return 'info';
    if (['FR', 'LP', 'LM', 'LO'].includes(codigo)) return 'warning';
    if (['PD', 'PN', 'PT', 'CF'].includes(codigo)) return 'accent';
    return 'primary';
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }

  trackServidor(_: number, s: EscalaServidor): string {
    return s.servidorId;
  }
}
