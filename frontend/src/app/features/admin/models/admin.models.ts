export interface UsuarioListItem {
  id: string;
  login: string;
  nomeServidor: string;
  matricula: string;
  bloqueado: boolean;
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
  bloqueado: boolean;
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
  bloqueado?: boolean | null;
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

export interface PermissaoItem {
  id: string;
  codigo: string;
  nome: string;
  descricao?: string | null;
  modulo: string;
  sistema: boolean;
  ativo: boolean;
}

export interface CreatePermissaoPayload {
  codigo: string;
  nome: string;
  modulo: string;
  descricao?: string | null;
}

export interface UpdatePermissaoPayload {
  nome: string;
  modulo: string;
  descricao?: string | null;
  ativo?: boolean | null;
}

export interface ServidorListItem {
  id: string;
  nome: string;
  matricula: string;
  cpf: string;
  cargo: string;
  email: string;
  telefone?: string | null;
  setorId: string;
  setorNome: string;
  possuiUsuario: boolean;
  ativo: boolean;
}

export interface CreateServidorPayload {
  nome: string;
  matricula: string;
  cpf: string;
  cargo: string;
  email: string;
  setorId: string;
  telefone?: string | null;
}

export interface SetorListItem {
  id: string;
  nome: string;
  sigla: string;
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
