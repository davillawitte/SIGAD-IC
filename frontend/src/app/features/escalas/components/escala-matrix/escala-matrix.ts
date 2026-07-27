import { CommonModule } from '@angular/common';
import { Component, Input, computed, input } from '@angular/core';

import type { EscalaDetail } from '../../models/escalas.models';

const MES_NOMES = [
  '',
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
];

const MES_ABREV = [
  '',
  'Jan',
  'Fev',
  'Mar',
  'Abr',
  'Mai',
  'Jun',
  'Jul',
  'Ago',
  'Set',
  'Out',
  'Nov',
  'Dez',
];

const WEEK_LETTERS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];

const LEGEND = [
  { codigo: 'M', nome: 'Manhã 6h' },
  { codigo: 'T', nome: 'Tarde 6h' },
  { codigo: 'TL6', nome: 'Teletrabalho 6h' },
  { codigo: 'PD', nome: 'Plantão Diurno 12h' },
  { codigo: 'PN', nome: 'Plantão Noturno 12h' },
  { codigo: 'PT', nome: 'Plantão 24h' },
  { codigo: 'CF', nome: 'Chefia Núcleo 12h' },
  { codigo: 'FR', nome: 'Férias' },
  { codigo: 'F', nome: 'Feriado' },
  { codigo: 'LP', nome: 'Licença Prêmio' },
  { codigo: 'LM', nome: 'Licença Médica' },
  { codigo: 'LO', nome: 'Licença (outro)' },
  { codigo: 'D', nome: 'Descanso' },
  { codigo: 'R', nome: 'Remoção' },
];

const VALID_OCORRENCIA_CODIGOS = new Set(LEGEND.map((x) => x.codigo));

export function isValidOcorrenciaCodigo(codigo: string): boolean {
  return VALID_OCORRENCIA_CODIGOS.has(codigo.trim().toUpperCase());
}

const FOLGA = new Set(['D', 'F', 'FR', 'LP', 'LM', 'LO', 'R']);

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

function dayNumber(day: string): number {
  return new Date(day + 'T00:00:00').getDate();
}

function dayLetter(day: string): string {
  return WEEK_LETTERS[new Date(day + 'T00:00:00').getDay()];
}

function formatVerticalDate(day: string): string {
  const d = new Date(day + 'T00:00:00');
  const letter = WEEK_LETTERS[d.getDay()];
  const dd = String(d.getDate()).padStart(2, '0');
  const mon = MES_ABREV[d.getMonth() + 1] ?? '';
  return `${letter} - ${dd}/${mon}`;
}

function ocorrenciaFor(escala: EscalaDetail, servidorId: string, day: string) {
  const s = escala.servidores.find((x) => x.servidorId === servidorId);
  return s?.ocorrencias.find((o) => o.data.slice(0, 10) === day) ?? null;
}

function codeFor(escala: EscalaDetail, servidorId: string, day: string): string {
  return ocorrenciaFor(escala, servidorId, day)?.tipoOcorrenciaCodigo ?? '';
}

function cellLabelFor(escala: EscalaDetail, servidorId: string, day: string): string {
  return codeFor(escala, servidorId, day);
}

function horasOf(
  escala: EscalaDetail,
  servidorId: string,
  day: string,
): number {
  const s = escala.servidores.find((x) => x.servidorId === servidorId);
  const oc = s?.ocorrencias.find((o) => o.data.slice(0, 10) === day);
  return oc?.horas ?? 0;
}

function isPresencial(codigo: string): boolean {
  if (!codigo) return false;
  if (codigo.toUpperCase().startsWith('TL')) return false;
  return !FOLGA.has(codigo.toUpperCase());
}

function isRemota(codigo: string): boolean {
  return codigo.toUpperCase().startsWith('TL');
}

function chPresencial(escala: EscalaDetail, servidorId: string): number {
  const s = escala.servidores.find((x) => x.servidorId === servidorId);
  if (!s) return 0;
  if (s.cargaHorariaPresencial != null) return Number(s.cargaHorariaPresencial);
  return s.ocorrencias.reduce((acc, o) => {
    const code = o.tipoOcorrenciaCodigo;
    if (!isPresencial(code)) return acc;
    return acc + (o.horas ?? 0);
  }, 0);
}

