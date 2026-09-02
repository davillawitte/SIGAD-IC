import { describe, expect, it } from 'vitest';

import type { PadraoEscala } from '../models/escalas.models';
import {
  buildOcorrenciasForServidor,
  buildOcorrenciasFromCicloDerivado,
  normalizeDay,
  posicaoNaData,
  primeiraDataParaPosicao,
} from './escala-ocorrencia.builder';

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

const padraoPT24TL12: PadraoEscala = {
  ...padrao24x72,
  id: 'ppt24tl12',
  codigo: 'PT24_TL12',
  nome: 'Plantão 24h + Laudo 12h',
  recorrenciaTipo: 'CicloPersonalizado',
  diasTrabalho: null,
  diasFolga: null,
  sequenciaCiclo: 'PT,D,D,D,TL12,D',
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

  it('PT24_TL12 expande as 6 fases da sequência (PT, folga x3, TL12, folga) e repete no 7º dia', () => {
    const result = buildOcorrenciasForServidor({
      servidorId: 'a',
      days: semana,
      regimesSelected: ['PT24_TL12'],
      padroesByCodigo: new Map([['PT24_TL12', padraoPT24TL12]]),
      servidorInicioCiclo: new Map([['a', '2026-07-06']]),
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual([
      'PT',
      'D',
      'D',
      'D',
      'TL12',
      'D',
      'PT',
    ]);
  });
});

// Espelha os casos de `EscalaResumidaRotacaoExpanderTests.cs` — mesma fórmula de módulo
// negativo-seguro, pra garantir que as duas implementações (frontend e backend) não driftem.
describe('posicaoNaData / primeiraDataParaPosicao', () => {
  it('avança uma posição do pool por dia a partir da âncora', () => {
    const ancora = '2026-08-01';
    const dias = ['2026-08-01', '2026-08-02', '2026-08-03', '2026-08-04', '2026-08-05', '2026-08-06'];

    expect(dias.map((d) => posicaoNaData(d, ancora, 3))).toEqual([0, 1, 2, 0, 1, 2]);
  });

  it('datas antes da âncora mantêm a fase correta sem índice negativo', () => {
    const ancora = '2026-08-15';
    const dias = ['2026-08-13', '2026-08-14', '2026-08-15'];

    expect(dias.map((d) => posicaoNaData(d, ancora, 2))).toEqual([0, 1, 0]);
  });

  it('âncora de mês anterior continua o ciclo em fase no mês seguinte', () => {
    const ancora = '2026-07-30';
    const dias = ['2026-08-01', '2026-08-02', '2026-08-03'];

    expect(dias.map((d) => posicaoNaData(d, ancora, 3))).toEqual([2, 0, 1]);
  });

  it('primeiraDataParaPosicao acha a próxima data cuja posição bate com a alvo', () => {
    // Âncora de julho, pool de 3; a partir de 01/08 (posição 2), a posição 0 só cai em 02/08.
    expect(primeiraDataParaPosicao('2026-08-01', '2026-07-30', 3, 0)).toBe('2026-08-02');
    expect(primeiraDataParaPosicao('2026-08-01', '2026-07-30', 3, 2)).toBe('2026-08-01');
  });
});

describe('buildOcorrenciasFromCicloDerivado', () => {
  it('trabalha na posição 0 do ciclo pessoal e folga nas demais', () => {
    const result = buildOcorrenciasFromCicloDerivado({
      days: ['2026-08-01', '2026-08-02', '2026-08-03', '2026-08-04'],
      ancora: '2026-08-01',
      tamanhoPool: 4,
    });

    expect(result.map((o) => o.tipoOcorrenciaCodigo)).toEqual(['PT', 'D', 'D', 'D']);
    expect(result[0].horas).toBe(24);
  });
});
