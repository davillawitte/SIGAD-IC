import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import type {
  EscalaResumidaDetail,
  EscalaResumidaEquipe,
  EscalaResumidaServidorElegivel,
  EscalaResumidaSetor,
} from '../../models/escalas-resumidas.models';

const WEEK_LETTERS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
const DO_VALUE = '__DO__';

function formatLocalDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function daysInRange(dataInicio: string, dataFim: string): string[] {
  const start = new Date(dataInicio.slice(0, 10) + 'T00:00:00');
  const end = new Date(dataFim.slice(0, 10) + 'T00:00:00');
  const list: string[] = [];
  for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
    list.push(formatLocalDate(d));
  }
  return list;
}

function isWeekend(day: string): boolean {
  const wd = new Date(day + 'T00:00:00').getDay();
  return wd === 0 || wd === 6;
}

/**
 * Grade pivotada da escala resumida: linha = dia (uma coluna de data só, sem repetir),
 * grupo de colunas = setor, subcoluna = equipe, célula = quem ocupa a vaga naquele dia.
 * Eixo diferente do `escala-matrix` (que é servidor fixo x dia com código de ocorrência) —
 * aqui a coluna é a vaga, e quem a preenche muda de um dia pro outro.
 */
@Component({
  selector: 'app-escala-resumida-grid',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './escala-resumida-grid.html',
  styleUrl: './escala-resumida-grid.scss',
})
export class EscalaResumidaGrid {
  readonly escala = input.required<EscalaResumidaDetail>();
  readonly editable = input(false);
  /** Servidores elegíveis do núcleo (mesma lista usada pra configurar o rodízio) — a segunda
   * pessoa de uma vaga não precisa já estar no pool de rodízio da equipe, então usa esta lista
   * mais ampla em vez de `cellOptions`. */
  readonly elegiveis = input<EscalaResumidaServidorElegivel[]>([]);

  readonly cellSave = output<{
    equipeId: string;
    data: string;
    servidorId: string | null;
    isFolga: boolean;
    servidorId2?: string | null;
    isFolga2?: boolean;
  }>();

  /** Pedido de reverter uma célula com override manual de volta pro valor calculado pelo
   * rodízio — sem isso, uma célula que virou "Manual" (mesmo sem querer, ex.: um clique em
   * branco) fica presa nesse valor pra sempre: `RegerarSetorAsync` nunca sobrescreve uma
   * célula manual, e não existia nenhum jeito de desfazer isso pela grade. */
  readonly cellRevert = output<{ equipeId: string; data: string }>();

  private readonly cellControls = new Map<string, FormControl<string>>();
  private readonly cellControls2 = new Map<string, FormControl<string>>();

  /** Chaves (equipe+dia) com o seletor de segunda pessoa aberto sem valor ainda — some assim
   * que uma segunda pessoa/DO é escolhida (o próprio valor salvo já indica que está "aberto"),
   * ou quando removida. */
  private readonly segundaPessoaAberta = new Set<string>();

  readonly days = computed(() => {
    const e = this.escala();
    return daysInRange(e.dataInicio, e.dataFim);
  });

  /** Só setores com ao menos uma equipe configurada entram na grade — um setor marcado pra
   * participar da escala mas ainda sem equipe geraria um grupo de coluna vazio (`colspan="0"`),
   * o que descasa o cabeçalho das colunas de dado reais (ex.: nomes de "Agentes" aparecendo
   * visualmente sob o cabeçalho de outro setor). */
  readonly setoresComEquipes = computed(() =>
    this.escala().setores.filter((s) => s.equipes.length > 0),
  );

  isWeekend = isWeekend;

  dayLabel(day: string): string {
    const d = new Date(day + 'T00:00:00');
    return `${WEEK_LETTERS[d.getDay()]} ${String(d.getDate()).padStart(2, '0')}`;
  }

