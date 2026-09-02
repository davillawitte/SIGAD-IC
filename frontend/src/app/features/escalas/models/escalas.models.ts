export type StatusEscala = 'Rascunho' | 'Finalizada' | 'Publicada' | 'DevolucaoSolicitada';

export function statusEscalaLabel(status: StatusEscala): string {
  if (status === 'DevolucaoSolicitada') return 'Devolução solicitada';
  return status;
}
export type TipoFuncionamento = 'Expediente' | 'VinteQuatroHoras';
export type TipoJornada = 'Administrativo' | 'Plantao' | 'Expediente' | 'Outro';
export type RecorrenciaTipo =
  | 'Nenhuma'
  | 'TodosOsDias'
  | 'DiasSemana'
  | 'ACadaXDias'
  | 'CicloPlantao'
  | 'CicloPersonalizado';
export type OrigemOcorrencia = 'Regra' | 'Manual';
export type CategoriaOcorrencia = 'Trabalho' | 'Folga' | 'Afastamento' | 'Outro';
export type StatusSolicitacaoDevolucao = 'Pendente' | 'Aprovada' | 'Recusada';

export interface EscalaListItem {
  id: string;
  identificacao: string;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  ano: number;
  mes: number;
  dataInicio: string;
  dataFim: string;
  tipoFuncionamento: TipoFuncionamento;
  status: StatusEscala;
  publicadaEm?: string | null;
  publicadaPor?: string | null;
  createdAt: string;
  createdBy?: string | null;
  resumidaId?: string | null;
}

export interface EscalaOcorrencia {
  id: string;
  data: string;
  tipoOcorrenciaCodigo: string;
  tipoOcorrenciaNome?: string | null;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  origem: OrigemOcorrencia;
  escalaJornadaId?: string | null;
  observacao?: string | null;
}

export interface EscalaJornada {
  id: string;
  padraoEscalaId?: string | null;
  padraoEscalaNome?: string | null;
  tipoJornada: TipoJornada;
  dataInicio: string;
  dataFim: string;
  dataInicioCiclo?: string | null;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  tipoOcorrenciaCodigo: string;
  recorrenciaTipo: RecorrenciaTipo;
  diasSemana?: string | null;
  intervaloDias?: number | null;
  diasTrabalho?: number | null;
  diasFolga?: number | null;
  tipoOcorrenciaFolgaCodigo?: string | null;
  sequenciaCiclo?: string | null;
  observacao?: string | null;
}

export interface EscalaServidor {
  id: string;
  servidorId: string;
  cargoId: string;
  ordem: number;
  servidorNome: string;
  matricula: string;
  cargoNome: string;
  cargoCodigo: string;
  cargaHorariaPresencial?: number;
  cargaHorariaRemota?: number;
  jornadas: EscalaJornada[];
  ocorrencias: EscalaOcorrencia[];
}

export interface EscalaDetail {
  id: string;
  identificacao: string;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  ano: number;
  mes: number;
  dataInicio: string;
  dataFim: string;
  tipoFuncionamento: TipoFuncionamento;
  status: StatusEscala;
  observacao?: string | null;
  publicadaEm?: string | null;
  publicadaPor?: string | null;
  createdAt: string;
  createdBy?: string | null;
  cargaHorariaPresencial: number;
  cargaHorariaRemota: number;
  servidores: EscalaServidor[];
}

export interface EscalaCalendario {
  escalaId: string;
  ano: number;
  mes: number;
  dataInicio: string;
  dataFim: string;
  servidores: {
    escalaServidorId: string;
    servidorId: string;
    servidorNome: string;
    matricula: string;
    cargoNome: string;
    cargoCodigo: string;
    ocorrencias: EscalaOcorrencia[];
  }[];
}

export interface TipoOcorrencia {
  codigo: string;
  nome: string;
  horasPadrao?: number | null;
  categoria: CategoriaOcorrencia;
  ativo: boolean;
}

