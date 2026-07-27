import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCheckboxComponent,
  PciInputComponent,
  PciPageHeaderComponent,
  PciSelectComponent,
  PciStackComponent,
  PciStepComponent,
  PciStepperComponent,
  PciTabComponent,
  PciTabsComponent,
  PciFeedbackModalService,
  PciToastService,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';
import { Observable, forkJoin, merge, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { AdminApiService } from '../../../admin/services/admin-api.service';
import type { ServidorListItem } from '../../../admin/models/admin.models';
import {
  AfastamentosApiService,
  AfastamentoItem,
} from '../../../afastamentos/services/afastamentos-api.service';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { ESCALAS_ROUTE_PAGES } from '../../escalas-route-pages';
import { EscalasApiService } from '../../services/escalas-api.service';
import type {
  EscalaDetail,
  EscalaListItem,
  EscalaOcorrencia,
  EscalaServidor,
  PadraoEscala,
  TipoFuncionamento,
} from '../../models/escalas.models';
import { statusEscalaLabel } from '../../models/escalas.models';
import {
  AfastamentoDialog,
  AfastamentoDialogData,
} from '../../components/afastamento-dialog/afastamento-dialog';
import {
  EscalaMatrix,
  MES_NOMES,
  daysInRange,
  formatLocalDate,
  isValidOcorrenciaCodigo,
} from '../../components/escala-matrix/escala-matrix';
import {
  buildOcorrenciasForServidor,
  normalizeDay,
  type RegimeCodigo,
} from '../../utils/escala-ocorrencia.builder';

type WizardStep = 1 | 2 | 3 | 4;
type Step3Tab = 'matriz' | 'formulario';
type ReviewTab = 'matriz' | 'vertical';

interface SelectedCell {
  servidorId: string;
  day: string;
}

interface CellPayload {
  data: string;
  tipoOcorrenciaCodigo: string;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
}

function nextMonthYear(): { mes: number; ano: number } {
  const now = new Date();
  const next = new Date(now.getFullYear(), now.getMonth() + 1, 1);
  return { mes: next.getMonth() + 1, ano: next.getFullYear() };
}

function lastDayOfMonth(ano: number, mes: number): string {
  const d = new Date(ano, mes, 0);
  return formatLocalDate(d);
}

function firstDayOfMonth(ano: number, mes: number): string {
  return `${ano}-${String(mes).padStart(2, '0')}-01`;
}

@Component({
  selector: 'app-escala-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCheckboxComponent,
    PciInputComponent,
    PciPageHeaderComponent,
    PciSelectComponent,
    PciStackComponent,
    PciStepComponent,
    PciStepperComponent,
    PciTabComponent,
    PciTabsComponent,
    AppFormSectionComponent,
    AppFormColDirective,
    EscalaMatrix,
  ],
  templateUrl: './escala-form.html',
  styleUrl: './escala-form.scss',
})
export class EscalaForm implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly afastamentosApi = inject(AfastamentosApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(PciToastService);
  private readonly feedback = inject(PciFeedbackModalService);

  /** Capturado na criação — `getCurrentNavigation()` só funciona durante a construção. */
  private readonly initialOpenStep: WizardStep =
    (this.router.getCurrentNavigation()?.extras.state as { openStep?: number } | undefined)
      ?.openStep === 3
      ? 3
      : 2;

  readonly routePages = ESCALAS_ROUTE_PAGES;

  readonly step = signal<WizardStep>(1);
  readonly working = signal(false);
  readonly error = signal<string | null>(null);
  readonly escala = signal<EscalaDetail | null>(null);
  readonly dirty = signal(false);
  readonly isEditMode = signal(false);
  readonly periodLocked = signal(false);

  private skipDeactivateGuard = false;

  private readonly next = nextMonthYear();

  readonly step1Form = this.fb.nonNullable.group({
    setorId: ['', Validators.required],
    mes: [String(this.next.mes), Validators.required],
    ano: [this.next.ano, [Validators.required, Validators.min(2000), Validators.max(2100)]],
    origemEscalaId: [''],
  });

  readonly setorOptions = signal<PciSelectOption[]>([]);
  readonly showSetorSelect = computed(() => this.setorOptions().length > 1);
  readonly setorUnicoLabel = signal<string | null>(null);
  readonly origemOptions = signal<PciSelectOption[]>([]);
  readonly origemEscalaId = signal('');
  readonly periodoDuplicado = signal(false);

  readonly mesOptions: PciSelectOption[] = MES_NOMES.slice(1).map((label, i) => ({
    label,
    value: String(i + 1),
  }));

  readonly regimeOptions: { codigo: RegimeCodigo; label: string }[] = [
    { codigo: 'EXP_ADM', label: 'Expediente administrativo 6h' },
    { codigo: '12X36', label: 'Plantão 12h' },
    { codigo: '24X72', label: 'Plantão 24h' },
  ];

  readonly padroes = signal<PadraoEscala[]>([]);
  readonly padroesByCodigo = computed(() => new Map(this.padroes().map((p) => [p.codigo, p])));
  readonly servidoresSetor = signal<ServidorListItem[]>([]);
  readonly regimesSelected = signal<RegimeCodigo[]>([]);
  private lastAppliedRegimesFingerprint = '';
  readonly selectedServidorIds = signal<Set<string>>(new Set());
  readonly servidorRegimes = signal<Map<string, Set<RegimeCodigo>>>(new Map());
  readonly servidorInicioCiclo = signal<Map<string, string>>(new Map());
  /** Valores dos selects de início de ciclo — só copiados para o Map ao avançar o passo. */
  readonly inicioCicloForm = this.fb.nonNullable.group({});
  readonly multiRegime = computed(() => this.regimesSelected().length > 1);
  readonly needsInicioCiclo = computed(() => {
    const r = this.regimesSelected();
    return r.length === 1 && (r[0] === '12X36' || r[0] === '24X72');
  });

  inicioCicloOptions(): PciSelectOption[] {
    const v = this.step1Form.getRawValue();
    const ano = Number(v.ano);
    const mes = Number(v.mes);
    if (!ano || !mes) return [];
    return daysInRange(firstDayOfMonth(ano, mes), lastDayOfMonth(ano, mes)).map((d) => ({
      label: this.fmt(d),
      value: d,
    }));
  }

  readonly afastamentos = signal<AfastamentoItem[]>([]);
  readonly homeOfficeDays = signal<Map<string, Set<number>>>(new Map());
  readonly expedienteTardeDays = signal<Map<string, Set<number>>>(new Map());
  readonly selectedCells = signal<SelectedCell[]>([]);
  private clipboard: string[] = [];

  readonly weekdayOptions: { value: number; label: string }[] = [
    { value: 1, label: 'Seg' },
    { value: 2, label: 'Ter' },
    { value: 3, label: 'Qua' },
    { value: 4, label: 'Qui' },
    { value: 5, label: 'Sex' },
  ];

  private readonly step3TabOrder: Step3Tab[] = ['matriz', 'formulario'];
  readonly step3Tab = signal<Step3Tab>('matriz');
  readonly step3TabIndex = computed(() => Math.max(0, this.step3TabOrder.indexOf(this.step3Tab())));

  readonly formServidorId = signal('');
  readonly formServidorControl = this.fb.nonNullable.control('');
  readonly formServidorOptions = computed<PciSelectOption[]>(() =>
    (this.escala()?.servidores ?? []).map((s) => ({
      label: `${s.servidorNome} — ${s.matricula}`,
      value: s.servidorId,
    })),
  );
  readonly formServidor = computed(() => {
    const id = this.formServidorId();
    return this.escala()?.servidores.find((s) => s.servidorId === id) ?? null;
  });

  readonly singleRegime = computed<RegimeCodigo | null>(() =>
    this.regimesSelected().length === 1 ? this.regimesSelected()[0] : null,
  );

  readonly hoServidores = computed(() => {
    const e = this.escala();
    if (!e || this.singleRegime() !== 'EXP_ADM') return [];
    return e.servidores;
  });

  readonly matrizDays = computed(() => {
    const e = this.escala();
    if (!e) return [] as string[];
    return daysInRange(e.dataInicio, e.dataFim);
  });

  private readonly reviewTabOrder: ReviewTab[] = ['matriz', 'vertical'];
  readonly reviewTab = signal<ReviewTab>('matriz');
  readonly reviewTabIndex = computed(() => Math.max(0, this.reviewTabOrder.indexOf(this.reviewTab())));

  readonly stepperIndex = computed(() => this.step() - 1);

  readonly pageTitle = computed(() => (this.isEditMode() ? 'Editar escala' : 'Nova escala'));

  readonly onMatrizCodeChangeBound = (servidorId: string, day: string, code: string) =>
    this.onMatrizCodeChange(servidorId, day, code);
  readonly onCellMouseDownBound = (servidorId: string, day: string, event: MouseEvent) =>
    this.onCellMouseDown(servidorId, day, event);
  readonly onCellKeydownBound = (servidorId: string, day: string, event: KeyboardEvent) =>
    this.onCellKeydown(servidorId, day, event);
  readonly isSelectedBound = (servidorId: string, day: string) => this.isSelected(servidorId, day);

  private get escalaId(): string {
    return this.escala()?.id ?? '';
  }

  private get isPersisted(): boolean {
    return !!this.escalaId;
  }

  ngOnInit(): void {
    const editId = this.route.snapshot.paramMap.get('id');
    this.adminApi.listMeusSetores().subscribe({
      next: (setores) => {
        this.setorOptions.set(setores.map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id })));
        if (!editId && setores.length === 1) {
          this.step1Form.controls.setorId.setValue(setores[0].id);
          this.step1Form.controls.setorId.disable({ emitEvent: false });
          this.setorUnicoLabel.set(`${setores[0].sigla} — ${setores[0].nome}`);
        }
        if (!editId) {
          this.loadOrigensDisponiveis();
          this.checkPeriodoDuplicado();
        }
      },
      error: () => this.error.set('Não foi possível carregar os setores disponíveis.'),
    });

    this.step1Form.controls.origemEscalaId.valueChanges.subscribe((id) => {
      this.origemEscalaId.set(id || '');
    });

    this.step1Form.controls.setorId.valueChanges.pipe(debounceTime(200)).subscribe(() => {
      if (this.periodLocked() || this.isEditMode()) return;
      this.loadOrigensDisponiveis();
      this.checkPeriodoDuplicado();
    });

    merge(this.step1Form.controls.mes.valueChanges, this.step1Form.controls.ano.valueChanges)
      .pipe(debounceTime(300))
      .subscribe(() => {
        if (this.periodLocked() || this.isEditMode()) return;
        this.sugerirOrigemAnterior();
        this.checkPeriodoDuplicado();
      });

    this.formServidorControl.valueChanges.subscribe((id) => {
      this.formServidorId.set(id || '');
    });

    if (editId) {
      this.isEditMode.set(true);
      this.periodLocked.set(true);
      this.loadExisting(editId, this.initialOpenStep);
    }
  }

  /** Controles do select — criados sob demanda; o Map só é preenchido ao sync/continuar. */
  private ensureInicioCicloControls(): void {
    if (!this.needsInicioCiclo()) return;
    for (const id of this.selectedServidorIds()) {
      if (!this.inicioCicloForm.contains(id)) {
        const control = this.fb.nonNullable.control(this.servidorInicioCiclo().get(id) ?? '');
        control.valueChanges.subscribe((val) => {
          const normalized = this.normalizeDay(val);
          const map = new Map(this.servidorInicioCiclo());
          if (normalized) map.set(id, normalized);
          else map.delete(id);
          this.servidorInicioCiclo.set(map);
          if (this.escala()) {
            this.regenerateServidorOcorrencias(id);
          }
        });
        this.inicioCicloForm.addControl(id, control);
      }
    }
  }

  private normalizeDay(value: string | null | undefined): string {
    return normalizeDay(value);
  }

  private emptyOc(day: string): EscalaOcorrencia {
    return this.oc(day, '');
  }

  private oc(
    day: string,
    codigo: string,
    horas?: number | null,
    horaInicio?: string | null,
    horaFim?: string | null,
  ): EscalaOcorrencia {
    return {
      id: `local-${day}-${codigo || 'empty'}`,
      data: day,
      tipoOcorrenciaCodigo: codigo,
      horas: horas ?? null,
      horaInicio: horaInicio ?? null,
      horaFim: horaFim ?? null,
      origem: 'Manual',
    };
  }

  /** Lê os selects e grava no Map (YYYY-MM-DD). */
  private syncInicioCicloFromControls(): void {
    const map = new Map<string, string>();
    for (const id of this.selectedServidorIds()) {
      const val = this.normalizeDay(this.inicioCicloForm.get(id)?.value);
      if (val) map.set(id, val);
    }
    this.servidorInicioCiclo.set(map);
  }

  private regenerateServidorOcorrencias(servidorId: string): void {
    const e = this.escala();
    if (!e) return;
    const days = daysInRange(e.dataInicio, e.dataFim);
    const servidores = e.servidores.map((s) => {
      if (s.servidorId !== servidorId) return s;
      return { ...s, ocorrencias: this.buildOcorrenciasForServidor(servidorId, days) };
    });
    this.escala.set({ ...e, servidores });
    this.recalcCargas();
    this.markDirty();
  }

  private regenerateAllPlantaoOcorrencias(): void {
    const e = this.escala();
    if (!e) return;
    const days = daysInRange(e.dataInicio, e.dataFim);
    const servidores = e.servidores.map((s) => ({
      ...s,
      ocorrencias: this.buildOcorrenciasForServidor(s.servidorId, days),
    }));
    this.escala.set({ ...e, servidores });
    this.recalcCargas();
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.dirty()) {
      event.preventDefault();
      event.returnValue = true;
    }
  }

  canDeactivate(): boolean | Observable<boolean> {
    if (this.skipDeactivateGuard || !this.dirty()) return true;
    return openConfirmDialog(this.dialog, {
      title: 'Alterações não salvas',
      message: 'Há alterações não salvas. Deseja salvar como rascunho antes de sair?',
      confirmLabel: 'Salvar rascunho',
      cancelLabel: 'Sair sem salvar',
    }).pipe(
      switchMap((save) => {
        if (!save) {
          this.dirty.set(false);
          return of(true);
        }
        return this.persistDraft$().pipe(
          tap(() => {
            this.dirty.set(false);
            this.skipDeactivateGuard = true;
            this.feedback.showSuccess('Rascunho salvo com sucesso.');
          }),
          map(() => true),
          catchError((err: { error?: { message?: string } }) => {
            this.fail(err.error?.message ?? 'Não foi possível salvar o rascunho.');
            return of(false);
          }),
        );
      }),
    );
  }

  onStepperIndexChange(index: number): void {
    const target = (index + 1) as WizardStep;
    if (target === 3 && this.step() === 2) {
      if (!this.enterStep3()) {
        queueMicrotask(() => this.step.set(this.step()));
      }
      return;
    }
    if (target < this.step() || (target === this.step() + 1 && this.canAdvanceTo(target))) {
      this.step.set(target);
      if (target === 3) this.loadStep3Data();
    } else {
      queueMicrotask(() => this.step.set(this.step()));
    }
  }

  private canAdvanceTo(target: WizardStep): boolean {
    if (target === 2) return this.step1Form.valid && !this.periodoDuplicado();
    if (target === 3) return !!this.escala();
    if (target === 4) return !!this.escala();
    return true;
  }

  // ---------- Step 1 ----------

  continuar1(): void {
    this.step1Form.markAllAsTouched();
    if (this.step1Form.invalid) {
      this.error.set(
        this.showSetorSelect() ? 'Selecione o setor, mês e ano.' : 'Preencha o mês e o ano.',
      );
      return;
    }
    if (this.isEditMode()) {
      this.error.set(null);
      this.step.set(2);
      this.loadStep2Data();
      return;
    }

    const origemId = this.step1Form.getRawValue().origemEscalaId?.trim();
    if (origemId) {
      this.copiarOrigem(origemId);
      return;
    }

    this.working.set(true);
    this.checkPeriodoDuplicado$((exists) => {
      this.working.set(false);
      if (exists) {
        this.error.set('Já existe escala para este setor neste mês/ano.');
        return;
      }
      this.error.set(null);
      this.step.set(2);
      this.loadStep2Data();
    });
  }

  private copiarOrigem(origemId: string): void {
    if (this.periodoDuplicado()) {
      this.error.set('Já existe escala para este setor neste mês/ano.');
      return;
    }
    const v = this.step1Form.getRawValue();
    this.working.set(true);
    this.error.set(null);
    this.api.copiar(origemId, { ano: Number(v.ano), mes: Number(v.mes) }).subscribe({
      next: (escala) => {
        this.dirty.set(false);
        this.working.set(false);
        this.skipDeactivateGuard = true;
        void this.router.navigate(['/escalas', escala.id, 'editar'], {
          replaceUrl: true,
          state: { openStep: 3 },
        });
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  private loadOrigensDisponiveis(): void {
    const setorId = this.step1Form.getRawValue().setorId;
    if (!setorId) {
      this.origemOptions.set([]);
      this.step1Form.controls.origemEscalaId.setValue('', { emitEvent: false });
      this.origemEscalaId.set('');
      return;
    }

    this.api.list({ setorId, page: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        const items = [...(result.items ?? [])].sort((a, b) =>
          b.ano !== a.ano ? b.ano - a.ano : b.mes - a.mes,
        );
        this.origemOptions.set([
          { label: 'Não copiar — Criar integralmente', value: '' },
          ...items.map((item) => this.toOrigemOption(item)),
        ]);
        this.sugerirOrigemAnterior(items);
      },
      error: () => {
        this.origemOptions.set([]);
      },
    });
  }

  private toOrigemOption(item: EscalaListItem): PciSelectOption {
    const mesNome = MES_NOMES[item.mes] ?? String(item.mes);
    return {
      label: `${item.identificacao} · ${mesNome}/${item.ano} · ${statusEscalaLabel(item.status)}`,
      value: item.id,
    };
  }

  private sugerirOrigemAnterior(items?: EscalaListItem[]): void {
    const v = this.step1Form.getRawValue();
    if (!v.setorId || !v.mes || !v.ano) return;
    if (this.step1Form.controls.origemEscalaId.dirty) return;

    const apply = (id: string | null | undefined) => {
      if (!id) return;
      if (!this.origemOptions().some((o) => o.value === id)) return;
      this.step1Form.controls.origemEscalaId.setValue(id, { emitEvent: true });
    };

    if (items?.length) {
      const mes = Number(v.mes);
      const ano = Number(v.ano);
      const prev = new Date(ano, mes - 2, 1); // mês anterior ao destino
      const match = items.find((i) => i.ano === prev.getFullYear() && i.mes === prev.getMonth() + 1);
      if (match) {
        apply(match.id);
        return;
      }
    }

    this.api.getEscalaAnterior(v.setorId, Number(v.ano), Number(v.mes)).subscribe({
      next: (info) => apply(info?.id),
      error: () => undefined,
    });
  }

  private checkPeriodoDuplicado(): void {
    this.checkPeriodoDuplicado$((exists) => {
      this.periodoDuplicado.set(exists);
      if (exists) {
        this.error.set('Já existe escala para este setor neste mês/ano.');
      } else if (this.error() === 'Já existe escala para este setor neste mês/ano.') {
        this.error.set(null);
      }
    });
  }

  private checkPeriodoDuplicado$(done: (exists: boolean) => void): void {
    if (this.isEditMode() || this.periodLocked()) {
      this.periodoDuplicado.set(false);
      done(false);
      return;
    }
    const v = this.step1Form.getRawValue();
    const setorId = v.setorId;
    const ano = Number(v.ano);
    const mes = Number(v.mes);
    if (!setorId || !mes || !ano) {
      this.periodoDuplicado.set(false);
      done(false);
      return;
    }
    this.api.list({ setorId, ano, mes, page: 1, pageSize: 1 }).subscribe({
      next: (result) => {
        const exists = (result.totalItems ?? result.items?.length ?? 0) > 0;
        this.periodoDuplicado.set(exists);
        done(exists);
      },
      error: () => {
        this.periodoDuplicado.set(false);
        done(false);
      },
    });
  }

  private loadExisting(id: string, openStep: WizardStep = 2): void {
    this.working.set(true);
    this.api.get(id).subscribe({
      next: (escala) => {
        if (escala.status === 'Publicada') {
          this.working.set(false);
          this.dirty.set(false);
          this.skipDeactivateGuard = true;
          this.toast.showError('Escala publicada não pode ser editada. Solicite devolução se necessário.');
          void this.router.navigateByUrl(`/escalas/${id}`);
          return;
        }
        this.escala.set(escala);
        this.step1Form.patchValue({
          setorId: escala.setorId,
          mes: String(escala.mes),
          ano: escala.ano,
          origemEscalaId: '',
        });
        this.step1Form.disable();
        this.loadStep2Data(true, () => {
          this.hydrateSelectionFromEscala(escala);
          this.step.set(openStep);
          if (openStep === 3) this.loadStep3Data();
          this.working.set(false);
          this.dirty.set(false);
        });
      },
      error: () => {
        this.fail('Não foi possível carregar a escala.');
      },
    });
  }

  // ---------- Step 2 ----------

  private loadStep2Data(preserveSelection = false, afterLoad?: () => void): void {
    const setorId = this.step1Form.getRawValue().setorId;
    forkJoin({
      padroes: this.api.listPadroes(),
      servidores: this.adminApi.listMeusServidores(false),
    }).subscribe({
      next: ({ padroes, servidores }) => {
        this.padroes.set(padroes);
        const doSetor = servidores.filter((s) => s.setorId === setorId);
        this.servidoresSetor.set(doSetor);
        if (!preserveSelection) {
          this.selectedServidorIds.set(new Set(doSetor.map((s) => s.id)));
        } else {
          const existing = new Set((this.escala()?.servidores ?? []).map((s) => s.servidorId));
          this.selectedServidorIds.set(existing.size ? existing : new Set(doSetor.map((s) => s.id)));
        }
        this.ensureInicioCicloControls();
        afterLoad?.();
      },
      error: () => this.error.set('Não foi possível carregar servidores e padrões do setor.'),
    });
  }

  private hydrateSelectionFromEscala(escala: EscalaDetail): void {
    const padroesById = new Map(this.padroes().map((p) => [p.id, p]));
    const codes = new Set<RegimeCodigo>();
    const inicio = new Map<string, string>();
    const porServidor = new Map<string, Set<RegimeCodigo>>();

    for (const s of escala.servidores) {
      const regs = new Set<RegimeCodigo>();
      for (const j of s.jornadas) {
        const fromPadrao = j.padraoEscalaId
          ? (padroesById.get(j.padraoEscalaId)?.codigo as RegimeCodigo | undefined)
          : undefined;
        const inferred = this.inferRegimeCodigo(j.recorrenciaTipo, j.diasTrabalho, j.diasFolga, j.tipoJornada);
        const codigo = fromPadrao && this.isRegimeCodigo(fromPadrao) ? fromPadrao : inferred;
        if (!codigo) continue;
        codes.add(codigo);
        regs.add(codigo);
        if (j.dataInicioCiclo) {
          inicio.set(s.servidorId, j.dataInicioCiclo.slice(0, 10));
        }
      }
      if (regs.size) porServidor.set(s.servidorId, regs);
    }

    this.regimesSelected.set([...codes]);
    this.servidorRegimes.set(porServidor);
    this.servidorInicioCiclo.set(inicio);
    this.selectedServidorIds.set(new Set(escala.servidores.map((s) => s.servidorId)));
    this.ensureInicioCicloControls();
    this.lastAppliedRegimesFingerprint = this.regimesFingerprint();
  }

  private isRegimeCodigo(value: string): value is RegimeCodigo {
    return value === 'EXP_ADM' || value === '12X36' || value === '24X72';
  }

  private inferRegimeCodigo(
    recorrencia: string,
    diasTrabalho?: number | null,
    diasFolga?: number | null,
    tipoJornada?: string,
  ): RegimeCodigo | null {
    if (recorrencia === 'CicloPlantao') {
      if (diasTrabalho === 1 && diasFolga === 1) return '12X36';
      if (diasTrabalho === 1 && diasFolga === 3) return '24X72';
    }
    if (
      tipoJornada === 'Administrativo' ||
      tipoJornada === 'Expediente' ||
      recorrencia === 'DiasSemana'
    ) {
      return 'EXP_ADM';
    }
    return null;
  }

  isRegimeSelected(codigo: RegimeCodigo): boolean {
    return this.regimesSelected().includes(codigo);
  }

  toggleRegime(codigo: RegimeCodigo): void {
    const current = this.regimesSelected();
    this.regimesSelected.set(
      current.includes(codigo) ? current.filter((c) => c !== codigo) : [...current, codigo],
    );
    this.ensureInicioCicloControls();
    this.markDirty();
  }

  isServidorSelected(id: string): boolean {
    return this.selectedServidorIds().has(id);
  }

  toggleServidor(id: string): void {
    const set = new Set(this.selectedServidorIds());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.selectedServidorIds.set(set);
    this.ensureInicioCicloControls();
    this.markDirty();
  }

  isServidorRegimeSelected(servidorId: string, codigo: RegimeCodigo): boolean {
    return this.servidorRegimes().get(servidorId)?.has(codigo) ?? false;
  }

  toggleServidorRegime(servidorId: string, codigo: RegimeCodigo): void {
    const map = new Map(this.servidorRegimes());
    const set = new Set(map.get(servidorId) ?? []);
    if (set.has(codigo)) set.delete(codigo);
    else set.add(codigo);
    map.set(servidorId, set);
    this.servidorRegimes.set(map);
    this.markDirty();
  }

  continuar2(): void {
    this.enterStep3();
  }

  /** Valida passo 2, regenera draft se necessário e avança para o passo 3. */
  private enterStep3(): boolean {
    if (!this.regimesSelected().length && !this.isEditMode()) {
      this.error.set('Selecione ao menos um regime de trabalho.');
      return false;
    }
    if (!this.selectedServidorIds().size) {
      this.error.set('Selecione ao menos um servidor.');
      return false;
    }
    if (this.needsInicioCiclo()) {
      this.ensureInicioCicloControls();
      this.syncInicioCicloFromControls();
      const semInicio = Array.from(this.selectedServidorIds()).some(
        (id) => !this.servidorInicioCiclo().get(id),
      );
      if (semInicio) {
        this.error.set('Informe o dia de início do ciclo para cada servidor selecionado.');
        return false;
      }
    }

    this.error.set(null);
    this.markDirty();

    const fingerprint = this.regimesFingerprint();
    const current = this.escala();
    const hasRegimeSelection = this.regimesSelected().length > 0;
    const tipo = hasRegimeSelection
      ? this.deriveTipoFuncionamento()
      : (current?.tipoFuncionamento ?? 'Expediente');
    const regimesChanged =
      hasRegimeSelection &&
      (fingerprint !== this.lastAppliedRegimesFingerprint ||
        (current != null && current.tipoFuncionamento !== tipo));
    const mustRegenerate = this.needsInicioCiclo() || regimesChanged;

    if (this.isEditMode() && current) {
      this.rebuildDraftFromSelection(current, {
        keepOcorrencias: !mustRegenerate,
        tipoFuncionamento: tipo,
      });
    } else if (current && mustRegenerate) {
      this.syncInicioCicloFromControls();
      this.regenerateAllPlantaoOcorrencias();
      const e = this.escala();
      if (e && e.tipoFuncionamento !== tipo) {
        this.escala.set({ ...e, tipoFuncionamento: tipo });
      }
    } else if (!current) {
      this.buildLocalDraft();
    } else {
      // Só atualiza lista de servidores mantendo edições, mas sincroniza tipo.
      this.rebuildDraftFromSelection(current, {
        keepOcorrencias: true,
        tipoFuncionamento: tipo,
      });
    }

    this.lastAppliedRegimesFingerprint = fingerprint;
    this.step.set(3);
    this.loadStep3Data();
    return true;
  }

  /** UI de tarde (T) no passo 2: só com regime único EXP_ADM. */
  showExpedienteTardeForServidor(_servidorId: string): boolean {
    return this.singleRegime() === 'EXP_ADM';
  }

  private deriveTipoFuncionamento(): TipoFuncionamento {
    return this.regimesSelected().some((r) => r === '12X36' || r === '24X72')
      ? 'VinteQuatroHoras'
      : 'Expediente';
  }

  private regimesFingerprint(): string {
    const global = [...this.regimesSelected()].sort().join(',');
    const inicios = Array.from(this.selectedServidorIds())
      .sort()
      .map((id) => `${id}:${this.servidorInicioCiclo().get(id) ?? ''}`)
      .join('|');
    return `${global}#${inicios}`;
  }

  private buildLocalDraft(): void {
    const v = this.step1Form.getRawValue();
    const ano = Number(v.ano);
    const mes = Number(v.mes);
    const dataInicio = firstDayOfMonth(ano, mes);
    const dataFim = lastDayOfMonth(ano, mes);
    const days = daysInRange(dataInicio, dataFim);
    const setorOpt = this.setorOptions().find((s) => s.value === v.setorId);
    const tipoFuncionamento = this.deriveTipoFuncionamento();

    const selected = this.servidoresSetor().filter((s) => this.selectedServidorIds().has(s.id));
    const servidores: EscalaServidor[] = selected.map((s, index) => ({
      id: `local-${s.id}`,
      servidorId: s.id,
      cargoId: s.cargoId,
      ordem: index,
      servidorNome: s.nome,
      matricula: s.matricula,
      cargoNome: s.cargo,
      cargoCodigo: s.cargoCodigo || s.cargo,
      jornadas: [],
      ocorrencias: this.buildOcorrenciasForServidor(s.id, days),
    }));

    this.escala.set({
      id: '',
      identificacao: `Escala ${setorOpt?.label ?? ''} ${String(mes).padStart(2, '0')}/${ano}`,
      setorId: v.setorId,
      setorNome: setorOpt?.label?.split('—')[1]?.trim() ?? '',
      setorSigla: setorOpt?.label?.split('—')[0]?.trim() ?? '',
      ano,
      mes,
      dataInicio,
      dataFim,
      tipoFuncionamento,
      status: 'Rascunho',
      createdAt: new Date().toISOString(),
      createdBy: this.auth.currentUser()?.displayName ?? null,
      cargaHorariaPresencial: 0,
      cargaHorariaRemota: 0,
      servidores,
    });
    this.recalcCargas();
    this.markDirty();
  }

  private rebuildDraftFromSelection(
    base: EscalaDetail,
    opts: { keepOcorrencias: boolean; tipoFuncionamento?: TipoFuncionamento },
  ): void {
    const days = daysInRange(base.dataInicio, base.dataFim);
    const selected = this.servidoresSetor().filter((s) => this.selectedServidorIds().has(s.id));
    const existingById = new Map(base.servidores.map((s) => [s.servidorId, s]));
    const tipo = opts.tipoFuncionamento ?? this.deriveTipoFuncionamento();

    const servidores: EscalaServidor[] = selected.map((s, index) => {
      const prev = existingById.get(s.id);
      if (prev && opts.keepOcorrencias) {
        return { ...prev, ordem: index };
      }
      return {
        id: prev?.id ?? `local-${s.id}`,
        servidorId: s.id,
        cargoId: s.cargoId,
        ordem: index,
        servidorNome: s.nome,
        matricula: s.matricula,
        cargoNome: s.cargo,
        cargoCodigo: s.cargoCodigo || s.cargo,
        jornadas: [],
        ocorrencias: this.buildOcorrenciasForServidor(s.id, days),
      };
    });

    this.escala.set({ ...base, tipoFuncionamento: tipo, servidores });
    this.recalcCargas();
    this.markDirty();
  }

  private buildOcorrenciasForServidor(servidorId: string, days: string[]): EscalaOcorrencia[] {
    return buildOcorrenciasForServidor({
      servidorId,
      days,
      regimesSelected: this.regimesSelected(),
      padroesByCodigo: this.padroesByCodigo(),
      servidorInicioCiclo: this.servidorInicioCiclo(),
    });
  }

  // ---------- Step 3 ----------

  private loadStep3Data(): void {
    const e = this.escala();
    if (!e) return;
    const ids = e.servidores.map((s) => s.servidorId);
    if (!this.formServidorId() && ids.length) {
      this.formServidorId.set(ids[0]);
      this.formServidorControl.setValue(ids[0], { emitEvent: false });
    }
    this.afastamentosApi
      .list({ ano: e.ano, mes: e.mes, setorId: e.setorId, servidorIds: ids })
      .subscribe({
        next: (items) => {
          this.afastamentos.set(items);
          this.applyAfastamentosToDraft(items);
          // Dias T marcados no passo 2 — após afastamentos para não sobrescrever FR/LM/etc.
          this.aplicarExpedienteTarde();
        },
        error: () => {
          this.afastamentos.set([]);
          this.aplicarExpedienteTarde();
        },
      });
  }

  private applyAfastamentosToDraft(items: AfastamentoItem[]): void {
    const e = this.escala();
    if (!e || !items.length) return;
    let changed = false;
    const servidores = e.servidores.map((s) => {
      const ocorrencias = s.ocorrencias.map((o) => {
        const day = o.data.slice(0, 10);
        const af = items.find(
          (a) =>
            a.servidorId === s.servidorId &&
            day >= a.dataInicio.slice(0, 10) &&
            day <= a.dataFim.slice(0, 10),
        );
        if (!af) return o;
        if (o.tipoOcorrenciaCodigo === af.tipoOcorrenciaCodigo) return o;
        changed = true;
        return { ...o, tipoOcorrenciaCodigo: af.tipoOcorrenciaCodigo, horas: null };
      });
      return { ...s, ocorrencias };
    });
    if (changed) {
      this.escala.set({ ...e, servidores });
      this.recalcCargas();
      this.markDirty();
    }
  }

  openAfastamentoModal(): void {
    const e = this.escala();
    if (!e) return;

    if (!this.auth.canAccess('afastamentos.criar', e.setorId)) {
      this.toast.showError('Só é possível cadastrar afastamento para servidores do seu setor.');
      return;
    }

    const data: AfastamentoDialogData = {
      servidorOptions: this.formServidorOptions(),
      defaultServidorId: this.formServidorId() || undefined,
      dataInicioPadrao: e.dataInicio.slice(0, 10),
      dataFimPadrao: e.dataFim.slice(0, 10),
    };
    this.dialog
      .open(AfastamentoDialog, {
        data,
        width: '640px',
        maxWidth: '95vw',
        panelClass: 'pci-app-dialog-panel',
      })
      .afterClosed()
      .subscribe((created: AfastamentoItem | false | undefined) => {
        if (!created) return;
        const merged = [
          created,
          ...this.afastamentos().filter((a) => a.id !== created.id),
        ];
        this.afastamentos.set(merged);
        this.applyAfastamentosToDraft([created]);
        this.feedback.showSuccess('Afastamento cadastrado com sucesso.');
        this.loadStep3Data();
      });
  }

  isHomeOfficeDay(servidorId: string, day: number): boolean {
    return this.homeOfficeDays().get(servidorId)?.has(day) ?? false;
  }

  toggleHomeOfficeDay(servidorId: string, day: number): void {
    const map = new Map(this.homeOfficeDays());
    const set = new Set(map.get(servidorId) ?? []);
    if (set.has(day)) set.delete(day);
    else set.add(day);
    map.set(servidorId, set);
    this.homeOfficeDays.set(map);
  }

  aplicarHomeOffice(): void {
    const e = this.escala();
    if (!e) return;

    const servidores = e.servidores.map((servidor) => {
      if (!this.hoServidores().some((h) => h.servidorId === servidor.servidorId)) {
        return servidor;
      }
      const hoDays = this.homeOfficeDays().get(servidor.servidorId);
      if (!hoDays || hoDays.size === 0) {
        return servidor;
      }
      const ocorrencias = this.matrizDays().map((day) => {
        const existing = servidor.ocorrencias.find((o) => o.data.slice(0, 10) === day);
        if (this.hasBlockingAfastamento(servidor.servidorId, day)) {
          return existing ?? this.emptyOc(day);
        }
        const weekday = new Date(day + 'T00:00:00').getDay();
        if (hoDays.has(weekday)) {
          return this.oc(day, 'TL6', 6);
        }
        // Demais dias: preservar o que já estava (ex.: T, M, D).
        return existing ?? this.emptyOc(day);
      });
      return { ...servidor, ocorrencias };
    });

    this.escala.set({ ...e, servidores });
    this.recalcCargas();
    this.markDirty();
  }

  isExpedienteTardeDay(servidorId: string, day: number): boolean {
    return this.expedienteTardeDays().get(servidorId)?.has(day) ?? false;
  }

  toggleExpedienteTardeDay(servidorId: string, day: number): void {
    const map = new Map(this.expedienteTardeDays());
    const set = new Set(map.get(servidorId) ?? []);
    if (set.has(day)) set.delete(day);
    else set.add(day);
    map.set(servidorId, set);
    this.expedienteTardeDays.set(map);
  }

  /** Aplica dias T marcados no passo 2 sobre a matriz já gerada (só servidores EXP_ADM com dias). */
  aplicarExpedienteTarde(): void {
    const e = this.escala();
    if (!e) return;

    const servidores = e.servidores.map((servidor) => {
      if (!this.showExpedienteTardeForServidor(servidor.servidorId)) {
        return servidor;
      }
      const tardeDays = this.expedienteTardeDays().get(servidor.servidorId);
      if (!tardeDays || tardeDays.size === 0) {
        return servidor;
      }
      const ocorrencias = this.matrizDays().map((day) => {
        const existing = servidor.ocorrencias.find((o) => o.data.slice(0, 10) === day);
        if (this.hasBlockingAfastamento(servidor.servidorId, day)) {
          return existing ?? this.emptyOc(day);
        }
        const weekday = new Date(day + 'T00:00:00').getDay();
        if (weekday === 0 || weekday === 6) {
          return existing ?? this.oc(day, 'D');
        }
        if (tardeDays.has(weekday)) {
          return this.oc(day, 'T', 6, '14:00', '20:00');
        }
        return existing ?? this.emptyOc(day);
      });
      return { ...servidor, ocorrencias };
    });

    this.escala.set({ ...e, servidores });
    this.recalcCargas();
    this.markDirty();
  }

  private hasBlockingAfastamento(servidorId: string, day: string): boolean {
    return this.afastamentos().some(
      (a) =>
        a.servidorId === servidorId &&
        ['FR', 'LM', 'LP', 'LO'].includes(a.tipoOcorrenciaCodigo) &&
        day >= a.dataInicio.slice(0, 10) &&
        day <= a.dataFim.slice(0, 10),
    );
  }

  onStep3TabIndexChange(index: number): void {
    const tab = this.step3TabOrder[index];
    if (tab) this.step3Tab.set(tab);
  }

  isSelected(servidorId: string, day: string): boolean {
    return this.selectedCells().some((c) => c.servidorId === servidorId && c.day === day);
  }

  onCellMouseDown(servidorId: string, day: string, event: MouseEvent): void {
    const exists = this.isSelected(servidorId, day);
    if (event.ctrlKey || event.metaKey || event.shiftKey) {
      this.selectedCells.set(
        exists
          ? this.selectedCells().filter((c) => !(c.servidorId === servidorId && c.day === day))
          : [...this.selectedCells(), { servidorId, day }],
      );
    } else if (!exists || this.selectedCells().length > 1) {
      this.selectedCells.set([{ servidorId, day }]);
    }
  }

  onCellKeydown(_servidorId: string, _day: string, event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey)) return;
    const key = event.key.toLowerCase();
    if (key === 'c') {
      event.preventDefault();
      this.copySelection();
    } else if (key === 'v') {
      event.preventDefault();
      this.pasteSelection();
    }
  }

  copySelection(): void {
    const cells = this.selectedCells();
    if (!cells.length) return;
    this.clipboard = cells.map((c) => this.codeFor(c.servidorId, c.day));
  }

  pasteSelection(): void {
    const cells = this.selectedCells();
    if (!cells.length || !this.clipboard.length) return;
    cells.forEach((cell, idx) => {
      this.setLocalCell(cell.servidorId, cell.day, this.clipboard[idx % this.clipboard.length]);
    });
  }

  onMatrizCodeChange(servidorId: string, day: string, code: string): void {
    this.setLocalCell(servidorId, day, code);
  }

  onFormCodigoInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const upper = input.value.toUpperCase();
    if (input.value !== upper) {
      const start = input.selectionStart;
      const end = input.selectionEnd;
      input.value = upper;
      if (start != null && end != null) {
        input.setSelectionRange(start, end);
      }
    }
  }

  onFormCodigoBlur(day: string, event: FocusEvent): void {
    const servidorId = this.formServidorId();
    if (!servidorId) return;
    const input = event.target as HTMLInputElement;
    const value = input.value.trim().toUpperCase();
    input.value = value;
    if (!value) return;
    if (!isValidOcorrenciaCodigo(value)) {
      input.value = this.codeFor(servidorId, day);
      return;
    }
    if (value === this.codeFor(servidorId, day)) return;
    this.setLocalCell(servidorId, day, value);
  }

  private setLocalCell(servidorId: string, day: string, codigo: string): void {
    const e = this.escala();
    if (!e) return;
    const code = codigo.trim().toUpperCase();
    if (code && !isValidOcorrenciaCodigo(code)) return;
    const servidores = e.servidores.map((s) => {
      if (s.servidorId !== servidorId) return s;
      const ocorrencias = [...s.ocorrencias];
      const idx = ocorrencias.findIndex((o) => o.data.slice(0, 10) === day);
      const next = this.oc(day, code, code === 'TL6' || code === 'M' || code === 'T' ? 6 : null);
      if (idx >= 0) ocorrencias[idx] = { ...ocorrencias[idx], ...next, id: ocorrencias[idx].id };
      else ocorrencias.push(next);
      return { ...s, ocorrencias };
    });
    this.escala.set({ ...e, servidores });
    this.recalcCargas();
    this.markDirty();
  }

  continuar3(): void {
    this.step.set(4);
  }

  // ---------- Step 4 ----------

  onReviewTabIndexChange(index: number): void {
    const tab = this.reviewTabOrder[index];
    if (tab) this.reviewTab.set(tab);
  }

  downloadPdf(layout: 'horizontal' | 'vertical'): void {
    const persistFirst = this.dirty() || !this.isPersisted;
    const run = (id: string) => {
      this.api.downloadPdf(id, layout).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `escala-${layout}.pdf`;
          a.click();
          URL.revokeObjectURL(url);
          this.feedback.showSuccess('PDF gerado com sucesso.');
        },
        error: (err: { error?: { message?: string } }) => {
          const msg = err.error?.message ?? 'Falha ao exportar PDF.';
          this.error.set(msg);
          this.toast.showError(msg);
        },
      });
    };

    if (!persistFirst) {
      run(this.escalaId);
      return;
    }
    this.persistDraft$().subscribe({
      next: (escala) => run(escala.id),
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  imprimir(): void {
    const layout = this.reviewTab() === 'vertical' ? 'vertical' : 'horizontal';
    this.downloadPdf(layout);
  }

  salvarRascunho(): void {
    this.persistDraft$().subscribe({
      next: () => {
        this.dirty.set(false);
        this.skipDeactivateGuard = true;
        this.feedback.showSuccess('Rascunho salvo com sucesso.');
        void this.router.navigateByUrl('/escalas');
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  finalizar(): void {
    this.persistDraft$()
      .pipe(switchMap((escala) => this.api.finalizar(escala.id)))
      .subscribe({
        next: () => {
          this.dirty.set(false);
          this.skipDeactivateGuard = true;
          this.feedback.showSuccess('Escala finalizada com sucesso.');
          void this.router.navigateByUrl('/escalas');
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  private persistDraft$(): Observable<EscalaDetail> {
    const draft = this.escala();
    if (!draft) {
      return new Observable((sub) => {
        sub.error({ error: { message: 'Não há escala para salvar.' } });
      });
    }

    this.working.set(true);
    this.error.set(null);

    const servidorIds = draft.servidores.map((s) => s.servidorId);
    const cells = this.collectCells(draft);
    const mes = Number(this.step1Form.getRawValue().mes) || draft.mes;
    const ano = Number(this.step1Form.getRawValue().ano) || draft.ano;

    const afterReady$ = this.isPersisted
      ? this.syncServidores$(draft.id, servidorIds)
      : this.api
          .create({
            setorId: draft.setorId,
            ano,
            mes,
            tipoFuncionamento: draft.tipoFuncionamento,
          })
          .pipe(switchMap((escala) => this.api.addServidores(escala.id, servidorIds)));

    // Draft já contém a matriz completa; sync em lote evita gerar+upsert (concorrência EF).
    return afterReady$.pipe(
      switchMap((escala) => {
        this.escala.set({ ...draft, id: escala.id, identificacao: escala.identificacao });
        return this.syncCells$(escala.id, cells);
      }),
      tap((escala) => {
        this.escala.set(escala);
        this.working.set(false);
      }),
    );
  }
  private syncServidores$(id: string, servidorIds: string[]): Observable<EscalaDetail> {
    return this.api.get(id).pipe(
      switchMap((current) => {
        const existing = new Set(current.servidores.map((s) => s.servidorId));
        const toAdd = servidorIds.filter((sid) => !existing.has(sid));
        const toRemove = current.servidores
          .filter((s) => !servidorIds.includes(s.servidorId))
          .map((s) => s.servidorId);

        let chain: Observable<EscalaDetail> = of(current);
        if (toAdd.length) {
          chain = chain.pipe(switchMap(() => this.api.addServidores(id, toAdd)));
        }
        for (const sid of toRemove) {
          chain = chain.pipe(
            switchMap(() =>
              this.api.removeServidor(id, sid).pipe(switchMap(() => this.api.get(id))),
            ),
          );
        }
        return chain;
      }),
    );
  }

  private collectCells(draft: EscalaDetail): { servidorId: string; payload: CellPayload }[] {
    const result: { servidorId: string; payload: CellPayload }[] = [];
    for (const s of draft.servidores) {
      for (const o of s.ocorrencias) {
        const code = o.tipoOcorrenciaCodigo?.trim();
        if (!code) continue;
        result.push({
          servidorId: s.servidorId,
          payload: {
            data: o.data.slice(0, 10),
            tipoOcorrenciaCodigo: code,
            horaInicio: o.horaInicio,
            horaFim: o.horaFim,
            horas: o.horas,
          },
        });
      }
    }
    return result;
  }

  private syncCells$(
    id: string,
    cells: { servidorId: string; payload: CellPayload }[],
  ): Observable<EscalaDetail> {
    return this.api.syncOcorrencias(id, {
      itens: cells.map((c) => ({
        servidorId: c.servidorId,
        data: c.payload.data,
        tipoOcorrenciaCodigo: c.payload.tipoOcorrenciaCodigo,
        horaInicio: c.payload.horaInicio,
        horaFim: c.payload.horaFim,
        horas: c.payload.horas,
      })),
    });
  }

  voltarStep(n: WizardStep): void {
    this.error.set(null);
    this.step.set(n);
  }

  cancelar(): void {
    void this.router.navigateByUrl('/escalas');
  }

  codeFor(servidorId: string, day: string): string {
    const s = this.escala()?.servidores.find((x) => x.servidorId === servidorId);
    return s?.ocorrencias.find((o) => o.data.slice(0, 10) === day)?.tipoOcorrenciaCodigo ?? '';
  }

  fmt(iso?: string | null): string {
    if (!iso) return '—';
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  private markDirty(): void {
    this.dirty.set(true);
  }

  private recalcCargas(): void {
    const e = this.escala();
    if (!e) return;
    const folga = new Set(['D', 'F', 'FR', 'LP', 'LM', 'LO', 'R']);
    const servidores = e.servidores.map((s) => {
      let pres = 0;
      let rem = 0;
      for (const o of s.ocorrencias) {
        const code = (o.tipoOcorrenciaCodigo || '').toUpperCase();
        const h = o.horas ?? 0;
        if (code.startsWith('TL')) rem += h;
        else if (code && !folga.has(code)) pres += h;
      }
      return { ...s, cargaHorariaPresencial: pres, cargaHorariaRemota: rem };
    });
    const cargaHorariaPresencial = servidores.reduce((a, s) => a + (s.cargaHorariaPresencial ?? 0), 0);
    const cargaHorariaRemota = servidores.reduce((a, s) => a + (s.cargaHorariaRemota ?? 0), 0);
    this.escala.set({ ...e, servidores, cargaHorariaPresencial, cargaHorariaRemota });
  }

  private fail(message?: string): void {
    const msg = message ?? 'Operação não concluída.';
    this.error.set(msg);
    this.toast.showError(msg);
    this.working.set(false);
  }
}
