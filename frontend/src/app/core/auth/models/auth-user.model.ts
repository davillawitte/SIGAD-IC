export type Abrangencia = 'MeusSetores' | 'TodosOsSetores' | 1 | 2;

export interface PerfilAuthDetalhe {
  codigo: string;
  permissoes: string[];
  /** Abrangência por código de permissão (ex.: escalas.listar). */
  abrangenciaPorPermissao: Record<string, Abrangencia>;
  /** @deprecated prefer abrangenciaPorPermissao */
  abrangenciaPorModulo?: Record<string, Abrangencia>;
}

export interface AuthUser {
  id: string;
  login: string;
  displayName: string;
  email?: string | null;
  perfis: string[];
  permissoes: string[];
  perfisDetalhe?: PerfilAuthDetalhe[];
  servidorId: string;
  setorLotacaoId?: string | null;
  setorLotacaoNome?: string | null;
  setoresGerenciadosIds: string[];
  deveAlterarSenha: boolean;
  meta: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  usuario: {
    id: string;
    login: string;
    nome: string;
    email?: string | null;
    perfis: string[];
    permissoes: string[];
    perfisDetalhe?: PerfilAuthDetalhe[];
    servidorId: string;
    setorLotacaoId?: string | null;
    setorLotacaoNome?: string | null;
    setoresGerenciadosIds: string[];
    deveAlterarSenha: boolean;
  };
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthUser;
}