  rotulo(equipe: EscalaResumidaEquipe, day: string): string {
    return equipe.dias.find((x) => x.data.slice(0, 10) === day)?.rotulo ?? '';
  }

  isDo(equipe: EscalaResumidaEquipe, day: string): boolean {
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    return dia?.isFolga === true;
  }

  isManual(equipe: EscalaResumidaEquipe, day: string): boolean {
    return equipe.dias.find((x) => x.data.slice(0, 10) === day)?.origem === 'Manual';
  }

  /** "Agentes" (sem setor específico) é o único grupo onde faz sentido escalar uma segunda
   * pessoa avulsa na mesma vaga/dia — os demais setores têm uma equipe por especialidade. */
  isAgentesSetor(setor: EscalaResumidaSetor): boolean {
    return setor.setorId === null;
  }

  /** Opções do select da célula: a vaga fica em branco, é folga (DO), ou é um dos membros
   * já cadastrados no rodízio — nunca texto livre.
   *
   * Não basta olhar só o pool da própria equipe: quando equipes-irmãs (mesmo tamanho de pool
   * e mesma âncora) trocam de pool entre si a cada ciclo completo (ver
   * `EscalaResumidaRotacaoExpander` no backend), a coluna desta equipe pode mostrar, em dias
   * diferentes, gente do pool de OUTRA equipe do grupo — e o valor salvo pra essa célula é o
   * ID dessa pessoa. Se as opções do select vierem só do pool próprio, esse valor não bate
   * com nenhuma `<option>` e o navegador mostra a célula em branco mesmo com dado salvo. Por
   * isso as opções juntam o pool de toda equipe-irmã do mesmo grupo de rodízio, não só o dela. */
  cellOptions(setor: EscalaResumidaSetor, equipe: EscalaResumidaEquipe): PciSelectOption[] {
    const grupo = setor.equipes.filter(
      (e) => e.rotacao.length > 0 && e.rotacao.length === equipe.rotacao.length
        && e.dataInicioCiclo === equipe.dataInicioCiclo,
    );
    const membros = new Map<string, string>();
    for (const e of grupo) {
      for (const m of e.rotacao) {
        if (m.servidorId) membros.set(m.servidorId, m.servidorNome ?? '—');
        if (m.servidorId2) membros.set(m.servidorId2, m.servidorNome2 ?? '—');
      }
    }
    return [
      { label: '—', value: '' },
      { label: 'DO', value: DO_VALUE },
      ...[...membros.entries()].map(([value, label]) => ({ label, value })),
    ];
  }

  /** Opções da segunda pessoa: qualquer servidor elegível do núcleo (não precisa já estar no
   * pool de rodízio desta equipe — é um reforço avulso) mais "DO", exceto quem já é a pessoa
   * principal do dia. */
  segundaPessoaOptions(equipe: EscalaResumidaEquipe, day: string): PciSelectOption[] {
    const principal = this.currentValue(equipe, day);
    return [
      { label: '—', value: '' },
      { label: 'DO', value: DO_VALUE },
      ...this.elegiveis()
        .filter((s) => s.id !== principal)
        .map((s) => ({
          label: s.nome,
          value: s.id,
        })),
    ];
  }

  /** Um `FormControl` por célula (chave equipe+dia), ressincronizado com o dado vindo do
   * servidor sempre que ele muda por fora (ex.: regeneração do rodízio) sem reemitir. */
  cellControl(equipe: EscalaResumidaEquipe, day: string): FormControl<string> {
    const key = `${equipe.id}|${day}`;
    const value = this.currentValue(equipe, day);
    let ctrl = this.cellControls.get(key);
    if (!ctrl) {
      ctrl = new FormControl<string>(value, { nonNullable: true });
      ctrl.valueChanges.subscribe((v) => this.emitPrincipalChange(equipe, day, v));
      this.cellControls.set(key, ctrl);
    } else if (ctrl.value !== value) {
      ctrl.setValue(value, { emitEvent: false });
    }
    return ctrl;
  }