function chRemota(escala: EscalaDetail, servidorId: string): number {
  const s = escala.servidores.find((x) => x.servidorId === servidorId);
  if (!s) return 0;
  if (s.cargaHorariaRemota != null) return Number(s.cargaHorariaRemota);
  return s.ocorrencias.reduce((acc, o) => {
    if (!isRemota(o.tipoOcorrenciaCodigo)) return acc;
    return acc + (o.horas ?? 0);
  }, 0);
}

@Component({
  selector: 'app-escala-matrix',
  imports: [CommonModule],
  templateUrl: './escala-matrix.html',
  styleUrl: './escala-matrix.scss',
})
export class EscalaMatrix {
  readonly escala = input.required<EscalaDetail>();
  readonly layout = input<'matriz' | 'vertical'>('matriz');
  /** When true, cells are editable inputs (wizard step 3). */
  readonly editable = input(false);
  /** When true, omits institutional header/meta (detail page already shows that). */
  readonly compactHeader = input(false);

  @Input() codeChange: ((servidorId: string, day: string, code: string) => void) | null = null;
  @Input() cellMouseDown: ((servidorId: string, day: string, event: MouseEvent) => void) | null =
    null;
  @Input() cellKeydown: ((servidorId: string, day: string, event: KeyboardEvent) => void) | null =
    null;
  @Input() isCellSelected: ((servidorId: string, day: string) => boolean) | null = null;

  readonly legend = LEGEND;

  readonly days = computed(() => {
    const e = this.escala();
    return daysInRange(e.dataInicio, e.dataFim);
  });

  readonly cargosParticipantes = computed(() => {
    const codes = this.escala()
      .servidores.map((s) => s.cargoCodigo || s.cargoNome)
      .filter(Boolean);
    return [...new Set(codes)].join(', ') || '—';
  });

  readonly mesNome = computed(() => MES_NOMES[this.escala().mes] ?? String(this.escala().mes));

  readonly titulo = computed(() => {
    const e = this.escala();
    const mes = (MES_ABREV[e.mes] ?? String(e.mes)).toUpperCase();
    return `ESCALA DE ${mes}/${e.ano} - ${e.setorNome}`.toUpperCase();
  });

  isWeekend = isWeekend;
  dayNumber = dayNumber;
  dayLetter = dayLetter;
  formatVerticalDate = formatVerticalDate;

  code(servidorId: string, day: string): string {
    return codeFor(this.escala(), servidorId, day);
  }

  cellLabel(servidorId: string, day: string): string {
    return cellLabelFor(this.escala(), servidorId, day);
  }

  chPres(servidorId: string): number {
    return chPresencial(this.escala(), servidorId);
  }

  chRem(servidorId: string): number {
    return chRemota(this.escala(), servidorId);
  }

  dayLabel(day: string): string {
    return `${dayNumber(day)}\n${dayLetter(day)}`;
  }

  onCellInput(event: Event): void {
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

  onBlur(servidorId: string, day: string, event: FocusEvent): void {
    if (!this.editable() || !this.codeChange) return;
    const input = event.target as HTMLInputElement;
    const value = input.value.trim().toUpperCase();
    input.value = value;
    if (!value) {
      this.codeChange(servidorId, day, '');
      return;
    }
    if (!isValidOcorrenciaCodigo(value)) {
      input.value = this.code(servidorId, day);
      return;
    }
    if (value === this.code(servidorId, day)) return;
    this.codeChange(servidorId, day, value);
  }

  onMouseDown(servidorId: string, day: string, event: MouseEvent): void {
    this.cellMouseDown?.(servidorId, day, event);
  }

  onKeydown(servidorId: string, day: string, event: KeyboardEvent): void {
    this.cellKeydown?.(servidorId, day, event);
  }

  selected(servidorId: string, day: string): boolean {
    return this.isCellSelected?.(servidorId, day) ?? false;
  }
}

export {
  daysInRange,
  formatLocalDate,
  isWeekend,
  MES_NOMES,
  MES_ABREV,
  codeFor,
  chPresencial,
  chRemota,
  horasOf,
};
