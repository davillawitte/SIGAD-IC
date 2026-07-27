import { describe, expect, it } from 'vitest';

import type { PadraoEscala } from '../models/escalas.models';
import { buildOcorrenciasForServidor, normalizeDay } from './escala-ocorrencia.builder';

const padrao12x36: PadraoEscala = {
  id: 'p12',
  codigo: '12X36',
  nome: '12x36',
  tipoFuncionamento: 'VinteQuatroHoras',
  tipoJornada: 'Plantao',
  recorrenciaTipo: 'CicloPlantao',
  diasTrabalho: 1,
  diasFolga: 1,
  tipoOcorrenciaTrabalho: 'PD',
  tipoOcorrenciaFolga: 'D',
  horaInicioPadrao: '07:00',
  horaFimPadrao: '19:00',
  horasPadrao: 12,
  sistema: true,
  ativo: true,
};

const padrao24x72: PadraoEscala = {
  ...padrao12x36,
  id: 'p24',
  codigo: '24X72',
  nome: '24x72',
  diasTrabalho: 1,
  diasFolga: 3,
  tipoOcorrenciaTrabalho: 'PT',
  horasPadrao: 24,
  horaFimPadrao: '07:00',
};

const padraoExp: PadraoEscala = {
  ...padrao12x36,
  id: 'pexp',
  codigo: 'EXP_ADM',
  nome: 'Expediente',
  tipoFuncionamento: 'Expediente',
  tipoJornada: 'Expediente',
  recorrenciaTipo: 'DiasSemana',
  diasTrabalho: null,
  diasFolga: null,
  tipoOcorrenciaTrabalho: 'M',
  horasPadrao: 6,
  horaInicioPadrao: '08:00',
  horaFimPadrao: '14:00',
};

// 2026-07-06 = segunda; 2026-07-11 = sabado; 2026-07-12 = domingo
const semana = [
  '2026-07-06',
  '2026-07-07',
  '2026-07-08',
  '2026-07-09',
  '2026-07-10',
  '2026-07-11',
  '2026-07-12',
];

describe('escala-ocorrencia.builder', () => {
  it('normalizeDay corta ISO para YYYY-MM-DD', () => {
    expect(normalizeDay('2026-07-06T00:00:00')).toBe('2026-07-06');
    expect(normalizeDay(null)).toBe('');
  });

  it('multi-regime gera grade vazia', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana,
      regimesSelected: ['12X36', '24X72'],
      padroesByCodigo: new Map([['12X36', padrao12x36]]),
      servidorInicioCiclo: new Map(),
    });

    expect(result).toHaveLength(7);
    expect(result.every((o) => o.tipoOcorrenciaCodigo === '')).toBe(true);
  });

  it('EXP_ADM emite M em dias uteis e D no fim de semana', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana,
      regimesSelected: ['EXP_ADM'],
      padroesByCodigo: new Map([['EXP_ADM', padraoExp]]),
      servidorInicioCiclo: new Map(),
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual([
      'M',
      'M',
      'M',
      'M',
      'M',
      'D',
      'D',
    ]);
  });

  it('12X36 alterna a partir da ancora do servidor', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana.slice(0, 4),
      regimesSelected: ['12X36'],
      padroesByCodigo: new Map([['12X36', padrao12x36]]),
      servidorInicioCiclo: new Map([['a', '2026-07-06']]),
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual(['PD', 'D', 'PD', 'D']);
  });

  it('24X72 gera um plantao e tres folgas', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana.slice(0, 4),
      regimesSelected: ['24X72'],
      padroesByCodigo: new Map([['24X72', padrao24x72]]),
      servidorInicioCiclo: new Map([['a', '2026-07-06']]),
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual(['PT', 'D', 'D', 'D']);
  });

  it('ancora no meio do periodo desloca a fase do ciclo', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana.slice(0, 4),
      regimesSelected: ['12X36'],
      padroesByCodigo: new Map([['12X36', padrao12x36]]),
      // Ancora no segundo dia: primeiro dia fica em folga.
      servidorInicioCiclo: new Map([['a', '2026-07-07']]),
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual(['D', 'PD', 'D', 'PD']);
  });
});