  /** `FormControl` da segunda pessoa — só existe (e só é renderizado) quando já há reforço
   * salvo ou o usuário acabou de clicar "+ 2ª pessoa" nesta célula. */
  cellControl2(equipe: EscalaResumidaEquipe, day: string): FormControl<string> {
    const key = `${equipe.id}|${day}`;
    const value = this.currentValue2(equipe, day);
    let ctrl = this.cellControls2.get(key);
    if (!ctrl) {
      ctrl = new FormControl<string>(value, { nonNullable: true });
      ctrl.valueChanges.subscribe((v) => this.emitSegundaChange(equipe, day, v));
      this.cellControls2.set(key, ctrl);
    } else if (ctrl.value !== value) {
      ctrl.setValue(value, { emitEvent: false });
    }
    return ctrl;
  }

  temSegundaPessoa(equipe: EscalaResumidaEquipe, day: string): boolean {
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    return !!(dia?.servidorId2 || dia?.isFolga2) || this.segundaPessoaAberta.has(this.cellKey(equipe, day));
  }

  abrirSegundaPessoa(equipe: EscalaResumidaEquipe, day: string): void {
    this.segundaPessoaAberta.add(this.cellKey(equipe, day));
  }

  removerSegundaPessoa(equipe: EscalaResumidaEquipe, day: string): void {
    if (!this.editable()) return;
    this.segundaPessoaAberta.delete(this.cellKey(equipe, day));
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    this.cellControls2.delete(this.cellKey(equipe, day));
    this.cellSave.emit({
      equipeId: equipe.id,
      data: day,
      servidorId: dia?.servidorId ?? null,
      isFolga: dia?.isFolga ?? false,
      servidorId2: null,
      isFolga2: false,
    });
  }

  /** Descarta o override manual da célula e deixa o rodízio recalcular o valor dela de
   * novo — `cellControl`/`cellControl2` são recriados a partir do dado que voltar do
   * servidor, então não precisam de limpeza manual aqui. */
  reverterParaRegra(equipe: EscalaResumidaEquipe, day: string): void {
    if (!this.editable()) return;
    this.cellRevert.emit({ equipeId: equipe.id, data: day });
  }

  private cellKey(equipe: EscalaResumidaEquipe, day: string): string {
    return `${equipe.id}|${day}`;
  }

  private currentValue(equipe: EscalaResumidaEquipe, day: string): string {
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    if (!dia) return '';
    if (dia.isFolga) return DO_VALUE;
    return dia.servidorId ?? '';
  }

  private currentValue2(equipe: EscalaResumidaEquipe, day: string): string {
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    if (!dia) return '';
    if (dia.isFolga2) return DO_VALUE;
    return dia.servidorId2 ?? '';
  }

  /** Troca da pessoa principal: mantém o reforço (segunda pessoa) como está — só o rótulo no
   * PDF/visão final é que recombina os dois nomes. */
  private emitPrincipalChange(equipe: EscalaResumidaEquipe, day: string, value: string): void {
    if (!this.editable()) return;
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    this.cellSave.emit({
      equipeId: equipe.id,
      data: day,
      servidorId: value === DO_VALUE || !value ? null : value,
      isFolga: value === DO_VALUE,
      servidorId2: dia?.servidorId2 ?? null,
      isFolga2: dia?.isFolga2 ?? false,
    });
  }

  private emitSegundaChange(equipe: EscalaResumidaEquipe, day: string, value: string): void {
    if (!this.editable()) return;
    this.segundaPessoaAberta.delete(this.cellKey(equipe, day));
    const dia = equipe.dias.find((x) => x.data.slice(0, 10) === day);
    this.cellSave.emit({
      equipeId: equipe.id,
      data: day,
      servidorId: dia?.servidorId ?? null,
      isFolga: dia?.isFolga ?? false,
      servidorId2: value === DO_VALUE || !value ? null : value,
      isFolga2: value === DO_VALUE,
    });
  }
}
