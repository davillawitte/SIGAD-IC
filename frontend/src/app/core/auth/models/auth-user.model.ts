export interface AuthUser {
  id: string;
  login: string;
  displayName: string;
  email?: string | null;
  perfis: string[];
  permissoes: string[];
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
  };
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthUser;
}
