import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import {
  anyPermissionGuard,
  authGuard,
  guestGuard,
  mustChangePasswordGuard,
  passwordOkGuard,
  permissionGuard,
  superAdminGuard,
} from './auth.guard';
import { AuthService } from './auth.service';

function setup(authPartial: Partial<AuthService>) {
  const createUrlTree = vi.fn((commands: unknown[]) => ({ commands }) as unknown as UrlTree);
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthService, useValue: authPartial },
      { provide: Router, useValue: { createUrlTree } },
    ],
  });
  return { createUrlTree };
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
});
