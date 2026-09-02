import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciAvatarComponent,
  PciButtonComponent,
  PciCardComponent,
  PciCardContentComponent,
  PciCheckboxComponent,
  PciIconButtonComponent,
  PciIconComponent,
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
import type { NucleoListItem, ServidorListItem, SetorListItem } from '../../../admin/models/admin.models';
import { EscalaResumidaManager } from '../../components/escala-resumida-manager/escala-resumida-manager';
import { EscalasResumidasApiService } from '../../services/escalas-resumidas-api.service';
import type { EscalaResumidaDetail } from '../../models/escalas-resumidas.models';
import {
  AfastamentosApiService,
  AfastamentoItem,
} from '../../../afastamentos/services/afastamentos-api.service';
import { AppFormColDirective } from '../../../../shared/form-layout';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { ESCALAS_ROUTE_PAGES } from '../../escalas-route-pages';
import { EscalasApiService } from '../../services/escalas-api.service';
import type {
  EscalaDetail,
  EscalaListItem,
  EscalaOcorrencia,
  EscalaServidor,
  GerarEscalaItemPayload,
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
  buildOcorrenciasFromCicloDerivado,
  normalizeDay,
  primeiraDataParaPosicao,
  type RegimeCodigo,
} from '../../utils/escala-ocorrencia.builder';

type WizardStep = 'periodo' | 'servidores' | 'resumida' | 'afastamentos' | 'revisao';
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
    PciAvatarComponent,
    PciButtonComponent,
    PciCardComponent,
    PciCardContentComponent,
    PciCheckboxComponent,
    PciIconButtonComponent,
    PciIconComponent,
    PciInputComponent,
    PciPageHeaderComponent,
    PciSelectComponent,
    PciStackComponent,
    PciStepComponent,
    PciStepperComponent,
    PciTabComponent,
    PciTabsComponent,
    AppFormColDirective,
    EscalaMatrix,
    EscalaResumidaManager,
  ],
  templateUrl: './escala-form.html',
  styleUrl: './escala-form.scss',
})
export class EscalaForm implements OnInit {
  @ViewChild('stepperRef') private readonly stepperRef?: PciStepperComponent;

  private readonly api = inject(EscalasApiService);
  private readonly resumidaApi = inject(EscalasResumidasApiService);
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
  private readonly initialNavState = this.router.getCurrentNavigation()?.extras.state as
    | { openStep?: number; abrirResumida?: boolean }
    | undefined;
  private readonly initialOpenStep: WizardStep =
    this.initialNavState?.openStep === 2 ? 'servidores' : 'afastamentos';
  /** Só relevante quando `initialOpenStep === 3` — veio de `copiarOrigem` com o checkbox de
   * escala resumida marcado; abre o painel antes de mostrar o passo 3. */
  private readonly initialAbrirResumida = !!this.initialNavState?.abrirResumida;

  readonly routePages = ESCALAS_ROUTE_PAGES;

