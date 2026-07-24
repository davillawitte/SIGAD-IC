export interface AuthUser {
  id: string;
  login: string;
  displayName: string;
  email?: string | null;
  perfis: string[];
  permissoes: string[];
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
