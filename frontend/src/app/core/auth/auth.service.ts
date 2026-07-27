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
        tap((response) => this.persistFromResponse(response)),
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

  isSuperAdmin(): boolean {
    return this.currentUserSignal()?.perfis.includes('SUPERADMINISTRADOR') ?? false;
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
    return !setorId || user.setoresGerenciadosIds.includes(setorId);
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

  private persistFromResponse(response: LoginResponse): void {
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
      setoresGerenciadosIds: response.usuario.setoresGerenciadosIds ?? [],
      deveAlterarSenha: response.usuario.deveAlterarSenha === true,
      meta: response.usuario.setorLotacaoNome ?? (response.usuario.perfis ?? [])[0] ?? 'SIGAD-IC',
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
        setoresGerenciadosIds: session.user.setoresGerenciadosIds ?? [],
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
    } catch {
      this.logout();
    }
  }
}
