import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Abrangencia,
  AuthSession,
  AuthUser,
  LoginResponse,
  PerfilAuthDetalhe,
} from './models/auth-user.model';

const SESSION_KEY = 'sigad-ic.auth.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserSignal = signal<AuthUser | null>(null);
  private accessToken: string | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly deveAlterarSenha = computed(() => this.currentUserSignal()?.deveAlterarSenha === true);

  constructor() {
    this.restoreSession();
  }

  login(username: string, password: string): Observable<{ ok: true } | { ok: false; message: string }> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, {
        login: username.trim(),
        senha: password,
      })
      .pipe(
        tap((response) => this.persistSession(response)),
        map(() => ({ ok: true as const })),
        catchError((error: { error?: { message?: string }; status?: number }) => {
          const message =
            error?.error?.message ??
            (error?.status === 0
              ? 'Não foi possível conectar à API.'
              : 'Usuário ou senha inválidos.');
          return of({ ok: false as const, message });
        }),
      );
  }

  /** Reemite JWT e claims após alteração de perfis/permissões. */
  refreshSession(): Observable<boolean> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/refresh`, {}).pipe(
      tap((response) => this.persistSession(response)),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  alterarSenha(
    senhaAtual: string,
    novaSenha: string,
  ): Observable<{ ok: true } | { ok: false; message: string }> {
    return this.http
      .post<void>(`${environment.apiUrl}/api/auth/alterar-senha`, {
        senhaAtual,
        novaSenha,
      })
      .pipe(
        tap(() => this.clearDeveAlterarSenha()),
        map(() => ({ ok: true as const })),
        catchError((error: { error?: { message?: string; errors?: string[] } }) => {
          const message =
            error?.error?.message ??
            error?.error?.errors?.[0] ??
            'Não foi possível alterar a senha.';
          return of({ ok: false as const, message });
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(SESSION_KEY);
    this.accessToken = null;
    this.currentUserSignal.set(null);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  /** Gate grosso (união de permissões) — usar em route guards. Sem bypass de SuperAdmin. */
  hasPermission(code: string): boolean {
    const user = this.currentUserSignal();
    return !!user?.permissoes.includes(code);
  }

  hasAnyPermission(codes: string[]): boolean {
    return codes.some((code) => this.hasPermission(code));
  }

  /**
   * Avaliação por perfil: permissão + abrangência daquela permissão no mesmo perfil.
   * SuperAdmin não bypassa escopo de setor.
   */
  canAccess(permissao: string, setorId?: string | null): boolean {
    const user = this.currentUserSignal();
    if (!user) {
      return false;
    }

    const detalhes = user.perfisDetalhe ?? [];

    if (detalhes.length === 0) {
      if (!user.permissoes.includes(permissao)) {
        return false;
      }
      return this.abrangerMeusSetores(user, setorId);
    }

    return detalhes.some(
      (perfil) =>
        perfil.permissoes.includes(permissao) &&
        this.abrangerPermissao(perfil, permissao, setorId, user),
    );
  }

  /**
   * Variante de `canAccess` ciente de setor OU núcleo — uma escala pode ter dono setor ou
   * núcleo. `canAccess(permissao, undefined)` trata "sem setor" como sempre permitido (é um
   * escape hatch pra checks de módulo sem recurso específico); aqui isso não vale — só libera
   * quando o recurso realmente é de núcleo e o ator o gerencia (ou tem visão global).
   */
  canAccessEscala(permissao: string, setorId?: string | null, nucleoId?: string | null): boolean {
    if (setorId) {
      return this.canAccess(permissao, setorId);
    }
    if (nucleoId) {
      return this.isChefeNucleo(nucleoId) || (this.hasVisaoGlobal('escalas') && this.hasPermission(permissao));
    }
    return false;
  }

  /** Indica se o usuário pode mutar recursos do setor (perfil + abrangência). */
  canManageSetor(setorId: string): boolean {
    return (
      this.canAccess('escalas.editar', setorId) ||
      this.canAccess('escalas.criar', setorId) ||
      this.canAccess('afastamentos.criar', setorId) ||
      this.canAccess('afastamentos.editar', setorId) ||
      this.canAccess('servidores.editar', setorId)
    );
  }

  /** Chefe do núcleo informado (ou de algum núcleo, se omitido) — habilita a seção de outros
   * setores do núcleo dentro da escala resumida. */
  isChefeNucleo(nucleoId?: string): boolean {
    const ids = this.currentUserSignal()?.nucleosGerenciadosIds ?? [];
    return nucleoId ? ids.includes(nucleoId) : ids.length > 0;
  }

  /** Chefe do setor informado (ou de algum setor, se omitido) — habilita o passo opcional de
   * escala resumida pra qualquer chefe de setor, não só chefes de núcleo. */
  isChefeSetor(setorId?: string): boolean {
    const ids = this.currentUserSignal()?.setoresGerenciadosIds ?? [];
    return setorId ? ids.includes(setorId) : ids.length > 0;
  }

  /** Chefia de verdade (setor direto, ou núcleo que engloba o setor, ou o próprio núcleo) — ao
   * contrário de `canAccessEscala`, NÃO aceita visão institucional/abrangência ampla como
   * substituto de chefia. Usado só onde isso não deve bastar (ex.: excluir a escala de um setor
   * que o ator não chefia de fato — espelha `EscalaService.IsChefiaDireta` no backend). */
  isChefiaDireta(setorId?: string | null, nucleoId?: string | null): boolean {
    const user = this.currentUserSignal();
    if (!user) return false;
    if (setorId) {
      return (
        user.setoresGerenciadosIds.includes(setorId) ||
        user.setoresDosNucleosGerenciadosIds.includes(setorId)
      );
    }
    if (nucleoId) {
      return user.nucleosGerenciadosIds.includes(nucleoId);
    }
    return false;
  }

  isSuperAdmin(): boolean {
    return this.currentUserSignal()?.perfis.includes('SUPERADMINISTRADOR') ?? false;
  }

  /** Visão global do módulo (ex.: escalas.listar com TodosOsSetores). */
  hasVisaoGlobal(modulo: string): boolean {
    const user = this.currentUserSignal();
    if (!user) {
      return false;
    }

    const listar = `${modulo}.listar`;
    const detalhes = user.perfisDetalhe ?? [];
    if (detalhes.length === 0) {
      return false;
    }

    return detalhes.some(
      (perfil) =>
        perfil.permissoes.includes(listar) &&
        this.resolveAbrangenciaPermissao(perfil, listar) === 'TodosOsSetores',
    );
  }

  /** Área Gestão Institucional (visão de todos + estrutura/servidores/devoluções). */
  hasGestaoInstitucional(): boolean {
    return (
      this.hasVisaoGlobal('escalas') ||
      this.hasVisaoGlobal('afastamentos') ||
      this.hasVisaoGlobal('servidores') ||
      this.hasVisaoGlobal('setores') ||
      this.hasVisaoGlobal('nucleos') ||
      this.hasPermission('escalas.devolver')
    );
  }

  private abrangerPermissao(
    perfil: PerfilAuthDetalhe,
    permissao: string,
    setorId: string | null | undefined,
    user: AuthUser,
  ): boolean {
    if (this.resolveAbrangenciaPermissao(perfil, permissao) === 'TodosOsSetores') {
      return true;
    }
    return this.abrangerMeusSetores(user, setorId);
  }

  private abrangerMeusSetores(user: AuthUser, setorId: string | null | undefined): boolean {
    return (
      !setorId ||
      user.setoresGerenciadosIds.includes(setorId) ||
      user.setoresDosNucleosGerenciadosIds.includes(setorId)
    );
  }

  private resolveAbrangenciaPermissao(
    perfil: PerfilAuthDetalhe,
    permissao: string,
  ): 'MeusSetores' | 'TodosOsSetores' {
    const map = perfil.abrangenciaPorPermissao ?? {};
    const raw =
      map[permissao] ??
      map[permissao.toLowerCase()] ??
      Object.entries(map).find(([key]) => key.toLowerCase() === permissao.toLowerCase())?.[1];

    return this.normalizeAbrangencia(raw);
  }

  private normalizeAbrangencia(value: Abrangencia | undefined): 'MeusSetores' | 'TodosOsSetores' {
    if (value === 'TodosOsSetores' || value === 2) {
      return 'TodosOsSetores';
    }
    return 'MeusSetores';
  }

  private moduloDe(permissionCode: string): string {
    const sep = permissionCode.indexOf('.');
    return (sep > 0 ? permissionCode.slice(0, sep) : permissionCode).toLowerCase();
  }

  private clearDeveAlterarSenha(): void {
    const user = this.currentUserSignal();
    if (!user) return;

    const updated = { ...user, deveAlterarSenha: false };
    this.currentUserSignal.set(updated);

    try {
      const raw = localStorage.getItem(SESSION_KEY);
      if (!raw) return;
      const session = JSON.parse(raw) as AuthSession;
      session.user = updated;
      localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    } catch {
      // ignore persistence errors
    }
  }

  /** Grava a sessão (localStorage + signal) a partir de uma resposta de login — reaproveitada
   * pelo fluxo de setup, que loga automaticamente o superadministrador recém-criado. */
  persistSession(response: LoginResponse): void {
    const user: AuthUser = {
      id: response.usuario.id,
      login: response.usuario.login,
      displayName: response.usuario.nome,
      email: response.usuario.email,
      perfis: response.usuario.perfis ?? [],
      permissoes: response.usuario.permissoes ?? [],
      perfisDetalhe: (response.usuario.perfisDetalhe ?? []).map((p) => ({
        codigo: p.codigo,
        permissoes: p.permissoes ?? [],
        abrangenciaPorPermissao: p.abrangenciaPorPermissao ?? {},
      })),
      servidorId: response.usuario.servidorId,
      setorLotacaoId: response.usuario.setorLotacaoId ?? null,
      setorLotacaoNome: response.usuario.setorLotacaoNome ?? null,
      nucleoLotacaoId: response.usuario.nucleoLotacaoId ?? null,
      nucleoLotacaoNome: response.usuario.nucleoLotacaoNome ?? null,
      setoresGerenciadosIds: response.usuario.setoresGerenciadosIds ?? [],
      nucleosGerenciadosIds: response.usuario.nucleosGerenciadosIds ?? [],
      setoresDosNucleosGerenciadosIds: response.usuario.setoresDosNucleosGerenciadosIds ?? [],
      setoresGeridos: response.usuario.setoresGeridos ?? [],
      nucleosGeridos: response.usuario.nucleosGeridos ?? [],
      deveAlterarSenha: response.usuario.deveAlterarSenha === true,
      meta:
        response.usuario.setorLotacaoNome ??
        response.usuario.nucleoLotacaoNome ??
        (response.usuario.perfis ?? [])[0] ??
        'SIGAD-IC',
    };

    const session: AuthSession = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user,
    };

    localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    this.accessToken = session.accessToken;
    this.currentUserSignal.set(user);
  }

  private restoreSession(): void {
    try {
      const raw = localStorage.getItem(SESSION_KEY);
      if (!raw) {
        return;
      }

      const session = JSON.parse(raw) as AuthSession;
      if (!session?.accessToken || !session.user?.login) {
        this.logout();
        return;
      }

      if (session.expiresAtUtc && new Date(session.expiresAtUtc).getTime() <= Date.now()) {
        this.logout();
        return;
      }

      this.accessToken = session.accessToken;
      this.currentUserSignal.set({
        ...session.user,
        servidorId: session.user.servidorId ?? '',
        setorLotacaoId: session.user.setorLotacaoId ?? null,
        setorLotacaoNome: session.user.setorLotacaoNome ?? null,
        nucleoLotacaoId: session.user.nucleoLotacaoId ?? null,
        nucleoLotacaoNome: session.user.nucleoLotacaoNome ?? null,
        setoresGerenciadosIds: session.user.setoresGerenciadosIds ?? [],
        nucleosGerenciadosIds: session.user.nucleosGerenciadosIds ?? [],
        setoresDosNucleosGerenciadosIds: session.user.setoresDosNucleosGerenciadosIds ?? [],
        permissoes: session.user.permissoes ?? [],
        perfis: session.user.perfis ?? [],
        perfisDetalhe: (session.user.perfisDetalhe ?? []).map((p) => ({
          codigo: p.codigo,
          permissoes: p.permissoes ?? [],
          abrangenciaPorPermissao:
            p.abrangenciaPorPermissao ??
            (p as { abrangenciaPorModulo?: Record<string, Abrangencia> }).abrangenciaPorModulo ??
            {},
        })),
        deveAlterarSenha: session.user.deveAlterarSenha === true,
        meta:
          session.user.setorLotacaoNome ??
          session.user.meta ??
          session.user.perfis?.[0] ??
          'SIGAD-IC',
      });

      // A sessão só é gravada de novo no login ou quando o próprio usuário edita o perfil
      // dele (ver perfil-form.ts) — se um chefia/permissão mudou por outra via (outro perfil
      // editado, seed do banco, etc.), quem já estava logado ficava com permissões/abrangência
      // desatualizadas até deslogar e logar de novo manualmente. Reemitir em segundo plano a
      // cada carregamento do app corrige isso sem exigir relogin — falha aqui é inofensiva
      // (mantém a sessão restaurada), então não precisa bloquear nem tratar erro.
      this.refreshSession().subscribe();
    } catch {
      this.logout();
    }
  }
}
