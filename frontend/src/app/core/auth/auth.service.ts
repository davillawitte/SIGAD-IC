import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthSession, AuthUser, LoginResponse } from './models/auth-user.model';

const SESSION_KEY = 'sigad-ic.auth.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserSignal = signal<AuthUser | null>(null);
  private accessToken: string | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

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

  logout(): void {
    localStorage.removeItem(SESSION_KEY);
    this.accessToken = null;
    this.currentUserSignal.set(null);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  hasPermission(code: string): boolean {
    const user = this.currentUserSignal();
    return !!user?.permissoes.includes(code);
  }

  isSuperAdmin(): boolean {
    return this.currentUserSignal()?.perfis.includes('SUPERADMINISTRADOR') ?? false;
  }

  private persistFromResponse(response: LoginResponse): void {
    const user: AuthUser = {
      id: response.usuario.id,
      login: response.usuario.login,
      displayName: response.usuario.nome,
      email: response.usuario.email,
      perfis: response.usuario.perfis ?? [],
      permissoes: response.usuario.permissoes ?? [],
      meta: (response.usuario.perfis ?? [])[0] ?? 'SIGAD-IC',
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
      this.currentUserSignal.set(session.user);
    } catch {
      this.logout();
    }
  }
}
