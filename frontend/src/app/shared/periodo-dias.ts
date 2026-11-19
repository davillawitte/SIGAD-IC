import type { AbstractControl } from '@angular/forms';

/** Datas no formato ISO (`aaaa-mm-dd`), sem fuso: contas em UTC pra não escorregar um dia. */
function toUtc(iso: string): number | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso ?? '');
  return m ? Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : null;
}

function fromUtc(ms: number): string {
  return new Date(ms).toISOString().slice(0, 10);
}

const DIA_MS = 24 * 60 * 60 * 1000;

/** Data fim de um período de `dias` dias corridos começando em `inicio` (o dia de início conta):
 * 01/12 + 15 dias = 15/12. Nulo se a data ou a quantidade forem inválidas. */
export function dataFimPorDias(inicio: string, dias: number): string | null {
  const ini = toUtc(inicio);
  if (ini === null || !Number.isInteger(dias) || dias < 1) return null;
  return fromUtc(ini + (dias - 1) * DIA_MS);
}

/** Quantidade de dias corridos de `inicio` a `fim`, contando os dois. Nulo se inválido. */
export function diasDoPeriodo(inicio: string, fim: string): number | null {
  const ini = toUtc(inicio);
  const f = toUtc(fim);
  if (ini === null || f === null || f < ini) return null;
  return Math.round((f - ini) / DIA_MS) + 1;
}

/**
 * Liga os campos "Data início", "Quantidade de dias" e "Data fim" de um formulário: digitar a
 * quantidade (ou mudar o início com a quantidade preenchida) calcula a data fim; mudar a data fim
 * à mão atualiza a quantidade. Devolve a função que desfaz as inscrições.
 */
export function ligarPeriodoPorDias(
  inicio: AbstractControl<string>,
  dias: AbstractControl<string>,
  fim: AbstractControl<string>,
): () => void {
  let sincronizando = false;
  const aplicar = (fn: () => void) => {
    if (sincronizando) return;
    sincronizando = true;
    try {
      fn();
    } finally {
      sincronizando = false;
    }
  };

  const calcularFim = () =>
    aplicar(() => {
      const fimCalculado = dataFimPorDias(inicio.value, Number(dias.value));
      if (fimCalculado && fimCalculado !== fim.value) fim.setValue(fimCalculado);
    });
  const calcularDias = () =>
    aplicar(() => {
      const total = diasDoPeriodo(inicio.value, fim.value);
      const texto = total === null ? '' : String(total);
      if (texto !== dias.value) dias.setValue(texto);
    });

  const subs = [
    dias.valueChanges.subscribe(calcularFim),
    inicio.valueChanges.subscribe(() => (dias.value ? calcularFim() : calcularDias())),
    fim.valueChanges.subscribe(calcularDias),
  ];
  calcularDias();
  return () => subs.forEach((s) => s.unsubscribe());
}
