export interface UsuarioListItem {
  id: string;
  login: string;
  nomeServidor: string;
  matricula: string;
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
  email: string;
  ativo: boolean;
  ultimoLogin?: string | null;
  perfilIds: string[];
  perfis: string[];
}

export interface CreateUsuarioPayload {
  servidorId: string;
  login: string;
  senha: string;
  perfilIds: string[];
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

export interface PerfilDetail extends PerfilListItem {
  permissaoIds: string[];
  permissoes: string[];
}

export interface CreatePerfilPayload {
  nome: string;
  codigo: string;
  descricao?: string | null;
  permissaoIds?: string[];
}

export interface UpdatePerfilPayload {
  nome: string;
  descricao?: string | null;
  ativo?: boolean | null;
}

export interface PerfilExclusaoImpacto {
  quantidadeUsuarios: number;
  requerSubstituto: boolean;
}

export interface DesativarPerfilPayload {
  perfilSubstitutoId?: string | null;
}

export interface PermissaoItem {
  id: string;
  codigo: string;
  nome: string;
  descricao?: string | null;
  modulo: string;
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
  email: string;
  telefone?: string | null;
  dataNascimento: string;
  setorId: string;
  setorNome: string;
  possuiUsuario: boolean;
  status: StatusServidor;
}

export interface CreateServidorPayload {
  nome: string;
  matricula: string;
  cpf: string;
  cargoId: string;
  email: string;
  setorId: string;
  dataNascimento: string;
  telefone?: string | null;
  status?: StatusServidor | null;
}

export interface UpdateServidorPayload {
  nome: string;
  matricula: string;
  cpf: string;
  cargoId: string;
  email: string;
  setorId: string;
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

export interface CreateSetorPayload {
  nome: string;
  sigla: string;
  resumo?: string | null;
  nucleoId?: string | null;
  chefias: SetorChefiaInput[];
}

export interface UpdateSetorPayload {
  nome: string;
  sigla: string;
  resumo?: string | null;
  nucleoId?: string | null;
  chefias: SetorChefiaInput[];
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
