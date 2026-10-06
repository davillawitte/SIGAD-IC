import type { EscalaJornada, EscalaServidor } from '../models/escalas.models';
import type { RegimeCodigo } from './escala-ocorrencia.builder';

/** 0 = plantão 24h, 1 = plantão 12h, 2 = expediente, 3 = sem regime. */
export type GrupoRegime = 0 | 1 | 2 | 3;

/** Grupo de cada regime oferecido no passo de regimes da escala. */
export function grupoDoRegime(codigo: RegimeCodigo | null | undefined): GrupoRegime {
  switch (codigo) {
    case '24X72':
    case 'PT24_TL12':
      return 0;
    case '12X36':
      return 1;
    case 'EXP_ADM':
    case 'EXP_ADM_TARDE':
      return 2;
    default:
      return 3;
  }
}

function grupoDaJornada(j: EscalaJornada): GrupoRegime {
  if (j.tipoJornada !== 'Plantao') return 2;
  return Number(j.horas ?? 0) >= 24 ? 0 : 1;
}

/**
 * Grupo pelo regime do servidor — o escolhido no passo de regimes, que fica gravado na jornada
 * (24x72 / 24h + laudo 12h / rodízio da resumida = 24h; 12x36 = 12h; expediente). As ocorrências
 * do mês não contam: trocar um código na grade não muda a pessoa de grupo.
 * Espelha `EscalaServidorOrdenacao.GrupoRegime` no backend (ordem do PDF).
 */
export function grupoRegime(servidor: EscalaServidor): GrupoRegime {
  const jornadas = servidor.jornadas ?? [];
  if (!jornadas.length) return 3;
  return Math.min(...jornadas.map(grupoDaJornada)) as GrupoRegime;
}

/** Chave alfabética sem diferenciar maiúsculas nem acentos — mesma regra do backend. */
export function chaveNome(nome: string | null | undefined): string {
  return (nome ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

function compareOrdinal(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0;
}

/**
 * Ordem de exibição na escala: agrupada por regime (24h, 12h, expediente) e alfabética.
 * `grupoPorServidor` sobrepõe o grupo vindo das jornadas — usado no rascunho do wizard, em que
 * o regime já foi escolhido no passo de regimes mas as jornadas só existem depois de salvar.
 */
export function ordenarServidoresPorRegime<T extends EscalaServidor>(
  servidores: readonly T[],
  grupoPorServidor?: ReadonlyMap<string, GrupoRegime> | null,
): T[] {
  return servidores
    .map((s) => ({
      s,
      grupo: grupoPorServidor?.get(s.servidorId) ?? grupoRegime(s),
      nome: chaveNome(s.servidorNome),
    }))
    .sort(
      (a, b) =>
        a.grupo - b.grupo ||
        compareOrdinal(a.nome, b.nome) ||
        compareOrdinal(a.s.matricula ?? '', b.s.matricula ?? ''),
    )
    .map((x) => x.s);
}
