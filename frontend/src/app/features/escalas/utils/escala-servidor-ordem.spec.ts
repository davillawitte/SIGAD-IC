import { describe, expect, it } from 'vitest';

import type { EscalaJornada, EscalaOcorrencia, EscalaServidor } from '../models/escalas.models';
import {
  chaveNome,
  grupoDoRegime,
  grupoRegime,
  ordenarServidoresPorRegime,
  type GrupoRegime,
} from './escala-servidor-ordem';

function oc(codigo: string, horas: number | null = null): EscalaOcorrencia {
  return { id: codigo, data: '2026-10-01', tipoOcorrenciaCodigo: codigo, horas, origem: 'Manual' };
}

function jornada(tipoJornada: EscalaJornada['tipoJornada'], horas: number): EscalaJornada {
  return {
    id: `${tipoJornada}-${horas}`,
    tipoJornada,
    dataInicio: '2026-10-01',
    dataFim: '2026-10-31',
    horas,
    tipoOcorrenciaCodigo: tipoJornada === 'Plantao' ? 'PT' : 'M',
    recorrenciaTipo: 'CicloPlantao',
  };
}

function servidor(
  nome: string,
  jornadas: EscalaJornada[],
  ocorrencias: EscalaOcorrencia[] = [],
): EscalaServidor {
  return {
    id: nome,
    servidorId: nome,
    cargoId: 'c',
    ordem: 0,
    servidorNome: nome,
    matricula: '1',
    cargoNome: 'Perito',
    cargoCodigo: 'PCF',
    jornadas,
    ocorrencias,
  };
}

const p24 = jornada('Plantao', 24);
const p12 = jornada('Plantao', 12);
const exp = jornada('Expediente', 6);

describe('ordenarServidoresPorRegime', () => {
  it('agrupa pelo regime da jornada (24h, 12h, expediente, sem regime) e alfabético em cada grupo', () => {
    const lista = [
      servidor('ZULEIDE', [exp]),
      servidor('SEM REGIME', []),
      servidor('BRUNO', [p12]),
      servidor('ÁLVARO', [p24]),
      servidor('CARLA', [p24]),
      servidor('ANA', [p12]),
      servidor('ALICE', [exp]),
    ];

    expect(ordenarServidoresPorRegime(lista).map((s) => s.servidorNome)).toEqual([
      'ÁLVARO',
      'CARLA',
      'ANA',
      'BRUNO',
      'ALICE',
      'ZULEIDE',
      'SEM REGIME',
    ]);
  });

  it('ignora os códigos da grade: expediente com um PT digitado continua no expediente', () => {
    expect(grupoRegime(servidor('X', [exp], [oc('PT', 24)]))).toBe(2);
    expect(grupoRegime(servidor('Y', [p24], [oc('FR')]))).toBe(0);
  });

  it('o regime escolhido no wizard sobrepõe as jornadas (rascunho ainda sem jornadas)', () => {
    const grupos = new Map<string, GrupoRegime>([
      ['B', grupoDoRegime('PT24_TL12')],
      ['A', grupoDoRegime('EXP_ADM_TARDE')],
    ]);
    const lista = [servidor('A', []), servidor('B', [])];
    expect(ordenarServidoresPorRegime(lista, grupos).map((s) => s.servidorNome)).toEqual(['B', 'A']);
  });

  it('mapeia cada regime do passo de regimes pro grupo certo', () => {
    expect(grupoDoRegime('24X72')).toBe(0);
    expect(grupoDoRegime('PT24_TL12')).toBe(0);
    expect(grupoDoRegime('12X36')).toBe(1);
    expect(grupoDoRegime('EXP_ADM')).toBe(2);
    expect(grupoDoRegime('EXP_ADM_TARDE')).toBe(2);
    expect(grupoDoRegime(null)).toBe(3);
  });

  it('ordem alfabética ignora acentos e maiúsculas', () => {
    expect(chaveNome('Ângela')).toBe(chaveNome('ANGELA'));
  });
});
