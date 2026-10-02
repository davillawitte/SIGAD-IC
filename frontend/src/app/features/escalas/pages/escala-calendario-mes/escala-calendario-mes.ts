import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciBreadcrumbService,
  PciButtonComponent,
  PciCalendarComponent,
  PciCheckboxComponent,
  PciLayoutBreadcrumbService,
  PciPageHeaderComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type {
  PciCalendarEvent,
  PciCalendarEventVariant,
  PciSelectOption,
} from '@davillawitte/pci-design-system';
import { Subscription } from 'rxjs';

import { ESCALAS_ROUTE_PAGES } from '../../escalas-route-pages';
import { EscalasApiService } from '../../services/escalas-api.service';
import { statusEscalaLabel } from '../../models/escalas.models';
import type {
  EscalaCalendarioMesEscala,
  EscalaCalendarioMesItem,
} from '../../models/escalas.models';

type EscalaEscopo = 'setor' | 'institucional';
type GrupoOcorrencia = 'expediente' | 'plantao24' | 'plantao12' | 'afastamentos';

/** Prefixo que distingue núcleo de setor no valor do filtro de lotação (uma escala pertence a
 * um ou ao outro) — mesmo artifício do filtro da listagem de escalas. */
const NUCLEO_PREFIX = 'nucleo:';

/** Códigos por grupo do filtro "Mostrar", na mesma divisão da legenda da matriz da escala
 * (`escala-matrix`). Descanso (D) e feriado (F) ficam de fora: o calendário mostra quem
 * trabalha e quem está afastado, não quem está de folga. */
const CODIGOS_POR_GRUPO: Record<GrupoOcorrencia, readonly string[]> = {
  expediente: ['M', 'T', 'TL6', 'TL12'],
  plantao24: ['PT'],
  plantao12: ['PD', 'PN', 'CF'],
  afastamentos: ['FR', 'LP', 'LM', 'LO', 'R'],
};

const GRUPO_POR_CODIGO = new Map<string, GrupoOcorrencia>(
  (Object.keys(CODIGOS_POR_GRUPO) as GrupoOcorrencia[]).flatMap((grupo) =>
    CODIGOS_POR_GRUPO[grupo].map((codigo) => [codigo, grupo] as [string, GrupoOcorrencia]),
  ),
);

const VARIANTE_POR_GRUPO: Record<GrupoOcorrencia, PciCalendarEventVariant> = {
  expediente: 'info',
  plantao24: 'accent',
  plantao12: 'primary',
  afastamentos: 'warning',
};

const LEGENDA: { grupo: GrupoOcorrencia; label: string; codigos: string }[] = [
  { grupo: 'expediente', label: 'Expediente', codigos: CODIGOS_POR_GRUPO.expediente.join(', ') },
  { grupo: 'plantao24', label: 'Plantonistas 24h', codigos: CODIGOS_POR_GRUPO.plantao24.join(', ') },
  { grupo: 'plantao12', label: 'Plantonistas 12h', codigos: CODIGOS_POR_GRUPO.plantao12.join(', ') },
  {
    grupo: 'afastamentos',
    label: 'Afastamentos',
    codigos: CODIGOS_POR_GRUPO.afastamentos.join(', '),
  },
];

/** Primeiro nome + último sobrenome — o rótulo do dia no calendário é curto por necessidade. */
function abreviarNome(nome: string): string {
  const partes = nome.trim().split(/\s+/).filter(Boolean);
  if (partes.length <= 1) {
    return nome.trim();
  }
  return partes[0] + ' ' + partes[partes.length - 1];
}

/** Visão mês a mês das escalas: quem trabalha (e quem está afastado) em cada dia, com filtros
 * de lotação, servidor e tipo de marcação. A mesma tela serve Gestão do Setor e Gestão
 * Institucional — o escopo vem da rota e decide o alcance no backend. */
