import type { EscalaOcorrencia, PadraoEscala } from '../models/escalas.models';

export type RegimeCodigo = 'EXP_ADM' | '12X36' | '24X72' | 'PT24_TL12';

export interface BuildOcorrenciasInput {
  servidorId: string;
  days: string[];
  regimesSelected: RegimeCodigo[];
  padroesByCodigo: Map<string, PadraoEscala>;
  servidorInicioCiclo: Map<string, string>;
}

/** Normaliza YYYY-MM-DD (aceita ISO com horário). */
export function normalizeDay(value: string | null | undefined): string {
  return (value ?? '').trim().slice(0, 10);
}

function emptyOc(day: string): EscalaOcorrencia {
  return oc(day, '');
}

function oc(
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

/** Espelha `EscalaResumidaRotacaoExpander.PosicaoNaData` do backend (mesmo módulo
 * negativo-seguro) — usado só pra derivar sugestões de regime a partir do rodízio de uma
 * escala resumida no frontend, sem precisar de um endpoint novo. Ver
 * `EscalaResumidaRotacaoExpanderTests.cs` pros casos que ambas as implementações devem bater. */
export function posicaoNaData(data: string, ancora: string, tamanhoPool: number): number {
  const d = new Date(normalizeDay(data) + 'T00:00:00');
  const a = new Date(normalizeDay(ancora) + 'T00:00:00');
  const diasDesdeAncora = Math.round((d.getTime() - a.getTime()) / 86_400_000);
  return ((diasDesdeAncora % tamanhoPool) + tamanhoPool) % tamanhoPool;
}

/** Primeira data a partir de `inicioBusca` (inclusive) cuja posição no rodízio bate com
 * `posicaoAlvo` — ancora o ciclo pessoal derivado no início do período da escala sendo criada,
 * mesmo que a âncora original da equipe seja de um período anterior. */
export function primeiraDataParaPosicao(
  inicioBusca: string,
  ancora: string,
  tamanhoPool: number,
  posicaoAlvo: number,
): string | null {
  const d = new Date(normalizeDay(inicioBusca) + 'T00:00:00');
  for (let i = 0; i < tamanhoPool; i++) {
    const iso = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    if (posicaoNaData(iso, ancora, tamanhoPool) === posicaoAlvo) return iso;
    d.setDate(d.getDate() + 1);
  }
  return null;
}

export interface BuildOcorrenciasFromCicloInput {
  days: string[];
  ancora: string;
  tamanhoPool: number;
}

/** Ocorrências de um servidor cujo regime vem do rodízio de uma escala resumida (não um dos 4
 * `RegimeCodigo` fixos, já que o tamanho do pool é arbitrário): trabalha na posição 0 do seu
 * ciclo pessoal (derivado de `primeiraDataParaPosicao`), folga nas demais. */
export function buildOcorrenciasFromCicloDerivado(input: BuildOcorrenciasFromCicloInput): EscalaOcorrencia[] {
  const { days, ancora, tamanhoPool } = input;
  const cycle = Math.max(1, tamanhoPool);
  const normalizedDays = days.map((d) => normalizeDay(d));
  const inicio = normalizeDay(ancora);
  let startIdx = normalizedDays.indexOf(inicio);
  if (startIdx < 0) startIdx = 0;

  return normalizedDays.map((day, idx) => {
    const pos = (((idx - startIdx) % cycle) + cycle) % cycle;
    return pos === 0 ? oc(day, 'PT', 24) : oc(day, 'D');
  });
}

/**
 * Gera a grade de ocorrências do formulário de escala (ex-wizard).
 * Congela o comportamento atual — inclusive EXP_ADM emitindo `D` no fim de semana,
 * diferente do backend que não emite ocorrência nesses dias.
 */
export function buildOcorrenciasForServidor(input: BuildOcorrenciasInput): EscalaOcorrencia[] {
  const { servidorId, days, regimesSelected, padroesByCodigo, servidorInicioCiclo } = input;

  // Multi-regime: escala em branco — só seleção de servidores.
  if (regimesSelected.length !== 1) {
    return days.map((day) => emptyOc(day));
  }

  const regime = regimesSelected[0];
  const padrao = padroesByCodigo.get(regime);
  if (!padrao) {
    return days.map((day) => emptyOc(day));
  }

  if (regime === 'EXP_ADM') {
    return days.map((day) => {
      const wd = new Date(day + 'T00:00:00').getDay();
      if (wd === 0 || wd === 6) {
        return oc(day, 'D');
      }
      return oc(day, 'M', 6, '08:00', '14:00');
    });
  }

  if (padrao.recorrenciaTipo === 'CicloPersonalizado') {
    return buildCicloPersonalizado(padrao, days, servidorId, servidorInicioCiclo);
  }

  const work = Math.max(1, padrao.diasTrabalho ?? 1);
  const folga = Math.max(1, padrao.diasFolga ?? 1);
  const cycle = work + folga;
  const workCode = padrao.tipoOcorrenciaTrabalho || (regime === '24X72' ? 'PT' : 'PD');
  const offCode = padrao.tipoOcorrenciaFolga || 'D';
  const horas = padrao.horasPadrao ?? (regime === '24X72' ? 24 : 12);
  const normalizedDays = days.map((d) => normalizeDay(d));
  const inicio = normalizeDay(servidorInicioCiclo.get(servidorId) ?? normalizedDays[0]);
  let startIdx = normalizedDays.indexOf(inicio);
  if (startIdx < 0) startIdx = 0;

  return normalizedDays.map((day, idx) => {
    // Plantão: fins de semana também entram no ciclo (sem pular domingo/sábado).
    const pos = ((idx - startIdx) % cycle + cycle) % cycle;
    if (pos < work) {
      return oc(day, workCode, horas, padrao.horaInicioPadrao, padrao.horaFimPadrao);
    }
    return oc(day, offCode);
  });
}

/**
 * Ciclo com mais de 2 fases (ex.: 24h trabalho, 72h folga, 12h laudo, 36h folga): avança um
 * código de `padrao.sequenciaCiclo` por dia — mesma técnica de âncora usada acima, só que sobre
 * uma sequência explícita em vez de alternar entre 2 códigos. Espelha
 * `EscalaJornadaExpander.ExpandPersonalizado` no backend.
 */
function buildCicloPersonalizado(
  padrao: PadraoEscala,
  days: string[],
  servidorId: string,
  servidorInicioCiclo: Map<string, string>,
): EscalaOcorrencia[] {
  const sequencia = (padrao.sequenciaCiclo ?? '')
    .split(',')
    .map((c) => c.trim())
    .filter(Boolean);
  if (sequencia.length === 0) {
    return days.map((day) => emptyOc(day));
  }

  const offCode = padrao.tipoOcorrenciaFolga || 'D';
  const normalizedDays = days.map((d) => normalizeDay(d));
  const inicio = normalizeDay(servidorInicioCiclo.get(servidorId) ?? normalizedDays[0]);
  let startIdx = normalizedDays.indexOf(inicio);
  if (startIdx < 0) startIdx = 0;

  return normalizedDays.map((day, idx) => {
    const pos = (((idx - startIdx) % sequencia.length) + sequencia.length) % sequencia.length;
    const codigo = sequencia[pos];
    if (codigo === offCode) {
      return oc(day, codigo);
    }
    return oc(day, codigo, padrao.horasPadrao, padrao.horaInicioPadrao, padrao.horaFimPadrao);
  });
}
