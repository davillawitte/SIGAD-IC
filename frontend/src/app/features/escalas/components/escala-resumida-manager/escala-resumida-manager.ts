import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { from } from 'rxjs';
import { concatMap } from 'rxjs/operators';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCardComponent,
  PciCardContentComponent,
  PciCheckboxComponent,
  PciDatepickerComponent,
  PciFeedbackModalService,
  PciIconButtonComponent,
  PciIconComponent,
  PciSelectComponent,
  PciStackComponent,
  PciTabComponent,
  PciTabsComponent,
  PciToastService,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AdminApiService } from '../../../admin/services/admin-api.service';
import type { SetorListItem } from '../../../admin/models/admin.models';
import { AppFormColDirective } from '../../../../shared/form-layout';
import { EscalaResumidaGrid } from '../escala-resumida-grid/escala-resumida-grid';
import { EscalasResumidasApiService } from '../../services/escalas-resumidas-api.service';
import type {
  EscalaResumidaDetail,
  EscalaResumidaEquipe,
  EscalaResumidaServidorElegivel,
  EscalaResumidaSetor,
} from '../../models/escalas-resumidas.models';

const DO_VALUE = '__DO__';

/** Uma posição do pool de rodízio: `segunda` é um reforço opcional (vazio = sem reforço) —
 * oferecido só pra Agentes. Ao contrário da posição principal, não tem opção "DO": reforço
 * marcado como folga equivale a não ter reforço nenhum, então não faz sentido oferecer. */
interface PosicaoDraft {
  principal: FormControl<string>;
  segunda: FormControl<string>;
}

interface RotacaoDraft {
  equipeId: string;
  dataInicioCiclo: FormControl<string>;
  membros: PosicaoDraft[];
}

function errMsg(err: { error?: { message?: string } }, fallback: string): string {
  return err.error?.message ?? fallback;
}

/**
 * Gerencia setores/equipes/rodízio + grade de uma escala resumida já resolvida (criada ou
 * carregada pelo chamador). Embutido como step opcional do wizard de escala por setor
 * (`escala-form`) — quem chefia um núcleo configura a escala resumida ali mesmo, sem trocar
 * de tela; não existe mais nenhuma página avulsa de escala resumida.
 */
