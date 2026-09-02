import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import {
  anyPermissionGuard,
  authGuard,
  guestGuard,
  mustChangePasswordGuard,
  passwordOkGuard,
  permissionGuard,
  setupGuard,
  superAdminGuard,
} from './auth.guard';
import { AuthService } from './auth.service';
import { SetupApiService } from './setup-api.service';

function setup(authPartial: Partial<AuthService>, setupApiPartial?: Partial<SetupApiService>) {
  const createUrlTree = vi.fn((commands: unknown[]) => ({ commands }) as unknown as UrlTree);
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthService, useValue: authPartial },
      { provide: Router, useValue: { createUrlTree } },
      ...(setupApiPartial ? [{ provide: SetupApiService, useValue: setupApiPartial }] : []),
    ],
  });
  return { createUrlTree };
}

function runAsyncGuard(guardCall: () => unknown): Promise<boolean | UrlTree> {
  return firstValueFrom(
    TestBed.runInInjectionContext(guardCall) as Observable<boolean | UrlTree>,
  );
}

describe('auth guards', () => {
  it('authGuard libera autenticado e redireciona anonimo para login', () => {
    setup({ isAuthenticated: signal(true).asReadonly() } as Partial<AuthService>);
    expect(TestBed.runInInjectionContext(() => authGuard({} as never, {} as never))).toBe(true);

    const { createUrlTree } = setup({
      isAuthenticated: signal(false).asReadonly(),
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(createUrlTree).toHaveBeenCalledWith(['/login']);
  });

  it('guestGuard redireciona autenticado; prioriza trocar-senha quando obrigatorio', () => {
    const { createUrlTree } = setup({
      isAuthenticated: signal(true).asReadonly(),
      deveAlterarSenha: signal(true).asReadonly(),
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
    expect(createUrlTree).toHaveBeenCalledWith(['/trocar-senha']);

    const home = setup({
      isAuthenticated: signal(true).asReadonly(),
      deveAlterarSenha: signal(false).asReadonly(),
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
    expect(home.createUrlTree).toHaveBeenCalledWith(['/']);
  });

  it('mustChangePasswordGuard so libera autenticado com troca obrigatoria', () => {
    setup({
      isAuthenticated: signal(true).asReadonly(),
      deveAlterarSenha: signal(true).asReadonly(),
    } as Partial<AuthService>);
    expect(
      TestBed.runInInjectionContext(() => mustChangePasswordGuard({} as never, {} as never)),
    ).toBe(true);

    const { createUrlTree } = setup({
      isAuthenticated: signal(true).asReadonly(),
      deveAlterarSenha: signal(false).asReadonly(),
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => mustChangePasswordGuard({} as never, {} as never));
    expect(createUrlTree).toHaveBeenCalledWith(['/']);
  });

  it('passwordOkGuard bloqueia quem ainda deve trocar senha', () => {
    const { createUrlTree } = setup({
      isAuthenticated: signal(true).asReadonly(),
      deveAlterarSenha: signal(true).asReadonly(),
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => passwordOkGuard({} as never, {} as never));
    expect(createUrlTree).toHaveBeenCalledWith(['/trocar-senha']);
  });

  it('superAdminGuard exige perfil superadministrador', () => {
    setup({
      isAuthenticated: signal(true).asReadonly(),
      isSuperAdmin: () => true,
    } as Partial<AuthService>);
    expect(TestBed.runInInjectionContext(() => superAdminGuard({} as never, {} as never))).toBe(
      true,
    );

    const { createUrlTree } = setup({
      isAuthenticated: signal(true).asReadonly(),
      isSuperAdmin: () => false,
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() => superAdminGuard({} as never, {} as never));
    expect(createUrlTree).toHaveBeenCalledWith(['/']);
  });

  it('permissionGuard libera apenas por permissao (sem bypass de superadmin)', () => {
    setup({
      isAuthenticated: signal(true).asReadonly(),
      hasPermission: (code: string) => code === 'escalas.listar',
    } as Partial<AuthService>);
    expect(
      TestBed.runInInjectionContext(() =>
        permissionGuard('escalas.listar')({} as never, {} as never),
      ),
    ).toBe(true);

    const { createUrlTree } = setup({
      isAuthenticated: signal(true).asReadonly(),
      isSuperAdmin: () => true,
      hasPermission: () => false,
    } as Partial<AuthService>);
    TestBed.runInInjectionContext(() =>
      permissionGuard('qualquer.codigo')({} as never, {} as never),
    );
    expect(createUrlTree).toHaveBeenCalledWith(['/']);
  });

  it('anyPermissionGuard libera se qualquer codigo bater', () => {
    setup({
      isAuthenticated: signal(true).asReadonly(),
      isSuperAdmin: () => false,
      hasPermission: (code: string) => code === 'servidores.listar',
    } as Partial<AuthService>);
    expect(
      TestBed.runInInjectionContext(() =>
        anyPermissionGuard('nucleos.listar', 'servidores.listar')({} as never, {} as never),
      ),
    ).toBe(true);
  });

  it('guestGuard libera /login quando nao autenticado e setup nao esta pendente', async () => {
    const { createUrlTree } = setup(
      { isAuthenticated: signal(false).asReadonly() } as Partial<AuthService>,
      { status: () => of({ needsSetup: false }) } as Partial<SetupApiService>,
    );

    const result = await runAsyncGuard(() => guestGuard({} as never, {} as never));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('guestGuard redireciona para /setup quando nao autenticado e setup esta pendente', async () => {
    setup(
      { isAuthenticated: signal(false).asReadonly() } as Partial<AuthService>,
      { status: () => of({ needsSetup: true }) } as Partial<SetupApiService>,
    );

    const result = await runAsyncGuard(() => guestGuard({} as never, {} as never));

    expect(result).toEqual({ commands: ['/setup'] });
  });

  it('setupGuard libera quando ha setup pendente e bloqueia quando ja foi concluido', async () => {
    setup({} as Partial<AuthService>, { status: () => of({ needsSetup: true }) } as Partial<SetupApiService>);
    const liberado = await runAsyncGuard(() => setupGuard({} as never, {} as never));
    expect(liberado).toBe(true);

    setup({} as Partial<AuthService>, { status: () => of({ needsSetup: false }) } as Partial<SetupApiService>);
    const bloqueado = await runAsyncGuard(() => setupGuard({} as never, {} as never));
    expect(bloqueado).toEqual({ commands: ['/login'] });
  });
});
