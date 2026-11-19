import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { dataFimPorDias, diasDoPeriodo, ligarPeriodoPorDias } from './periodo-dias';

describe('periodo-dias', () => {
  it('data fim conta o dia de início (atestado de 15 dias a partir de 01/12)', () => {
    expect(dataFimPorDias('2026-12-01', 15)).toBe('2026-12-15');
    expect(dataFimPorDias('2026-12-20', 15)).toBe('2027-01-03');
    expect(dataFimPorDias('2026-12-01', 1)).toBe('2026-12-01');
    expect(dataFimPorDias('2026-12-01', 0)).toBeNull();
    expect(dataFimPorDias('', 10)).toBeNull();
  });

  it('quantidade de dias do período, contando início e fim', () => {
    expect(diasDoPeriodo('2026-12-01', '2026-12-15')).toBe(15);
    expect(diasDoPeriodo('2026-02-27', '2026-03-02')).toBe(4);
    expect(diasDoPeriodo('2026-12-15', '2026-12-01')).toBeNull();
  });

  it('liga os campos: quantidade calcula o fim e o fim atualiza a quantidade', () => {
    const inicio = new FormControl('2026-12-01', { nonNullable: true });
    const dias = new FormControl('', { nonNullable: true });
    const fim = new FormControl('', { nonNullable: true });
    ligarPeriodoPorDias(inicio, dias, fim);

    dias.setValue('15');
    expect(fim.value).toBe('2026-12-15');

    fim.setValue('2026-12-10');
    expect(dias.value).toBe('10');

    inicio.setValue('2026-12-05');
    expect(fim.value).toBe('2026-12-14');
  });
});