  readonly step = signal<WizardStep>('periodo');
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
    usarEscalaResumida: [false],
  });

  /** `getRawValue().setorId` não é um signal — um `computed()` que só lê isso nunca seria
   * invalidado quando o usuário troca a seleção no select (fica preso no valor de quando foi
   * lido pela 1ª vez). Este signal espelha o controle e é o que os computeds abaixo devem ler. */
  private readonly setorIdValue = toSignal(this.step1Form.controls.setorId.valueChanges, {
    initialValue: this.step1Form.controls.setorId.value,
  });
  private readonly mesValue = toSignal(this.step1Form.controls.mes.valueChanges, {
    initialValue: this.step1Form.controls.mes.value,
  });
  private readonly anoValue = toSignal(this.step1Form.controls.ano.valueChanges, {
    initialValue: this.step1Form.controls.ano.value,
  });

  private static readonly NUCLEO_OPTION_PREFIX = 'nucleo:';

  readonly setoresRaw = signal<SetorListItem[]>([]);
  readonly nucleosDoUsuario = signal<NucleoListItem[]>([]);
  /** Combina os setores geridos com uma opção por núcleo gerido — uma escala pode ter dono
   * setor ou núcleo (ex.: servidores lotados diretamente no núcleo). */
  readonly setorOptions = computed<PciSelectOption[]>(() => {
    const setores = this.setoresRaw().map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id }));
    const nucleos = this.nucleosDoUsuario().map((n) => ({
      label: `${n.sigla} — ${n.nome}`,
      value: `${EscalaForm.NUCLEO_OPTION_PREFIX}${n.id}`,
    }));
    return [...setores, ...nucleos];
  });
  readonly showSetorSelect = computed(
    () => this.setorOptions().length > 1 || this.auth.isChefeNucleo(),
  );
  readonly setorUnicoLabel = signal<string | null>(null);

  /** Período e setor escolhidos no passo 1 — exibidos no painel "Como funciona?" do passo de
   * servidores pra dar contexto sem precisar voltar. */
  readonly periodoLabel = computed(() => {
    const mes = Number(this.mesValue());
    const ano = this.anoValue();
    const nome = MES_NOMES[mes];
    return nome && ano ? `${nome}/${ano}` : '—';
  });
  readonly setorAtualLabel = computed(() => {
    const setorId = this.setorIdValue();
    if (!setorId) return this.setorUnicoLabel() ?? '—';
    return this.setorOptions().find((o) => o.value === setorId)?.label ?? this.setorUnicoLabel() ?? '—';
  });

  private isNucleoOption(value: string | null | undefined): boolean {
    return !!value && value.startsWith(EscalaForm.NUCLEO_OPTION_PREFIX);
  }

  private extractNucleoId(value: string): string {
    return value.slice(EscalaForm.NUCLEO_OPTION_PREFIX.length);
  }

  /** Escala sendo criada tem dono núcleo (não setor) — esconde "Copiar escala existente" e
   * muda o pool de servidores do passo 2 pra todo o núcleo. */
  readonly escalaDeNucleo = computed(() => this.isNucleoOption(this.setorIdValue()));

  readonly origemOptions = signal<PciSelectOption[]>([]);
  readonly origemEscalaId = signal('');
  readonly usarEscalaResumida = signal(false);

  /** Núcleo em uso no passo 1 — o do setor escolhido, ou o núcleo escolhido diretamente —
   * usado tanto pra oferecer a seção de "outros setores do núcleo" dentro do painel de escala
   * resumida quanto, quando a escala em si é de núcleo, como núcleo da própria escala. */
  readonly nucleoIdDoSetor = computed(() => {
    const setorId = this.setorIdValue();
    if (this.isNucleoOption(setorId)) return this.extractNucleoId(setorId);
    return this.setoresRaw().find((s) => s.id === setorId)?.nucleoId ?? null;
  });

  /** Qualquer um que possa criar escala no setor/núcleo escolhido (chefia direta, chefia do
   * núcleo, ou visão institucional com permissão de criar escala) pode oferecer o passo
   * opcional de escala resumida — não só quem tem chefia formal cadastrada. */
  readonly podeCriarResumida = computed(() => {
    const setorId = this.setorIdValue();
    if (!setorId) return false;
    if (this.isNucleoOption(setorId)) {
      return this.auth.canAccessEscala('escalas.criar', null, this.extractNucleoId(setorId));
    }
    return this.auth.canAccessEscala('escalas.criar', setorId, null);
  });

  /** Só quem também chefia o núcleo (não só este setor) vê a seção de incluir outros setores
   * do núcleo dentro do painel de escala resumida. */
  readonly podeGerenciarOutrosSetoresDoNucleo = computed(() => {
    const nucleoId = this.nucleoIdDoSetor();
    return !!nucleoId && this.auth.isChefeNucleo(nucleoId);
  });

  /** Container da escala resumida: o núcleo do setor escolhido quando existir; senão o próprio
   * setor (resumida de setor único, sem seção de "setores participantes" — o setor sozinho já
   * é o único container). `null` só quando nada foi escolhido ainda ou a opção é diretamente
   * um núcleo mas ainda sem id resolvido. */
  readonly resumidaContainer = computed<{ nucleoId: string } | { setorId: string } | null>(() => {
    const nucleoId = this.nucleoIdDoSetor();
    if (nucleoId) return { nucleoId };
    const setorId = this.setorIdValue();
    if (!setorId || this.isNucleoOption(setorId)) return null;
    return { setorId };
  });

  /** O checkbox "usar escala resumida" está marcado por quem pode usá-lo — independe de
   * copiar ou criar do zero (usado tanto pro gate do passo quanto pra decidir se o fluxo de
   * cópia deve abrir o painel depois do reload). */
  readonly resumidaDesejada = computed(() => this.podeCriarResumida() && this.usarEscalaResumida());

  /** O passo só é ofertado quando "criar do zero" (copiar já traz regimes/servidores prontos
   * do período de origem, mas ainda pode abrir o painel depois do reload — ver `copiarOrigem`). */
  readonly resumidaAtiva = computed(() => this.resumidaDesejada() && !this.origemEscalaId());

  readonly resumidaHandled = signal(false);
  readonly resumidaWorking = signal(false);
  readonly resumidaEscala = signal<EscalaResumidaDetail | null>(null);
  readonly resumidaAnteriorId = signal<string | null>(null);
  readonly resumidaAnteriorLabel = signal<string | null>(null);
  /** true só quando a resumida foi CRIADA por esta sessão do wizard (`criarResumidaDoZero`/
   * `copiarResumidaAnterior`) — não quando reaproveitada de um núcleo/período que já tinha
   * uma (`resolverResumida` "encontrou" uma existente, compartilhada com outro setor). Usada
   * só pra decidir se a resumida deve ser descartada automaticamente quando o usuário sai do
   * wizard sem salvar a escala normal como rascunho (ver `cleanupResumidaOrfa$`) — nunca se
   * aplica a uma resumida que já pertencia a outra escala. */
  readonly resumidaCriadaNestaSessao = signal(false);
  /** Ciclo pessoal derivado do rodízio da escala resumida por servidor (posição no pool +
   * âncora) — separado de `servidorRegimes` porque o tamanho do pool é arbitrário e não
   * corresponde a nenhum dos 4 `RegimeCodigo` fixos. */
  readonly servidorCicloResumida = signal<Map<string, { ancora: string; tamanhoPool: number }>>(
    new Map(),
  );

  readonly periodoDuplicado = signal(false);

  readonly mesOptions: PciSelectOption[] = MES_NOMES.slice(1).map((label, i) => ({
    label,
    value: String(i + 1),
  }));

  readonly regimeOptions: { codigo: RegimeCodigo; label: string }[] = [
    { codigo: 'EXP_ADM', label: 'Expediente administrativo 6h' },
    { codigo: '12X36', label: 'Plantão 12h' },
    { codigo: '24X72', label: 'Plantão 24h' },
    { codigo: 'PT24_TL12', label: 'Plantão 24h + Laudo 12h' },
  ];
  readonly regimeSelectOptions: PciSelectOption[] = this.regimeOptions.map((opt) => ({
    label: opt.label,
    value: opt.codigo,
  }));

  readonly padroes = signal<PadraoEscala[]>([]);
  readonly padroesByCodigo = computed(() => new Map(this.padroes().map((p) => [p.codigo, p])));
  readonly servidoresSetor = signal<ServidorListItem[]>([]);
  private lastAppliedRegimesFingerprint = '';
  readonly selectedServidorIds = signal<Set<string>>(new Set());
  /** Cards com o bloco de "Expediente tarde" expandido, no passo 2 — só controla exibição,
   * não afeta os dados salvos. */
  readonly expandedServidorCards = signal<Set<string>>(new Set());
  /** Regime de plantão escolhido por servidor (um só por servidor) — Set por compatibilidade
   * com o formato usado ao restaurar de `EscalaJornada` existentes em `hydrateSelectionFromEscala`. */
  readonly servidorRegimes = signal<Map<string, Set<RegimeCodigo>>>(new Map());
  readonly servidorInicioCiclo = signal<Map<string, string>>(new Map());
  /** Valores dos selects de início de ciclo — só copiados para o Map ao avançar o passo. */
  readonly inicioCicloForm = this.fb.nonNullable.group({});
  /** Valores dos selects de regime por servidor — grava no Map a cada mudança. */
  readonly regimeForm = this.fb.nonNullable.group({});
  /** União dos regimes em uso entre os servidores selecionados — mantém a semântica que já
   * existia (tipo de funcionamento da escala, painel de home office) agora derivada da
   * escolha por servidor em vez de uma seleção global manual. */
  readonly regimesSelected = computed<RegimeCodigo[]>(() => {
    const set = new Set<RegimeCodigo>();
    for (const regs of this.servidorRegimes().values()) {
      for (const r of regs) set.add(r);
    }
    return [...set];
  });
  readonly multiRegime = computed(() => this.regimesSelected().length > 1);
  /** Algum servidor selecionado tem regime cíclico (12x36/24x72) — controla se o select de
   * "Início do ciclo" aparece pra ele (ver `isServidorRegimeCiclico`, que faz a checagem por
   * servidor individual). */
  readonly needsInicioCiclo = computed(() =>
    Array.from(this.servidorRegimes().values()).some((regs) =>
      [...regs].some((r) => r === '12X36' || r === '24X72' || r === 'PT24_TL12'),
    ),
  );

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

  /** Lista ordenada dos passos ativos — "resumida" só entra quando `resumidaAtiva()`, o que
   * desloca a posição visual de "afastamentos"/"revisao" na barra automaticamente. */
  readonly steps = computed<WizardStep[]>(() => {
    const base: WizardStep[] = ['periodo', 'servidores'];
    if (this.resumidaAtiva()) base.push('resumida');
    base.push('afastamentos', 'revisao');
    return base;
  });

  readonly stepperIndex = computed(() => Math.max(0, this.steps().indexOf(this.step())));

  /** Usado pelos `[completed]` do cabeçalho do stepper — true quando `id` já foi ultrapassado
   * pelo passo atual (posição anterior à do passo corrente na lista de passos ativos). */
  isStepPast(id: WizardStep): boolean {
    return this.steps().indexOf(id) < this.stepperIndex();
  }

  /** Toda troca de passo passa por aqui — `PciStepperComponent` só resincroniza o destaque
   * visual (`isActive`/`isCompleted` de cada `pci-step`) quando o próprio componente processa
   * um clique via `selectStep()`; mudar `[selectedIndex]` de fora não dispara isso (sem
   * `ngOnChanges` no componente). Chamar `selectStep` aqui força a barra a refletir o passo
   * real sempre, incluindo quando a mudança veio do botão "Voltar" (não de um clique na barra)
   * ou quando um clique num chip à frente foi rejeitado (o componente já tinha adiantado seu
   * `selectedIndex` interno de forma otimista antes de perguntar pro pai). */
  private setStep(n: WizardStep): void {
    this.step.set(n);
    this.syncStepperHeader();
  }

  /** `PciStepperComponent.selectStep()` sempre emite `selectedIndexChange`, mesmo quando
   * chamado programaticamente por aqui (não só em clique do usuário) — sem essa guarda, o
   * evento reentra em `onStepperIndexChange`, que pode chamar `syncStepperHeader()` de volta
   * (ex.: passo "Escala Resumida", índice 2) e criar um loop infinito de microtasks que trava
   * a aba sem nunca lançar exceção (o botão fica girando pra sempre). */
  private syncingStepperHeader = false;

  private syncStepperHeader(): void {
    queueMicrotask(() => {
      this.syncingStepperHeader = true;
      try {
        this.stepperRef?.selectStep(this.stepperIndex());
      } finally {
        this.syncingStepperHeader = false;
      }
    });
  }

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

    if (!editId && this.auth.isChefeNucleo()) {
      this.adminApi.listMeusNucleos().subscribe({
        next: (items) => this.nucleosDoUsuario.set(items),
        error: () => this.error.set('Não foi possível carregar os núcleos geridos por você.'),
      });
    }

    this.adminApi.listMeusSetores().subscribe({
      next: (setores) => {
        this.setoresRaw.set(setores);
        if (!editId && setores.length === 1 && !this.auth.isChefeNucleo()) {
          this.step1Form.controls.setorId.setValue(setores[0].id);
          this.step1Form.controls.setorId.disable({ emitEvent: false });
          this.setorUnicoLabel.set(`${setores[0].sigla} — ${setores[0].nome}`);
        } else if (!editId && setores.length >= 1 && this.auth.isChefeNucleo()) {
          this.step1Form.controls.setorId.setValue(setores[0].id);
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

    this.step1Form.controls.usarEscalaResumida.valueChanges.subscribe((v) => {
      this.usarEscalaResumida.set(!!v);
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
    for (const id of this.selectedServidorIds()) {
      if (!this.isServidorRegimeCiclico(id)) continue;
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

  /** Controles do select de regime por servidor — criados sob demanda a partir de
   * `selectedServidorIds`; grava no Map de `servidorRegimes` a cada mudança. */
  private ensureRegimeControls(): void {
    for (const id of this.selectedServidorIds()) {
      if (this.regimeForm.contains(id)) continue;
      const control = this.fb.nonNullable.control(this.servidorRegimeCodigo(id) ?? '');
      control.valueChanges.subscribe((val) => {
        const map = new Map(this.servidorRegimes());
        if (val) map.set(id, new Set([val as RegimeCodigo]));
        else map.delete(id);
        this.servidorRegimes.set(map);
        // Regime escolhido manualmente prevalece sobre um ciclo derivado da escala resumida
        // (passa a gerar ocorrências pelo padrão do regime, não pelo pool da resumida) — mas a
        // âncora já é conhecida (mesma data de início do ciclo configurada na escala resumida),
        // então não faz sentido perguntar de novo: pré-preenche "Início do ciclo" com ela.
        const ciclo = this.servidorCicloResumida().get(id);
        if (val && ciclo) {
          const ciclos = new Map(this.servidorCicloResumida());
          ciclos.delete(id);
          this.servidorCicloResumida.set(ciclos);

          const inicios = new Map(this.servidorInicioCiclo());
          inicios.set(id, ciclo.ancora);
          this.servidorInicioCiclo.set(inicios);
        }
        this.ensureInicioCicloControls();
        this.inicioCicloForm.get(id)?.setValue(this.servidorInicioCiclo().get(id) ?? '', { emitEvent: false });
        this.markDirty();
      });
      this.regimeForm.addControl(id, control);
    }
  }

  /** Regime único hoje atribuído a um servidor (ou null se ainda não escolhido). */
  servidorRegimeCodigo(servidorId: string): RegimeCodigo | null {
    const regs = this.servidorRegimes().get(servidorId);
    return regs && regs.size ? [...regs][0] : null;
  }

  isServidorRegimeCiclico(servidorId: string): boolean {
    const codigo = this.servidorRegimeCodigo(servidorId);
    return codigo === '12X36' || codigo === '24X72' || codigo === 'PT24_TL12';
  }

  /** Nomes dos servidores selecionados que ainda não têm regime de plantão escolhido. */
  private servidoresSemRegime(): string[] {
    const nomes: string[] = [];
    for (const id of this.selectedServidorIds()) {
      if (this.servidorRegimeCodigo(id)) continue;
      if (this.servidorCicloResumida().has(id)) continue;
      nomes.push(this.servidoresSetor().find((s) => s.id === id)?.nome ?? id);
    }
    return nomes;
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
    if (this.skipDeactivateGuard) return true;
    if (!this.dirty()) return this.cleanupResumidaOrfa$();
    return openConfirmDialog(this.dialog, {
      title: 'Alterações não salvas',
      message: 'Há alterações não salvas. Deseja salvar como rascunho antes de sair?',
      confirmLabel: 'Salvar rascunho',
      cancelLabel: 'Sair sem salvar',
    }).pipe(
      switchMap((save) => {
        if (!save) {
          this.dirty.set(false);
          return this.cleanupResumidaOrfa$();
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

  /** Descarta (best-effort) a escala resumida criada NESTA sessão de wizard, se o usuário sai
   * sem salvar a escala normal como rascunho — a resumida "pertence" à escala normal (ver
   * `vincularResumidaSeAplicavel$`) e não deve sobreviver sozinha, órfã, esperando pra
   * confundir a próxima tentativa de criar escala no mesmo núcleo/período. Nunca bloqueia a
   * navegação: falha de rede aqui não deve prender o usuário na tela, e uma resumida
   * reaproveitada de outro setor/tentativa (`resumidaCriadaNestaSessao` false) ou já vinculada
   * a uma escala salva nunca é tocada. */
  private cleanupResumidaOrfa$(): Observable<boolean> {
    const resumida = this.resumidaEscala();
    if (this.isEditMode() || !this.resumidaCriadaNestaSessao() || !resumida || resumida.escalaId) {
      return of(true);
    }
    return this.resumidaApi.delete(resumida.id).pipe(
      tap(() => this.resumidaEscala.set(null)),
      map(() => true),
      catchError(() => of(true)),
    );
  }

  onStepperIndexChange(index: number): void {
    // Emissão originada da nossa própria chamada de sincronização (`syncStepperHeader`), não
    // de um clique real do usuário na barra — ignorar evita o loop descrito ali.
    if (this.syncingStepperHeader) return;
    const target = this.steps()[index];
    if (!target) return;
    if (target === 'resumida') {
      // Aba "Escala Resumida" — sempre navegável (não só na primeira passagem): a escala
      // resumida já salva continua visível/editável enquanto a escala principal não for
      // publicada, então um clique aqui deve mostrar o que já existe, de qualquer passo.
      this.abrirResumidaStep();
      return;
    }
    this.jumpToStep(target);
  }

  private jumpToStep(target: WizardStep): void {
    if (target === 'afastamentos' && this.step() === 'servidores') {
      if (!this.enterStep3()) {
        this.syncStepperHeader();
      }
      return;
    }
    const order = this.steps();
    const targetIdx = order.indexOf(target);
    const currentIdx = order.indexOf(this.step());
    if (targetIdx < currentIdx || (targetIdx === currentIdx + 1 && this.canAdvanceTo(target))) {
      this.setStep(target);
      if (target === 'afastamentos') this.loadStep3Data();
    } else {
      this.syncStepperHeader();
    }
  }

  private canAdvanceTo(target: WizardStep): boolean {
    if (target === 'servidores') return this.step1Form.valid && !this.periodoDuplicado();
    if (target === 'afastamentos') return !!this.escala();
    if (target === 'revisao') return !!this.escala();
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
      this.setStep('servidores');
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
        return;
      }
      this.error.set(null);
      this.setStep('servidores');
      this.loadStep2Data();
    });
  }

  private copiarOrigem(origemId: string): void {
    if (this.periodoDuplicado()) {
      return;
    }
    const v = this.step1Form.getRawValue();
    const abrirResumida = this.resumidaDesejada();
    this.working.set(true);
    this.error.set(null);
    this.api.copiar(origemId, { ano: Number(v.ano), mes: Number(v.mes) }).subscribe({
      next: (escala) => {
        this.dirty.set(false);
        this.working.set(false);
        this.skipDeactivateGuard = true;
        void this.router.navigate(['/escalas', escala.id, 'editar'], {
          replaceUrl: true,
          state: { openStep: 3, abrirResumida },
        });
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  private loadOrigensDisponiveis(): void {
    const setorId = this.step1Form.getRawValue().setorId;
    // Copiar uma escala existente só faz sentido pra uma escala de setor — uma escala de
    // núcleo não tem de onde copiar (é o próprio conceito novo).
    if (!setorId || this.isNucleoOption(setorId)) {
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
    if (!v.setorId || !v.mes || !v.ano || this.isNucleoOption(v.setorId)) return;
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

    this.api.getEscalaAnterior({ setorId: v.setorId }, Number(v.ano), Number(v.mes)).subscribe({
      next: (info) => apply(info?.id),
      error: () => undefined,
    });
  }

  private checkPeriodoDuplicado(): void {
    this.checkPeriodoDuplicado$((exists) => {
      this.periodoDuplicado.set(exists);
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
    const params = this.isNucleoOption(setorId)
      ? { nucleoId: this.extractNucleoId(setorId), ano, mes, page: 1, pageSize: 1 }
      : { setorId, ano, mes, page: 1, pageSize: 1 };
    this.api.list(params).subscribe({
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

  private loadExisting(id: string, openStep: WizardStep = 'afastamentos'): void {
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
          setorId: escala.setorId ?? (escala.nucleoId ? `${EscalaForm.NUCLEO_OPTION_PREFIX}${escala.nucleoId}` : ''),
          mes: String(escala.mes),
          ano: escala.ano,
          origemEscalaId: '',
        });
        this.step1Form.disable();
        this.loadStep2Data(true, () => {
          this.hydrateSelectionFromEscala(escala);
          this.working.set(false);
          this.dirty.set(false);
          if (openStep === 'afastamentos' && this.initialAbrirResumida) {
            // Veio de "copiar" com o checkbox de escala resumida marcado — entra direto no
            // passo "resumida"; `continuarResumida()` avança pro passo de afastamentos
            // normalmente ao terminar.
            this.abrirResumidaStep();
            return;
          }

          if (openStep === 'afastamentos') {
            // "Editar" a partir da listagem (sem state de navegação) — se essa escala tem uma
            // escala resumida associada ao núcleo/período dela, abre direto nela em vez de
            // pular pro passo 3: quem usa escala resumida quer vê-la/ajustá-la ao voltar pra
            // editar, não só a escala final derivada dela.
            const container = this.resumidaContainer();
            if (container) {
              this.abrirResumidaExistenteAoEditar(container, escala.ano, escala.mes, openStep);
              return;
            }
          }

          this.setStep(openStep);
          if (openStep === 'afastamentos') this.loadStep3Data();
        });
      },
      error: () => {
        this.fail('Não foi possível carregar a escala.');
      },
    });
  }

  /** Abre a escala resumida existente do núcleo/período direto (sem passar pelo fluxo de
   * "criar do zero"/"copiar do mês anterior" — essa é só pra escalas NOVAS) — se não houver
   * nenhuma, segue pro passo normal. Marca `resumidaHandled` porque os servidores/regimes já
   * vieram hidratados da própria escala salva; `continuarResumida()` deve só voltar pro passo de
   * servidores, não reaplicar sugestões por cima do que já foi editado. */
  private abrirResumidaExistenteAoEditar(
    container: { nucleoId: string } | { setorId: string },
    ano: number,
    mes: number,
    openStep: WizardStep,
  ): void {
    this.setStep('servidores');
    this.resumidaWorking.set(true);
    this.resumidaApi.list({ ...container, ano, mes, pageSize: 1 }).subscribe({
      next: (result) => {
        const existente = result.items[0];
        if (!existente) {
          this.resumidaWorking.set(false);
          this.setStep(openStep);
          this.loadStep3Data();
          return;
        }
        this.resumidaApi.get(existente.id).subscribe({
          next: (e) => {
            this.resumidaWorking.set(false);
            // Precisa estar marcado pra `resumidaAtiva()` ficar true — senão a aba "Escala
            // Resumida" nem aparece na barra do stepper, mesmo com o passo aberto.
            this.step1Form.controls.usarEscalaResumida.setValue(true);
            this.resumidaEscala.set(e);
            this.resumidaHandled.set(true);
            this.setStep('resumida');
          },
          error: () => {
            this.resumidaWorking.set(false);
            this.setStep(openStep);
            this.loadStep3Data();
          },
        });
      },
      error: () => {
        this.resumidaWorking.set(false);
        this.setStep(openStep);
        this.loadStep3Data();
      },
    });
  }

  // ---------- Step 2 ----------

  private loadStep2Data(preserveSelection = false, afterLoad?: () => void): void {
    const setorId = this.step1Form.getRawValue().setorId;
    const ehNucleo = this.escalaDeNucleo();
    forkJoin({
      padroes: this.api.listPadroes(),
      servidores: this.adminApi.listMeusServidores(false),
    }).subscribe({
      next: ({ padroes, servidores }) => {
        this.padroes.set(padroes);

        let pool: ServidorListItem[];
        let padrao: ServidorListItem[];
        if (ehNucleo) {
          // Escala de núcleo: o pool é todo mundo do núcleo (qualquer setor que ele engloba +
          // lotados diretos) — todos entram marcados por padrão, já que é o próprio conceito.
          const nucleoId = this.nucleoIdDoSetor();
          const setoresDoNucleoIds = new Set(
            this.setoresRaw().filter((s) => s.nucleoId === nucleoId).map((s) => s.id),
          );
          pool = servidores.filter(
            (s) => s.nucleoId === nucleoId || (!!s.setorId && setoresDoNucleoIds.has(s.setorId)),
          );
          padrao = pool;
        } else {
          // Servidor lotado direto no núcleo presta serviço a todos os setores do núcleo —
          // aparece como opção aqui também, mas não pré-selecionado (só quem é do próprio
          // setor entra marcado por padrão).
          const nucleoId = this.setoresRaw().find((s) => s.id === setorId)?.nucleoId ?? null;
          pool = servidores.filter(
            (s) => s.setorId === setorId || (!!nucleoId && s.nucleoId === nucleoId),
          );
          padrao = pool.filter((s) => s.setorId === setorId);
        }

        this.servidoresSetor.set(pool);
        if (!preserveSelection) {
          this.selectedServidorIds.set(new Set(padrao.map((s) => s.id)));
        } else {
          const existing = new Set((this.escala()?.servidores ?? []).map((s) => s.servidorId));
          this.selectedServidorIds.set(existing.size ? existing : new Set(padrao.map((s) => s.id)));
        }
        this.ensureRegimeControls();
        this.ensureInicioCicloControls();
        afterLoad?.();
      },
      error: () => this.error.set('Não foi possível carregar servidores e padrões do setor.'),
    });
  }

  private hydrateSelectionFromEscala(escala: EscalaDetail): void {
    const padroesById = new Map(this.padroes().map((p) => [p.id, p]));
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
        regs.add(codigo);
        if (j.dataInicioCiclo) {
          inicio.set(s.servidorId, j.dataInicioCiclo.slice(0, 10));
        }
      }
      if (regs.size) porServidor.set(s.servidorId, regs);
    }

    this.servidorRegimes.set(porServidor);
    this.servidorInicioCiclo.set(inicio);
    this.selectedServidorIds.set(new Set(escala.servidores.map((s) => s.servidorId)));
    this.ensureRegimeControls();
    // `ensureRegimeControls` não reatribui controles já existentes (podem ter sido criados
    // vazios por `loadStep2Data`, antes desta escala ser carregada) — força o valor correto.
    for (const [servidorId, regs] of porServidor) {
      this.regimeForm.get(servidorId)?.setValue(regs.size ? [...regs][0] : '', { emitEvent: false });
    }
    this.ensureInicioCicloControls();
    this.lastAppliedRegimesFingerprint = this.regimesFingerprint();
  }

  private isRegimeCodigo(value: string): value is RegimeCodigo {
    return value === 'EXP_ADM' || value === '12X36' || value === '24X72' || value === 'PT24_TL12';
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
    if (recorrencia === 'CicloPersonalizado') {
      return 'PT24_TL12';
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

  isServidorSelected(id: string): boolean {
    return this.selectedServidorIds().has(id);
  }

  toggleServidor(id: string): void {
    const set = new Set(this.selectedServidorIds());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.selectedServidorIds.set(set);
    this.ensureRegimeControls();
    this.ensureInicioCicloControls();
    this.markDirty();
  }

  selecionarTodos(): void {
    const set = new Set(this.selectedServidorIds());
    for (const s of this.servidoresSetor()) set.add(s.id);
    this.selectedServidorIds.set(set);
    this.ensureRegimeControls();
    this.ensureInicioCicloControls();
    this.markDirty();
  }

  removerSelecionados(): void {
    this.selectedServidorIds.set(new Set());
    this.markDirty();
  }

  isServidorCardExpanded(id: string): boolean {
    return this.expandedServidorCards().has(id);
  }

  toggleServidorCardExpanded(id: string): void {
    const set = new Set(this.expandedServidorCards());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.expandedServidorCards.set(set);
  }

  continuar2(): void {
    const semRegime = this.servidoresSemRegime();
    if (semRegime.length && !this.isEditMode()) {
      this.error.set(`Selecione o tipo de regime de: ${semRegime.join(', ')}.`);
      return;
    }
    if (!this.selectedServidorIds().size) {
      this.error.set('Selecione ao menos um servidor.');
      return;
    }

    const v = this.step1Form.getRawValue();
    const avancar = () => {
      if (this.resumidaAtiva()) {
        this.abrirResumidaStep();
        return;
      }
      this.enterStep3();
    };

    this.working.set(true);
    this.api
      .checkConflitosServidores({
        ano: Number(v.ano),
        mes: Number(v.mes),
        servidorIds: Array.from(this.selectedServidorIds()),
        excluirEscalaId: this.escala()?.id || undefined,
      })
      .subscribe({
        next: (conflitos) => {
          this.working.set(false);
          if (conflitos.length > 0) {
            const detalhe = conflitos.map((c) => `${c.servidorNome} (${c.origem})`).join('; ');
            this.error.set(`Já escalado(s) em outra escala neste mês: ${detalhe}.`);
            return;
          }
          this.error.set(null);
          avancar();
        },
        // Checagem indisponível não deve travar o fluxo — o backend segue autoritativo
        // (AddServidoresAsync/GerarEscalaAsync) na hora de salvar, se for o caso.
        error: () => {
          this.working.set(false);
          avancar();
        },
      });
  }

  // ---------- Passo opcional "Escala Resumida" (entre o passo 2 e o passo 3) ----------

  private abrirResumidaStep(): void {
    this.setStep('resumida');
    const container = this.resumidaContainer();
    if (!container) return;

    const v = this.step1Form.getRawValue();
    this.resolverResumida(container, Number(v.ano), Number(v.mes));
  }

  private resolverResumida(
    container: { nucleoId: string } | { setorId: string },
    ano: number,
    mes: number,
  ): void {
    this.resumidaWorking.set(true);
    this.resumidaApi.list({ ...container, ano, mes, pageSize: 1 }).subscribe({
      next: (result) => {
        this.resumidaWorking.set(false);
        const existente = result.items[0];
        if (existente) {
          this.resumidaApi.get(existente.id).subscribe({ next: (e) => this.resumidaEscala.set(e) });
          return;
        }
        this.resumidaEscala.set(null);
        this.resumidaApi.getAnterior(container, ano, mes).subscribe({
          next: (info) => {
            if (!info?.id) {
              // Sem escala resumida anterior pra copiar: já se sabe (pelo checkbox do passo 1)
              // que o usuário quer escala resumida — cria do zero direto, sem perguntar de novo.
              this.resumidaAnteriorId.set(null);
              this.criarResumidaDoZero();
              return;
            }
            this.resumidaAnteriorId.set(info.id);
            this.resumidaAnteriorLabel.set(info.identificacao ?? null);
          },
          error: () => {
            this.resumidaAnteriorId.set(null);
            this.criarResumidaDoZero();
          },
        });
      },
      error: () => this.resumidaWorking.set(false),
    });
  }

  criarResumidaDoZero(): void {
    const container = this.resumidaContainer();
    if (!container) return;
    const v = this.step1Form.getRawValue();

    this.resumidaWorking.set(true);
    this.resumidaApi.create({ ...container, ano: Number(v.ano), mes: Number(v.mes) }).subscribe({
      next: (escala) => {
        this.resumidaEscala.set(escala);
        this.resumidaCriadaNestaSessao.set(true);
        this.resumidaWorking.set(false);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível criar a escala resumida.');
        this.resumidaWorking.set(false);
      },
    });
  }

  copiarResumidaAnterior(): void {
    const origemId = this.resumidaAnteriorId();
    if (!origemId) return;
    const v = this.step1Form.getRawValue();

    this.resumidaWorking.set(true);
    this.resumidaApi.copiar(origemId, { ano: Number(v.ano), mes: Number(v.mes) }).subscribe({
      next: (escala) => {
        this.resumidaEscala.set(escala);
        this.resumidaCriadaNestaSessao.set(true);
        this.resumidaWorking.set(false);
        this.feedback.showSuccess(
          'Escala resumida copiada — os últimos 4 dias do mês anterior continuam o rodízio automaticamente.',
        );
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível copiar a escala resumida do mês anterior.');
        this.resumidaWorking.set(false);
      },
    });
  }

  voltarResumida(): void {
    this.voltarStep('servidores');
  }

  /** Qualquer edição real na escala resumida (setores, equipes, rodízio, célula) invalida as
   * sugestões já aplicadas — sem isso, mudar o rodízio depois de já ter avançado uma vez pro
   * passo de servidores não refletia na escala definitiva, porque `continuarResumida()` só
   * reaplica sugestões enquanto `resumidaHandled` estiver falso. */
  onResumidaChange(escala: EscalaResumidaDetail): void {
    this.resumidaEscala.set(escala);
    this.resumidaHandled.set(false);
  }

  /** Só aplica sugestões na primeira passagem (ou depois de uma edição real, ver
   * `onResumidaChange`) — revisitar este passo sem mudar nada (pra só olhar a escala resumida já
   * salva, ver `onStepperIndexChange`) não deve reaplicar nada por cima de ajustes manuais já
   * feitos no passo de servidores. De qualquer forma, sempre avança pro passo de afastamentos —
   * resumida nunca é um beco sem saída. */
  continuarResumida(): void {
    if (!this.resumidaHandled()) {
      this.resumidaHandled.set(true);
      this.aplicarSugestoesDeResumida();
    }
    this.enterStep3();
  }

  /** Deriva sugestões de servidor+regime pro passo seguinte a partir do rodízio configurado
   * na escala resumida (equipes do setor sendo criado) — o usuário só revisa/ajusta em vez de
   * montar tudo do zero. Só considera equipes do PRÓPRIO setor desta escala (uma escala
   * resumida de núcleo pode incluir outros setores, mas cada um recebe sua sugestão quando a
   * própria escala daquele setor for criada/editada). */
  private aplicarSugestoesDeResumida(): void {
    const resumida = this.resumidaEscala();
    const setorId = this.step1Form.getRawValue().setorId;
    if (!resumida || !setorId) return;

    // Escala de setor: só as equipes daquele setor. Escala de núcleo: todas as equipes de
    // todos os setores da resumida, já que a escala cobre o núcleo inteiro.
    const setores = this.escalaDeNucleo()
      ? resumida.setores
      : resumida.setores.filter((s) => s.setorId === setorId);
    if (setores.length === 0) return;

    const inicioEscala = normalizeDay(resumida.dataInicio);
    const poolServidores = new Set(this.servidoresSetor().map((s) => s.id));
    const ciclos = new Map(this.servidorCicloResumida());
    const ids = new Set(this.selectedServidorIds());
    let novos = 0;

    for (const setor of setores) {
      for (const equipe of setor.equipes) {
        const ancoraEquipe = equipe.dataInicioCiclo;
        const tamanhoPool = equipe.rotacao.length;
        if (!ancoraEquipe || tamanhoPool === 0) continue;

        for (const membro of equipe.rotacao) {
          if (!membro.servidorId || !poolServidores.has(membro.servidorId)) continue;
          const ancoraServidor = primeiraDataParaPosicao(inicioEscala, ancoraEquipe, tamanhoPool, membro.posicao);
          if (!ancoraServidor) continue;

          ciclos.set(membro.servidorId, { ancora: ancoraServidor, tamanhoPool });
          if (!ids.has(membro.servidorId)) {
            ids.add(membro.servidorId);
            novos++;
          }
        }
      }
    }

    if (ciclos.size === 0) return;

    this.servidorCicloResumida.set(ciclos);
    this.selectedServidorIds.set(ids);
    this.ensureRegimeControls();
    this.ensureInicioCicloControls();
    this.markDirty();
    if (novos > 0) {
      this.toast.showSuccess(
        `${novos} servidor(es) da escala resumida pré-preenchido(s) nesta escala.`,
      );
    }
  }

  /** Valida passo 2, regenera draft se necessário e avança para o passo 3. */
  private enterStep3(): boolean {
    const semRegime = this.servidoresSemRegime();
    if (semRegime.length && !this.isEditMode()) {
      this.error.set(`Selecione o tipo de regime de: ${semRegime.join(', ')}.`);
      return false;
    }
    if (!this.selectedServidorIds().size) {
      this.error.set('Selecione ao menos um servidor.');
      return false;
    }
    // Com escala resumida ativa, o início do ciclo de cada servidor vem da âncora configurada
    // no rodízio dela (passo seguinte) — perguntar de novo aqui é redundante e, pior, se o
    // usuário preenchesse mesmo assim, geraria uma jornada duplicada/conflitante com a que
    // `buildJornadasResumida$` monta a partir do rodízio (ver `buildGerarItens`, que já exclui
    // esses servidores). Só pede início do ciclo aqui pra quem NÃO vai usar escala resumida.
    if (this.needsInicioCiclo() && !this.resumidaAtiva()) {
      this.ensureInicioCicloControls();
      this.syncInicioCicloFromControls();
      const semInicio = Array.from(this.selectedServidorIds()).some(
        (id) => this.isServidorRegimeCiclico(id) && !this.servidorInicioCiclo().get(id),
      );
      if (semInicio) {
        this.error.set('Informe o dia de início do ciclo para cada servidor com regime cíclico.');
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
    this.setStep('afastamentos');
    this.loadStep3Data();
    return true;
  }

  /** UI de tarde (T) no passo 2: só pra servidor com regime EXP_ADM. */
  showExpedienteTardeForServidor(servidorId: string): boolean {
    return this.servidorRegimeCodigo(servidorId) === 'EXP_ADM';
  }

  private deriveTipoFuncionamento(): TipoFuncionamento {
    return this.regimesSelected().some((r) => r === '12X36' || r === '24X72' || r === 'PT24_TL12')
      ? 'VinteQuatroHoras'
      : 'Expediente';
  }

  private regimesFingerprint(): string {
    const porServidor = Array.from(this.selectedServidorIds())
      .sort()
      .map((id) => `${id}:${this.servidorRegimeCodigo(id) ?? ''}`)
      .join('|');
    const inicios = Array.from(this.selectedServidorIds())
      .sort()
      .map((id) => `${id}:${this.servidorInicioCiclo().get(id) ?? ''}`)
      .join('|');
    return `${porServidor}#${inicios}`;
  }

  private buildLocalDraft(): void {
    const v = this.step1Form.getRawValue();
    const ano = Number(v.ano);
    const mes = Number(v.mes);
    const dataInicio = firstDayOfMonth(ano, mes);
    const dataFim = lastDayOfMonth(ano, mes);
    const days = daysInRange(dataInicio, dataFim);
    const tipoFuncionamento = this.deriveTipoFuncionamento();

    const ehNucleo = this.escalaDeNucleo();
    const nucleoUnidade = ehNucleo
      ? this.nucleosDoUsuario().find((n) => n.id === this.extractNucleoId(v.setorId))
      : null;
    const setorOpt = ehNucleo ? null : this.setoresRaw().find((s) => s.id === v.setorId);
    const unidadeLabel = ehNucleo
      ? `${nucleoUnidade?.sigla ?? ''} — ${nucleoUnidade?.nome ?? ''}`
      : `${setorOpt?.sigla ?? ''} — ${setorOpt?.nome ?? ''}`;

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
      identificacao: `Escala ${unidadeLabel} ${String(mes).padStart(2, '0')}/${ano}`,
      setorId: ehNucleo ? null : (setorOpt?.id ?? null),
      setorNome: ehNucleo ? null : (setorOpt?.nome ?? ''),
      setorSigla: ehNucleo ? null : (setorOpt?.sigla ?? ''),
      nucleoId: ehNucleo ? (nucleoUnidade?.id ?? null) : null,
      nucleoNome: ehNucleo ? (nucleoUnidade?.nome ?? null) : null,
      nucleoSigla: ehNucleo ? (nucleoUnidade?.sigla ?? null) : null,
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
    const ciclo = this.servidorCicloResumida().get(servidorId);
    if (ciclo) {
      return buildOcorrenciasFromCicloDerivado({
        days,
        ancora: ciclo.ancora,
        tamanhoPool: ciclo.tamanhoPool,
      });
    }
    const codigo = this.servidorRegimeCodigo(servidorId);
    return buildOcorrenciasForServidor({
      servidorId,
      days,
      regimesSelected: codigo ? [codigo] : [],
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
      // Escala de núcleo: sem setorId (a listagem por setor não cobriria os demais setores
      // do núcleo) — servidorIds sozinho já escopa corretamente a consulta.
      .list({ ano: e.ano, mes: e.mes, setorId: e.setorId ?? undefined, servidorIds: ids })
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

    if (!this.auth.canAccessEscala('afastamentos.criar', e.setorId, e.nucleoId)) {
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
        // Sem aviso de sucesso duplicado aqui — o próprio AfastamentoDialog já mostrou o dele
        // antes de fechar.
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
    this.setStep('revisao');
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
    if (!this.escala()) {
      this.buildLocalDraft();
    }
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
            setorId: draft.setorId ?? undefined,
            nucleoId: draft.nucleoId ?? undefined,
            ano,
            mes,
            tipoFuncionamento: draft.tipoFuncionamento,
          })
          .pipe(switchMap((escala) => this.api.addServidores(escala.id, servidorIds)));

    // Draft já contém a matriz completa; sync em lote evita gerar+upsert (concorrência EF).
    return afterReady$.pipe(
      switchMap((escala) => {
        this.escala.set({ ...draft, id: escala.id, identificacao: escala.identificacao });
        const itens = this.buildGerarItens(draft);
        // Gera a EscalaJornada/PadraoEscalaId estruturada de cada servidor (regime por
        // servidor) antes de sincronizar a grade — a grade sincronizada em seguida (com as
        // eventuais edições manuais do passo 3) prevalece célula a célula sobre o que foi
        // gerado, então chamar gerar() primeiro nunca perde o que o usuário editou.
        const gerar$ = itens.length ? this.api.gerar(escala.id, { itens, distribuirAutomaticamente: false }) : of(escala);
        return gerar$.pipe(
          switchMap(() => this.vincularResumidaSeAplicavel$(escala.id)),
          switchMap(() => this.buildJornadasResumida$(escala.id, draft)),
          switchMap(() => this.syncCells$(escala.id, cells)),
        );
      }),
      tap((escala) => {
        this.escala.set(escala);
        this.working.set(false);
      }),
    );
  }

  /** Atrela a escala resumida (se alguma foi usada nesta sessão) à escala real assim que ela
   * tem um id persistido — a resumida continua compartilhada por núcleo+período entre
   * setores, então o vínculo só grava a primeira vez (ver `EscalaResumida.VincularEscala`). */
  private vincularResumidaSeAplicavel$(escalaId: string): Observable<unknown> {
    const resumidaId = this.resumidaEscala()?.id;
    if (!resumidaId) return of(null);
    return this.resumidaApi.vincularEscala(resumidaId, escalaId);
  }

  /** Persiste a `EscalaJornada` de cada servidor cujo regime veio do rodízio da escala
   * resumida (não um dos 4 `PadraoEscala`/`RegimeCodigo` fixos, então fora de `buildGerarItens`). */
  private buildJornadasResumida$(escalaId: string, draft: EscalaDetail): Observable<unknown> {
    const ciclos = this.servidorCicloResumida();
    const servidorIds = draft.servidores.map((s) => s.servidorId).filter((id) => ciclos.has(id));
    if (!servidorIds.length) return of(null);

    let chain: Observable<unknown> = of(null);
    for (const servidorId of servidorIds) {
      const ciclo = ciclos.get(servidorId)!;
      chain = chain.pipe(
        switchMap(() =>
          this.api.addJornada(escalaId, servidorId, {
            tipoJornada: 'Plantao',
            dataInicio: draft.dataInicio,
            dataFim: draft.dataFim,
            tipoOcorrenciaCodigo: 'PT',
            recorrenciaTipo: 'CicloPlantao',
            diasTrabalho: 1,
            diasFolga: ciclo.tamanhoPool - 1,
            tipoOcorrenciaFolgaCodigo: 'D',
            dataInicioCiclo: ciclo.ancora,
            horas: 24,
          }),
        ),
      );
    }
    return chain;
  }

  /** Monta um item de geração (regime + início de ciclo) por servidor do draft, a partir do
   * regime escolhido por cada um no passo 2. Servidor sem regime resolvido é ignorado (não
   * deveria acontecer — passo 2 já valida que todo servidor selecionado tem um regime).
   *
   * Servidor cujo ciclo vem do rodízio da escala resumida (`servidorCicloResumida`) também é
   * ignorado aqui, mesmo que tenha um regime escolhido no passo 2 — `buildJornadasResumida$`
   * já monta a jornada dele a partir da âncora real do rodízio; gerar aqui TAMBÉM criaria uma
   * segunda jornada conflitante por cima daquela. */
  private buildGerarItens(draft: EscalaDetail): GerarEscalaItemPayload[] {
    const fallbackInicio = normalizeDay(draft.dataInicio);
    const ciclosResumida = this.servidorCicloResumida();
    const itens: GerarEscalaItemPayload[] = [];
    for (const s of draft.servidores) {
      if (ciclosResumida.has(s.servidorId)) continue;
      const codigo = this.servidorRegimeCodigo(s.servidorId);
      const padrao = codigo ? this.padroesByCodigo().get(codigo) : undefined;
      if (!padrao) continue;
      itens.push({
        servidorId: s.servidorId,
        padraoEscalaId: padrao.id,
        dataInicioCiclo: this.servidorInicioCiclo().get(s.servidorId) ?? fallbackInicio,
      });
    }
    return itens;
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
    this.setStep(n);
  }

  /** "Voltar" do passo de afastamentos — quando a escala usa resumida, ela é a etapa anterior de
   * fato (o passo de servidores já foi concluído e suas sugestões já foram aplicadas), então
   * volta pra lá em vez de pular pra "Regimes e servidores". */
  voltarDeStep3(): void {
    if (this.resumidaAtiva()) {
      this.abrirResumidaStep();
      return;
    }
    this.voltarStep('servidores');
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