@Component({
  selector: 'app-escala-resumida-manager',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCardComponent,
    PciCardContentComponent,
    PciCheckboxComponent,
    PciDatepickerComponent,
    PciIconButtonComponent,
    PciIconComponent,
    PciSelectComponent,
    PciStackComponent,
    PciTabComponent,
    PciTabsComponent,
    AppFormColDirective,
    EscalaResumidaGrid,
  ],
  templateUrl: './escala-resumida-manager.html',
  styleUrl: './escala-resumida-manager.scss',
})
export class EscalaResumidaManager implements OnInit {
  private readonly api = inject(EscalasResumidasApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly toast = inject(PciToastService);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly escalaInicial = input.required<EscalaResumidaDetail>({ alias: 'escala' });
  /** Só chefes do núcleo (não só do setor sendo criado) podem incluir outros setores do
   * núcleo nesta mesma escala resumida — um chefe de setor simples já tem seu único setor
   * implícito, sem precisar desta seção. */
  readonly podeGerenciarSetores = input(false);
  readonly escalaChange = output<EscalaResumidaDetail>();

  readonly working = signal(false);
  readonly error = signal<string | null>(null);
  readonly escala = signal<EscalaResumidaDetail | null>(null);
  readonly setoresDoNucleo = signal<SetorListItem[]>([]);
  readonly elegiveis = signal<EscalaResumidaServidorElegivel[]>([]);
  readonly rotacaoAberta = signal<Record<string, RotacaoDraft>>({});

  /** Índice da aba de setor ativa em "Equipes e rodízios" — uma aba por setor participante,
   * pra não empilhar as equipes de todos os setores na tela ao mesmo tempo. */
  readonly setorTabIndex = signal(0);
  readonly clampedSetorTabIndex = computed(() => {
    const total = this.escala()?.setores.length ?? 0;
    return total === 0 ? 0 : Math.min(this.setorTabIndex(), total - 1);
  });
  readonly activeSetor = computed<EscalaResumidaSetor | null>(
    () => this.escala()?.setores[this.clampedSetorTabIndex()] ?? null,
  );

  onSetorTabChange(index: number): void {
    this.setorTabIndex.set(index);
  }

  /** Equipes do setor da aba ativa com o rodízio aberto pra edição — salvar é um botão só,
   * fora dos cards das equipes, que grava todas de uma vez (mais simples do que salvar uma
   * equipe por vez e cada nova salva ter que recalcular o rodízio considerando as anteriores). */
  readonly idsRotacaoAbertaDoSetorAtivo = computed(() => {
    const setor = this.activeSetor();
    if (!setor) return [];
    const abertos = this.rotacaoAberta();
    return setor.equipes.map((e) => e.id).filter((id) => !!abertos[id]);
  });

  readonly temRotacaoAbertaNoSetorAtivo = computed(() => this.idsRotacaoAbertaDoSetorAtivo().length > 0);

  readonly setorOptions = computed(() => {
    const nucleoId = this.escala()?.nucleoId;
    return this.setoresDoNucleo().filter((s) => s.nucleoId === nucleoId);
  });

  readonly elegiveisOptions = computed<PciSelectOption[]>(() => [
    { label: 'DO', value: DO_VALUE },
    ...this.elegiveis().map((s) => ({
      label: s.nome,
      value: s.id,
    })),
  ]);

  /** Opções pra segunda pessoa de uma posição — sem "DO" (reforço em folga equivale a não ter
   * reforço, então a opção não agrega nada aqui). */
  readonly elegiveisOptionsSemDo = computed<PciSelectOption[]>(() =>
    this.elegiveisOptions().filter((o) => o.value !== DO_VALUE),
  );

  readonly isRascunhoOuFinalizada = computed(() => {
    const status = this.escala()?.status;
    return status === 'Rascunho' || status === 'Finalizada';
  });

  ngOnInit(): void {
    this.escala.set(this.escalaInicial());
    this.adminApi.listMeusSetores().subscribe({
      next: (items) => this.setoresDoNucleo.set(items),
      error: () => this.error.set('Não foi possível carregar os setores do núcleo.'),
    });
    const inicial = this.escalaInicial();
    if (inicial.nucleoId) {
      this.loadElegiveis({ nucleoId: inicial.nucleoId });
    } else if (inicial.setorId) {
      this.loadElegiveis({ setorId: inicial.setorId });
    }
  }

  isSetorSelected(setorId: string | null): boolean {
    return this.escala()?.setores.some((s) => s.setorId === setorId) ?? false;
  }

  /** Rótulo do grupo (coluna) na grade/seção de equipes — "Agentes" quando não é um setor
   * real (`setorId` nulo), senão "SIGLA (Nome)". */
  setorGrupoLabel(setor: { setorId: string | null; setorSigla: string; setorNome: string }): string {
    return setor.setorId ? `${setor.setorSigla} (${setor.setorNome})` : 'Agentes';
  }

  /** Reforço (segunda pessoa na mesma posição/vaga) só faz sentido pro grupo Agentes — os
   * demais setores têm uma equipe por especialidade, sem vaga solta pra reforçar. */
  isAgentesSetor(setor: { setorId: string | null }): boolean {
    return setor.setorId === null;
  }

  fmt(iso?: string | null): string {
    if (!iso) return '—';
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  /** `setorId` nulo alterna o grupo "Agentes" (servidores à disposição do núcleo, sem
   * setor específico).
   *
   * A ordem enviada ao backend NÃO pode depender da ordem de inserção de um `Set` (desmarcar e
   * remarcar um setor reordenava tudo, porque `Set.add` de um item removido o reinsere no fim).
   * Em vez disso recalcula a ordem inteira do zero a cada toggle, sempre a partir da mesma
   * lista estável (`setorOptions()`, carregada uma vez em `ngOnInit`), com Agentes sempre por
   * último — assim cada setor real fica sempre na mesma posição relativa entre os outros
   * setores reais, e Agentes nunca "pula" de lugar. */
  toggleSetor(setorId: string | null): void {
    const escala = this.escala();
    if (!escala) return;

    const atuais = new Set(escala.setores.map((s) => s.setorId));
    if (atuais.has(setorId)) {
      atuais.delete(setorId);
    } else {
      atuais.add(setorId);
    }

    const setoresReaisSelecionados = this.setorOptions()
      .filter((s) => atuais.has(s.id))
      .map((s) => s.id as string | null);
    const ordemFinal = atuais.has(null)
      ? [...setoresReaisSelecionados, null]
      : setoresReaisSelecionados;
    const setores = ordemFinal.map((id, index) => ({ setorId: id, ordem: index + 1 }));

    this.working.set(true);
    this.error.set(null);
    this.api.configurarSetores(escala.id, { setores }).subscribe({
      next: (updated) => {
        this.setEscala(updated);
        this.working.set(false);
      },
      error: (err) => {
        this.error.set(errMsg(err, 'Não foi possível salvar os setores.'));
        this.working.set(false);
      },
    });
  }

  adicionarEquipe(escalaResumidaSetorId: string): void {
    const escala = this.escala();
    if (!escala) return;

    this.working.set(true);
    this.error.set(null);
    this.api.configurarEquipe(escala.id, { escalaResumidaSetorId }).subscribe({
      next: (updated) => {
        this.setEscala(updated);
        this.working.set(false);
      },
      error: (err) => {
        this.error.set(errMsg(err, 'Não foi possível adicionar a equipe.'));
        this.working.set(false);
      },
    });
  }

  removerEquipe(equipeId: string): void {
    const escala = this.escala();
    if (!escala) return;
    this.working.set(true);
    this.error.set(null);
    this.api.removerEquipe(escala.id, equipeId).subscribe({
      next: (updated) => {
        this.setEscala(updated);
        this.working.set(false);
      },
      error: (err) => {
        this.error.set(errMsg(err, 'Não foi possível remover a equipe.'));
        this.working.set(false);
      },
    });
  }

  abrirRotacao(equipe: EscalaResumidaEquipe): void {
    const membrosOrdenados = [...equipe.rotacao].sort((a, b) => a.posicao - b.posicao);
    const base = membrosOrdenados.length > 0
      ? membrosOrdenados.map((m) => ({ principal: m.servidorId ?? DO_VALUE, segunda: m.servidorId2 ?? '' }))
      : [{ principal: DO_VALUE, segunda: '' }];
    const membros = base.map((v) => this.novaPosicaoDraft(v.principal, v.segunda));

    const draft: RotacaoDraft = {
      equipeId: equipe.id,
      dataInicioCiclo: new FormControl<string>(
        (equipe.dataInicioCiclo ?? this.escala()?.dataInicio ?? '').slice(0, 10),
        { nonNullable: true, validators: Validators.required },
      ),
      membros,
    };
    this.rotacaoAberta.update((map) => ({ ...map, [equipe.id]: draft }));
  }

  isRotacaoAberta(equipeId: string): boolean {
    return !!this.rotacaoAberta()[equipeId];
  }

  rotacaoDraft(equipeId: string): RotacaoDraft | undefined {
    return this.rotacaoAberta()[equipeId];
  }

  private novaPosicaoDraft(principal: string, segunda: string): PosicaoDraft {
    return {
      principal: new FormControl<string>(principal, { nonNullable: true }),
      segunda: new FormControl<string>(segunda, { nonNullable: true }),
    };
  }

  addPosicao(equipeId: string): void {
    const draft = this.rotacaoAberta()[equipeId];
    if (!draft) return;
    draft.membros.push(this.novaPosicaoDraft(DO_VALUE, ''));
    this.rotacaoAberta.update((map) => ({ ...map, [equipeId]: { ...draft } }));
  }

  removePosicao(equipeId: string, index: number): void {
    const draft = this.rotacaoAberta()[equipeId];
    if (!draft || draft.membros.length <= 1) return;
    draft.membros.splice(index, 1);
    this.rotacaoAberta.update((map) => ({ ...map, [equipeId]: { ...draft } }));
  }

  /** Segunda pessoa da posição do rodízio — oferecida só pra Agentes (vaga solta, sem
   * especialidade fixa); os demais setores mantêm uma pessoa por posição. */
  temSegundaPosicaoAberta(equipeId: string, index: number): boolean {
    const draft = this.rotacaoAberta()[equipeId];
    return !!draft?.membros[index]?.segunda.value;
  }

  abrirSegundaPosicao(equipeId: string, index: number): void {
    const draft = this.rotacaoAberta()[equipeId];
    const posicao = draft?.membros[index];
    if (!posicao) return;
    posicao.segunda.setValue(this.elegiveis()[0]?.id ?? '');
    this.rotacaoAberta.update((map) => ({ ...map, [equipeId]: { ...draft } }));
  }

  removerSegundaPosicao(equipeId: string, index: number): void {
    const draft = this.rotacaoAberta()[equipeId];
    const posicao = draft?.membros[index];
    if (!posicao) return;
    posicao.segunda.setValue('');
  }

  cancelarRotacoesDoSetorAtivo(): void {
    const ids = new Set(this.idsRotacaoAbertaDoSetorAtivo());
    this.rotacaoAberta.update((map) =>
      Object.fromEntries(Object.entries(map).filter(([id]) => !ids.has(id))),
    );
  }

  /** Salva o rodízio de todas as equipes abertas do setor ativo numa tacada só — uma requisição
   * por equipe, em sequência (cada `PUT` já regera o setor inteiro no servidor, então a ordem
   * não muda o resultado final; sequencial só evita disparar tudo de uma vez à toa). */
  salvarRotacoesDoSetorAtivo(): void {
    const escala = this.escala();
    const ids = this.idsRotacaoAbertaDoSetorAtivo();
    if (!escala || ids.length === 0) return;

    const drafts = this.rotacaoAberta();
    const semAncora = ids.some((id) => drafts[id].dataInicioCiclo.invalid);
    if (semAncora) {
      this.error.set('Informe a data de início do ciclo em todas as equipes abertas.');
      return;
    }

    this.working.set(true);
    this.error.set(null);
    from(ids)
      .pipe(
        concatMap((equipeId) => {
          const draft = drafts[equipeId];
          const membros = draft.membros.map((pos, index) => ({
            posicao: index,
            servidorId: pos.principal.value === DO_VALUE || !pos.principal.value ? null : pos.principal.value,
            servidorId2: pos.segunda.value || null,
          }));
          return this.api.configurarRotacao(escala.id, equipeId, {
            dataInicioCiclo: draft.dataInicioCiclo.value,
            membros,
          });
        }),
      )
      .subscribe({
        next: (updated) => this.setEscala(updated),
        error: (err) => {
          this.error.set(errMsg(err, 'Não foi possível salvar os rodízios.'));
          this.working.set(false);
        },
        complete: () => {
          this.cancelarRotacoesDoSetorAtivo();
          this.working.set(false);
          this.feedback.showSuccess('Rodízios salvos — o mês foi preenchido automaticamente.');
        },
      });
  }

  onCellSave(event: {
    equipeId: string;
    data: string;
    servidorId: string | null;
    isFolga: boolean;
    servidorId2?: string | null;
    isFolga2?: boolean;
  }): void {
    const escala = this.escala();
    if (!escala) return;

    this.api
      .upsertDia(escala.id, event.equipeId, {
        data: event.data,
        servidorId: event.servidorId,
        textoLivre: null,
        isFolga: event.isFolga,
        servidorId2: event.servidorId2 ?? null,
        isFolga2: event.isFolga2 ?? false,
      })
      .subscribe({
        next: (updated) => this.setEscala(updated),
        error: (err) => {
          const msg = errMsg(err, 'Não foi possível salvar a célula.');
          this.error.set(msg);
          this.toast.showError(msg);
        },
      });
  }

  onCellRevert(event: { equipeId: string; data: string }): void {
    const escala = this.escala();
    if (!escala) return;

    this.api.reverterDia(escala.id, event.equipeId, event.data).subscribe({
      next: (updated) => this.setEscala(updated),
      error: (err) => {
        const msg = errMsg(err, 'Não foi possível reverter a célula para a regra automática.');
        this.error.set(msg);
        this.toast.showError(msg);
      },
    });
  }

  downloadPdf(): void {
    const id = this.escala()?.id;
    if (!id) return;
    this.api.downloadPdf(id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'escala-resumida.pdf';
        a.click();
        URL.revokeObjectURL(url);
        this.feedback.showSuccess('PDF gerado com sucesso.');
      },
      error: (err) => {
        const msg = errMsg(err, 'Falha ao exportar PDF.');
        this.error.set(msg);
        this.toast.showError(msg);
      },
    });
  }

  private loadElegiveis(container: { nucleoId: string } | { setorId: string }): void {
    this.api.listServidoresElegiveis(container).subscribe({ next: (items) => this.elegiveis.set(items) });
  }

  private setEscala(escala: EscalaResumidaDetail): void {
    this.escala.set(escala);
    this.escalaChange.emit(escala);
  }
}