export interface PadraoEscala {
  id: string;
  codigo: string;
  nome: string;
  tipoFuncionamento: TipoFuncionamento;
  tipoJornada: TipoJornada;
  recorrenciaTipo: RecorrenciaTipo;
  diasTrabalho?: number | null;
  diasFolga?: number | null;
  diasSemana?: string | null;
  tipoOcorrenciaTrabalho: string;
  tipoOcorrenciaFolga: string;
  sequenciaCiclo?: string | null;
  horaInicioPadrao?: string | null;
  horaFimPadrao?: string | null;
  horasPadrao?: number | null;
  sistema: boolean;
  ativo: boolean;
}

export interface EscalaAnteriorInfo {
  id: string;
  ano: number;
  mes: number;
  identificacao: string;
  tipoFuncionamento: TipoFuncionamento;
  status: StatusEscala;
  quantidadeServidores: number;
}

export interface EscalaCoberturaDia {
  data: string;
  servidoresTrabalho: string[];
  temCobertura: boolean;
}

export interface EscalaCobertura {
  escalaId: string;
  tipoFuncionamento: TipoFuncionamento;
  dias: EscalaCoberturaDia[];
}

export interface EscalaConflito {
  tipo: string;
  critico: boolean;
  servidorId?: string | null;
  servidorNome?: string | null;
  data?: string | null;
  mensagem: string;
}

export interface EscalaConflitos {
  escalaId: string;
  itens: EscalaConflito[];
  totalCriticos: number;
}

/** Servidor já escalado em outro lugar no mesmo período — `origem` identifica onde (setor,
 * núcleo, ou rodízio de escala resumida). */
export interface ConflitoServidor {
  servidorId: string;
  servidorNome: string;
  origem: string;
}

export interface CreateEscalaPayload {
  setorId?: string | null;
  nucleoId?: string | null;
  ano: number;
  mes: number;
  tipoFuncionamento: TipoFuncionamento;
  observacao?: string | null;
}

export interface UpdateEscalaPayload {
  ano: number;
  mes: number;
  tipoFuncionamento: TipoFuncionamento;
  observacao?: string | null;
}

export interface CreateEscalaJornadaPayload {
  tipoJornada: TipoJornada;
  dataInicio: string;
  dataFim: string;
  tipoOcorrenciaCodigo: string;
  recorrenciaTipo: RecorrenciaTipo;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  diasSemana?: string | null;
  intervaloDias?: number | null;
  diasTrabalho?: number | null;
  diasFolga?: number | null;
  tipoOcorrenciaFolgaCodigo?: string | null;
  sequenciaCiclo?: string | null;
  observacao?: string | null;
  padraoEscalaId?: string | null;
  dataInicioCiclo?: string | null;
}

export interface GerarEscalaItemPayload {
  servidorId: string;
  padraoEscalaId: string;
  dataInicioCiclo: string;
  horaInicio?: string | null;
  horaFim?: string | null;
}

export interface GerarEscalaPayload {
  itens: GerarEscalaItemPayload[];
  distribuirAutomaticamente: boolean;
  dataBaseDistribuicao?: string | null;
}

export interface UpsertOcorrenciaPayload {
  data: string;
  tipoOcorrenciaCodigo: string;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  observacao?: string | null;
}

export interface UpsertOcorrenciasLotePayload {
  servidorIds: string[];
  dataInicio: string;
  dataFim: string;
  tipoOcorrenciaCodigo: string;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  observacao?: string | null;
  confirmarSobrescrita: boolean;
}

export interface SyncOcorrenciaItemPayload {
  servidorId: string;
  data: string;
  tipoOcorrenciaCodigo: string;
  horaInicio?: string | null;
  horaFim?: string | null;
  horas?: number | null;
  observacao?: string | null;
}

export interface SyncOcorrenciasPayload {
  itens: SyncOcorrenciaItemPayload[];
}

export interface CopiarEscalaPayload {
  ano: number;
  mes: number;
  sobrescreverManuais?: boolean;
}

export interface SolicitacaoDevolucaoEscala {
  id: string;
  escalaId: string;
  escalaIdentificacao: string;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  ano: number;
  mes: number;
  solicitanteUsuarioId: string;
  solicitanteNome: string;
  justificativa: string;
  status: StatusSolicitacaoDevolucao;
  respondidoPor?: string | null;
  respostaEm?: string | null;
  observacaoResposta?: string | null;
  createdAt: string;
}

export interface PagedEscalas {
  items: EscalaListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
