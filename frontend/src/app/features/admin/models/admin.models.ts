export interface UsuarioListItem {
  id: string;
  login: string;
  nomeServidor: string;
  matricula: string;
  cpf: string;
  ativo: boolean;
  ultimoLogin?: string | null;
  perfis: string[];
}

export interface UsuarioDetail {
  id: string;
  servidorId: string;
  login: string;
  nomeServidor: string;
  matricula: string;
  email?: string | null;
  ativo: boolean;
  ultimoLogin?: string | null;
  perfilIds: string[];
  perfis: string[];
}

export interface UsuarioComSenha extends UsuarioDetail {
  senhaTemporaria: string;
}

export interface CreateUsuarioPayload {
  servidorId: string;
  perfilIds: string[];
}

export interface ResetSenhaResult {
  id: string;
  login: string;
  nomeServidor: string;
  senhaTemporaria: string;
}

export interface UpdateUsuarioPayload {
  perfilIds?: string[];
  ativo?: boolean | null;
}

export interface PerfilListItem {
  id: string;
  nome: string;
  codigo: string;
  descricao?: string | null;
  sistema: boolean;
  ativo: boolean;
  quantidadePermissoes: number;
}

export type AbrangenciaModulo = 'MeusSetores' | 'TodosOsSetores' | 1 | 2;

export interface PerfilDetail extends PerfilListItem {
  permissaoIds: string[];
  permissoes: string[];
  /** Abrangência por código de permissão. */
  abrangenciaPorPermissao?: Record<string, AbrangenciaModulo>;
  /** Áreas liberadas (Gestão do Setor / Gestão Institucional / Administração). */
  areas?: string[];
}

export interface CreatePerfilPayload {
  nome: string;
  codigo?: string | null;
  descricao?: string | null;
  permissaoIds?: string[];
  abrangenciaPorPermissao?: Record<string, AbrangenciaModulo>;
  areas?: string[];
}

export interface SetPerfilPermissoesPayload {
  permissaoIds?: string[];
  abrangenciaPorPermissao?: Record<string, AbrangenciaModulo>;
  areas?: string[];
}

export interface UpdatePerfilPayload {
  nome: string;
  descricao?: string | null;
  ativo?: boolean | null;
}

export interface PerfilExclusaoImpacto {
  quantidadeUsuarios: number;
  temUsuariosVinculados: boolean;
}

export interface ServidorExclusaoImpacto {
  escalas: number;
  afastamentos: number;
  chefias: number;
  usuarios: number;
  nucleosComoChefe: number;
  podeExcluir: boolean;
}

export interface DesativarPerfilPayload {
  perfilSubstitutoId?: string | null;
  removerVinculosSemSubstituto?: boolean;
}

export interface PermissaoItem {
  id: string;
  codigo: string;
  nome: string;
  descricao?: string | null;
  modulo: string;
  area: string;
  sistema: boolean;
  ativo: boolean;
}

export type StatusServidor = 'Ativo' | 'Afastado' | 'Cedido';

export interface ServidorListItem {
  id: string;
  nome: string;
  matricula: string;
  cpf: string;
  cargoId: string;
  cargo: string;
  cargoCodigo: string;
  email?: string | null;
  telefone?: string | null;
  dataNascimento: string;
  setorId?: string | null;
  setorNome?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  possuiUsuario: boolean;
  usuarioAtivo: boolean;
  status: StatusServidor;
}

/** Informe setorId (lotação num setor) ou nucleoId (lotação direta no núcleo — servidor
 * que atua em todos os setores do núcleo; não implica chefia, que é definida à parte
 * em Setor.SetorChefia / Nucleo.ChefeServidorId), nunca os dois nem nenhum. */
export interface CreateServidorPayload {
  nome: string;
  matricula: string;
  cpf: string;
  cargoId: string;
  email?: string | null;
  setorId?: string | null;
  nucleoId?: string | null;
  dataNascimento: string;
  telefone?: string | null;
  status?: StatusServidor | null;
}

export interface UpdateServidorPayload {
  nome: string;
  matricula: string;
  cpf: string;
  cargoId: string;
  email?: string | null;
  setorId?: string | null;
  nucleoId?: string | null;
  dataNascimento: string;
  telefone?: string | null;
  status: StatusServidor;
}

export type TipoChefia =
  | 'ChefiaImediata'
  | 'ChefiaSubstituta'
  | 'Diretor'
  | 'Subcoordenador';

export interface SetorChefia {
  tipoChefia: TipoChefia;
  servidorId: string;
  servidorNome?: string | null;
}

export interface SetorChefiaInput {
  tipoChefia: TipoChefia;
  servidorId: string;
}

export interface SetorListItem {
  id: string;
  nome: string;
  sigla: string;
  resumo?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  isDirecaoIc: boolean;
  chefias: SetorChefia[];
}

export interface ChefiaConflito {
  servidorId: string;
  servidorNome: string;
  tipoChefia: TipoChefia;
  setorId: string;
  setorNome: string;
}

export interface CreateSetorPayload {
  nome: string;
  sigla: string;
  resumo?: string | null;
  nucleoId?: string | null;
  chefias: SetorChefiaInput[];
  confirmarRemocaoChefiasEmOutrosSetores?: boolean;
}

export interface UpdateSetorPayload {
  nome: string;
  sigla: string;
  resumo?: string | null;
  nucleoId?: string | null;
  chefias: SetorChefiaInput[];
  confirmarRemocaoChefiasEmOutrosSetores?: boolean;
}

export interface NucleoListItem {
  id: string;
  nome: string;
  sigla: string;
  chefeServidorId?: string | null;
  chefeNome?: string | null;
  quantidadeSetores: number;
}

export interface NucleoDetail {
  id: string;
  nome: string;
  sigla: string;
  chefeServidorId?: string | null;
  chefeNome?: string | null;
  setorIds: string[];
}

export interface CreateNucleoPayload {
  nome: string;
  sigla: string;
  chefeServidorId?: string | null;
}

export interface UpdateNucleoPayload {
  nome: string;
  sigla: string;
  chefeServidorId?: string | null;
}

export interface NucleoComSetores {
  id: string;
  nome: string;
  sigla: string;
  chefeServidorId?: string | null;
  chefeNome?: string | null;
  setores: SetorListItem[];
}

export interface EstruturaOrganizacional {
  nucleos: NucleoComSetores[];
  direcaoIc?: SetorListItem | null;
}

export interface CargoListItem {
  id: string;
  nome: string;
  codigo: string;
  ativo: boolean;
}

export const PAGE_SIZE_OPTIONS = [30, 50, 100] as const;
export type PageSizeOption = (typeof PAGE_SIZE_OPTIONS)[number];
export const DEFAULT_PAGE_SIZE: PageSizeOption = 50;

export interface PaginationQuery {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
