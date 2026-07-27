import type { EscalaOcorrencia, PadraoEscala } from '../models/escalas.models';

export type RegimeCodigo = 'EXP_ADM' | '12X36' | '24X72';

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
