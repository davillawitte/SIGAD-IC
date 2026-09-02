export type Abrangencia = 'MeusSetores' | 'TodosOsSetores' | 1 | 2;

export interface PerfilAuthDetalhe {
  codigo: string;
  permissoes: string[];
  /** Abrangência por código de permissão (ex.: escalas.listar). */
  abrangenciaPorPermissao: Record<string, Abrangencia>;
  /** @deprecated prefer abrangenciaPorPermissao */
  abrangenciaPorModulo?: Record<string, Abrangencia>;
}

export type TipoChefia = 'ChefiaImediata' | 'ChefiaSubstituta' | 'Diretor' | 'Subcoordenador';

/** Resumo (id + sigla + tipo) de um setor/núcleo que o usuário chefia — só para exibição
 * (ex.: "Chefe do X" no menu; no setor Direção IC, `tipoChefia` distingue Diretor(a) de
 * Subcoordenador(a)). Núcleos sempre vêm com `ChefiaImediata` (chefia única, sem o conceito). */
export interface ChefiaResumo {
  id: string;
  sigla: string;
  tipoChefia: TipoChefia;
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
  nucleoLotacaoId?: string | null;
  nucleoLotacaoNome?: string | null;
  setoresGerenciadosIds: string[];
  nucleosGerenciadosIds: string[];
  setoresDosNucleosGerenciadosIds: string[];
  setoresGeridos?: ChefiaResumo[];
  nucleosGeridos?: ChefiaResumo[];
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
    nucleoLotacaoId?: string | null;
    nucleoLotacaoNome?: string | null;
    setoresGerenciadosIds: string[];
    nucleosGerenciadosIds: string[];
    setoresDosNucleosGerenciadosIds: string[];
    setoresGeridos?: ChefiaResumo[];
    nucleosGeridos?: ChefiaResumo[];
    deveAlterarSenha: boolean;
  };
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthUser;
}