@Component({
  selector: 'app-escala-calendario-mes',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciCalendarComponent,
    PciCheckboxComponent,
    PciPageHeaderComponent,
    PciSelectComponent,
  ],
  templateUrl: './escala-calendario-mes.html',
  styleUrl: './escala-calendario-mes.scss',
})
export class EscalaCalendarioMes implements OnInit, OnDestroy {
  private readonly api = inject(EscalasApiService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly breadcrumb = inject(PciBreadcrumbService);
  private readonly layoutBreadcrumb = inject(PciLayoutBreadcrumbService);
  private readonly subs = new Subscription();

  readonly legenda = LEGENDA;
  readonly escopo = signal<EscalaEscopo>('setor');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly viewDate = signal(new Date());
  readonly escalas = signal<EscalaCalendarioMesEscala[]>([]);
  readonly itens = signal<EscalaCalendarioMesItem[]>([]);

  readonly form = this.fb.nonNullable.group({
    lotacao: [''],
    servidorId: [''],
    expediente: [true],
    plantao24: [true],
    plantao12: [true],
    afastamentos: [true],
  });

  /** Espelho dos filtros em signal, pra os `computed` reagirem ao formulário. */
  readonly filtros = signal(this.form.getRawValue());

  readonly isInstitucional = computed(() => this.escopo() === 'institucional');

  readonly listBasePath = computed(() =>
    this.isInstitucional() ? '/escalas-institucionais' : '/escalas',
  );

  readonly pageTitle = computed(() =>
    this.isInstitucional() ? 'Calendário institucional do mês' : 'Calendário do mês',
  );

  /** Só as lotações que têm escala no mês carregado — o backend já devolveu apenas as
   * visíveis no escopo, então o filtro nunca oferece setor que a pessoa não possa ver. */
  readonly lotacaoOptions = computed<PciSelectOption[]>(() => {
    const opcoes = this.escalas().map((escala) => ({
      label: this.lotacaoLabel(escala),
      value: escala.nucleoId ? NUCLEO_PREFIX + escala.nucleoId : (escala.setorId ?? ''),
    }));
    return [
      { label: 'Todos', value: '' },
      ...opcoes
        .filter((o) => o.value !== '')
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR')),
    ];
  });

  private readonly itensPorLotacao = computed(() => {
    const lotacao = this.filtros().lotacao;
    if (!lotacao) {
      return this.itens();
    }
    if (lotacao.startsWith(NUCLEO_PREFIX)) {
      const nucleoId = lotacao.slice(NUCLEO_PREFIX.length);
      return this.itens().filter((item) => item.nucleoId === nucleoId);
    }
    return this.itens().filter((item) => item.setorId === lotacao);
  });

  readonly servidorOptions = computed<PciSelectOption[]>(() => {
    const nomePorId = new Map<string, string>();
    this.itensPorLotacao().forEach((item) => nomePorId.set(item.servidorId, item.servidorNome));
    return [
      { label: 'Todos', value: '' },
      ...[...nomePorId.entries()]
        .map(([value, label]) => ({ label, value }))
        .sort((a, b) => a.label.localeCompare(b.label, 'pt-BR')),
    ];
  });

  readonly itensFiltrados = computed(() => {
    const f = this.filtros();
    return this.itensPorLotacao().filter((item) => {
      if (f.servidorId && item.servidorId !== f.servidorId) {
        return false;
      }
      const grupo = GRUPO_POR_CODIGO.get(item.tipoOcorrenciaCodigo);
      return grupo ? f[grupo] : false;
    });
  });

  readonly eventos = computed<PciCalendarEvent[]>(() =>
    this.itensFiltrados().map((item) => {
      const grupo = GRUPO_POR_CODIGO.get(item.tipoOcorrenciaCodigo);
      const horas = item.horas ? ' (' + item.horas + 'h)' : '';
      const sigla = item.setorSigla ?? item.nucleoSigla ?? '';
      return {
        date: item.data.slice(0, 10),
        label: item.tipoOcorrenciaCodigo + ' · ' + abreviarNome(item.servidorNome),
        description: item.servidorNome + ' — ' + item.tipoOcorrenciaNome + horas,
        variant: grupo ? VARIANTE_POR_GRUPO[grupo] : 'info',
        meta: this.isInstitucional() ? sigla : undefined,
      };
    }),
  );

  /** Quantas marcações cada grupo tem no mês — ajuda a entender um calendário vazio. */
  readonly totaisPorGrupo = computed(() => {
    const totais: Record<GrupoOcorrencia, number> = {
      expediente: 0,
      plantao24: 0,
      plantao12: 0,
      afastamentos: 0,
    };
    this.itensPorLotacao().forEach((item) => {
      const grupo = GRUPO_POR_CODIGO.get(item.tipoOcorrenciaCodigo);
      if (grupo) {
        totais[grupo] += 1;
      }
    });
    return totais;
  });

  readonly escalasNaoPublicadas = computed(() =>
    this.escalas().filter((e) => e.status !== 'Publicada'),
  );

  readonly totalServidores = computed(
    () => new Set(this.itensFiltrados().map((i) => i.servidorId)).size,
  );

  ngOnInit(): void {
    const modo = (this.route.snapshot.data['escopo'] as EscalaEscopo | undefined) ?? 'setor';
    this.escopo.set(modo === 'institucional' ? 'institucional' : 'setor');
    this.layoutBreadcrumb.setItems(
      this.breadcrumb.buildFromRoutes(ESCALAS_ROUTE_PAGES, this.listBasePath() + '/calendario'),
    );

    this.subs.add(
      this.form.valueChanges.subscribe(() => this.filtros.set(this.form.getRawValue())),
    );
    // Trocar a lotação pode deixar o servidor escolhido fora da lista — limpa a escolha pra
    // não filtrar por alguém que não aparece mais.
    this.subs.add(
      this.form.controls.lotacao.valueChanges.subscribe(() =>
        this.form.controls.servidorId.setValue('', { emitEvent: false }),
      ),
    );

    this.reload();
  }

  ngOnDestroy(): void {
    this.layoutBreadcrumb.clear();
    this.subs.unsubscribe();
  }

  /** O próprio `pci-calendar` navega entre meses; cada troca recarrega o mês no servidor. */
  onViewDateChange(data: Date): void {
    this.viewDate.set(data);
    this.reload();
  }

  voltar(): void {
    void this.router.navigateByUrl(this.listBasePath());
  }

  statusLabel(escala: EscalaCalendarioMesEscala): string {
    return statusEscalaLabel(escala.status);
  }

  lotacaoLabel(escala: EscalaCalendarioMesEscala): string {
    return escala.nucleoId
      ? (escala.nucleoSigla ?? escala.nucleoNome ?? 'Núcleo') + ' (núcleo)'
      : (escala.setorSigla ?? escala.setorNome ?? 'Setor');
  }

  private reload(): void {
    const data = this.viewDate();
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getCalendarioMes({
        ano: data.getFullYear(),
        mes: data.getMonth() + 1,
        escopo: this.escopo(),
      })
      .subscribe({
        next: (resultado) => {
          this.escalas.set(resultado.escalas ?? []);
          this.itens.set(resultado.itens ?? []);
          this.loading.set(false);
        },
        error: (err: { error?: { message?: string } }) => {
          this.escalas.set([]);
          this.itens.set([]);
          this.error.set(err.error?.message ?? 'Não foi possível carregar o calendário do mês.');
          this.loading.set(false);
        },
      });
  }
}
