import type { OrigemOcorrencia, StatusEscala } from './escalas.models';

/** Exatamente um entre `nucleoId` e `setorId` vem preenchido: escala resumida de núcleo
 * (compartilhada entre os setores que ele engloba, mais o grupo "Agentes") ou de um único
 * setor sem núcleo (sem grupo de setores participantes). */
export interface EscalaResumidaListItem {
  id: string;
  identificacao: string;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  ano: number;
  mes: number;
  dataInicio: string;
  dataFim: string;
  status: StatusEscala;
  createdAt: string;
  createdBy?: string | null;
  quantidadeSetores: number;
  setoresSiglas: string[];
}

/** `servidorId2` é um reforço opcional na mesma posição (ex.: vaga de Agentes com duas
 * pessoas) — hoje só oferecido pra Agentes na UI. */
export interface EscalaResumidaRotacaoMembro {
  id: string;
  posicao: number;
  servidorId?: string | null;
  servidorNome?: string | null;
  servidorId2?: string | null;
  servidorNome2?: string | null;
}

export interface EscalaResumidaDia {
  id: string;
  data: string;
  servidorId?: string | null;
  servidorNome?: string | null;
  servidorId2?: string | null;
  servidorNome2?: string | null;
  isFolga2: boolean;
  textoLivre?: string | null;
  isFolga: boolean;
  rotulo: string;
  origem: OrigemOcorrencia;
  rotacaoMembroId?: string | null;
}

export interface EscalaResumidaEquipe {
  id: string;
  nome: string;
  ordem: number;
  dataInicioCiclo?: string | null;
  rotacao: EscalaResumidaRotacaoMembro[];
  dias: EscalaResumidaDia[];
}

/** `setorId` nulo representa o grupo "Agentes" — servidores lotados direto no núcleo, à
 * disposição, sem setor específico. */
export interface EscalaResumidaSetor {
  id: string;
  setorId: string | null;
  setorNome: string;
  setorSigla: string;
  ordem: number;
  equipes: EscalaResumidaEquipe[];
}

export interface EscalaResumidaDetail {
  id: string;
  identificacao: string;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  ano: number;
  mes: number;
  dataInicio: string;
  dataFim: string;
  status: StatusEscala;
  observacao?: string | null;
  escalaId?: string | null;
  createdAt: string;
  createdBy?: string | null;
  setores: EscalaResumidaSetor[];
}

export interface EscalaResumidaServidorElegivel {
  id: string;
  nome: string;
  matricula: string;
  setorId?: string | null;
  setorNome?: string | null;
}

export interface EscalaResumidaAnteriorInfo {
  id: string;
  ano: number;
  mes: number;
  identificacao: string;
  status: StatusEscala;
  quantidadeSetores: number;
}

/** Exatamente um entre `nucleoId` e `setorId` deve ser informado. */
export interface CreateEscalaResumidaPayload {
  nucleoId?: string | null;
  setorId?: string | null;
  ano: number;
  mes: number;
  observacao?: string | null;
}

export interface UpdateEscalaResumidaPayload {
  observacao?: string | null;
}

/** `setorId` nulo pede o grupo "Agentes" (só um por escala resumida). */
export interface ConfigurarSetorItem {
  setorId: string | null;
  ordem: number;
}

export interface ConfigurarSetoresPayload {
  setores: ConfigurarSetorItem[];
}

/** Nome/ordem não vão no payload — o backend sempre deriva a numeração ("Equipe 01", "Equipe
 * 02", ...) a partir de quantas equipes aquele setor já tem, pra nunca depender de uma
 * contagem calculada no cliente (ver bug de numeração cruzando setores). */
export interface ConfigurarEquipePayload {
  escalaResumidaSetorId: string;
}

export interface AtualizarEquipePayload {
  nome: string;
  ordem: number;
}

export interface RotacaoMembroItem {
  posicao: number;
  servidorId?: string | null;
  servidorId2?: string | null;
}

export interface ConfigurarRotacaoPayload {
  dataInicioCiclo: string;
  membros: RotacaoMembroItem[];
}

export interface UpsertDiaPayload {
  data: string;
  servidorId?: string | null;
  servidorId2?: string | null;
  isFolga2?: boolean;
  textoLivre?: string | null;
  isFolga: boolean;
}

export interface CopiarEscalaResumidaPayload {
  ano: number;
  mes: number;
}

export interface PagedEscalasResumidas {
  items: EscalaResumidaListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
